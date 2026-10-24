#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.HandGrab.Visuals;
using Oculus.Interaction.Grab.GrabSurfaces;

// Written by Claude: sets up custom hand grab poses for the mug, knives, plate and bowl
// by adapting Meta's sample grab poses (Mug, Torch, Key) to these objects.
// Menu: Tools > Claude Grab Setup > 3 - Set Up Hand Grabs (safe to run again; it rebuilds).
public static class ClaudeGrabSetup
{
    const string SampleRoot = "Packages/com.meta.xr.sdk.interaction/Runtime/Sample/Objects/Props/";
    const string RigName = "HandGrab (custom poses)";
    const string GhostGuid = "9b9df4f150839de48af887d742aabf53";

    class Entry
    {
        public string sample;          // sample prefab path
        public int[] interactables;    // which HandGrabInteractables of the sample to copy
        public Quaternion rot;         // sample-root space -> body space
        public Vector3 offset;
        public float radialPush;       // push poses away from a vertical axis
        public Vector3 axisCenter;     // body space
        public bool verticalCylinder;  // replace surface with 360deg cylinder around axisCenter
        public bool mirror;            // create left hand by mirroring (sample left pose unsuitable)
    }

    static Vector3 Map(Entry e, Vector3 p) => e.rot * p + e.offset;

    static List<Entry> EntriesFor(string name)
    {
        var list = new List<Entry>();
        switch (name)
        {
            case "mug":
            {
                // Sample mug: handle on -X, body axis x=0. Ours: handle on +X, body axis x=-0.0196.
                var r = Quaternion.Euler(0, 180, 0);
                var off = new Vector3(-0.0196f, -0.009f, 0f);
                var axis = new Vector3(-0.0196f, 0, 0);
                list.Add(new Entry { sample = "Mug/Mug.prefab", interactables = new[] { 0, 1, 3, 4 }, rot = r, offset = off, radialPush = 0.0035f, axisCenter = axis });
                list.Add(new Entry { sample = "Mug/Mug.prefab", interactables = new[] { 2, 5 }, rot = r, offset = off, radialPush = 0.005f, axisCenter = axis });
                break;
            }
            case "knife1":
            case "knife2":
            {
                // Torch handle (axis +Y, head up) -> knife handle (axis Z, blade toward -Z, edge toward +X).
                float a = 200f * Mathf.Deg2Rad;
                var e1 = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var qT = Quaternion.LookRotation(e1, Vector3.up);
                var qK = Quaternion.LookRotation(Vector3.right, Vector3.back);
                var r = qK * Quaternion.Inverse(qT);
                var target = name == "knife1" ? new Vector3(-0.0046f, 0f, 0.05f) : new Vector3(0.0032f, 0f, 0.05f);
                var off = target - r * new Vector3(0, -0.04f, 0);
                list.Add(new Entry { sample = "Torch/Torch.prefab", interactables = new[] { 0, 1 }, rot = r, offset = off, verticalCylinder = false });
                break;
            }
            case "plate":
            {
                // Key pinch: thumb on top of the rim, index underneath, hand outside the plate.
                var r = Quaternion.Euler(0, -90, 0);
                var pk = new Vector3(-0.002f, -0.0014f, -0.0334f);
                var off = new Vector3(0.13f, 0.0125f, 0f) - r * pk;
                list.Add(new Entry { sample = "Key/Key.prefab", interactables = new[] { 0 }, rot = r, offset = off, verticalCylinder = true, axisCenter = new Vector3(0, 0.0125f, 0), mirror = true });
                break;
            }
            case "bowl":
            {
                // Key pinch rotated onto the bowl wall: thumb inside, index outside, hand above the rim.
                var r = Quaternion.LookRotation(new Vector3(0, -1, 0), new Vector3(-1, 0, 0));
                var pk = new Vector3(-0.002f, -0.0014f, -0.0334f);
                var off = new Vector3(0.0817f, 0.060f, 0f) - r * pk;
                list.Add(new Entry { sample = "Key/Key.prefab", interactables = new[] { 0 }, rot = r, offset = off, verticalCylinder = true, axisCenter = new Vector3(-0.0013f, 0.060f, -0.0001f), mirror = true });
                break;
            }
        }
        return list;
    }

    static readonly string[] Targets = { "mug", "knife1", "knife2", "plate", "bowl" };
    static readonly string[] StaticSurfaces = { "counter1a", "counter1a (1)", "counter1a (2)", "dish-rack", "TestFloor" };

    [MenuItem("Tools/Claude Grab Setup/3 - Set Up Hand Grabs")]
    public static void SetupAll()
    {
        var log = new StringBuilder();
        var scene = SceneManager.GetActiveScene();
        Undo.SetCurrentGroupName("Claude hand grab setup");
        int group = Undo.GetCurrentGroup();

        foreach (var sName in StaticSurfaces)
        {
            var go = scene.GetRootGameObjects().FirstOrDefault(g => g.name == sName);
            if (!go) { log.AppendLine($"static {sName}: not found"); continue; }
            var cols = go.GetComponentsInChildren<Collider>(true).Where(c => !c.isTrigger).ToArray();
            if (cols.Length > 0) { log.AppendLine($"static {sName}: has {cols.Length} collider(s)"); continue; }
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
            {
                var mc = Undo.AddComponent<MeshCollider>(mf.gameObject);
                mc.sharedMesh = mf.sharedMesh;
            }
            log.AppendLine($"static {sName}: added MeshCollider(s)");
        }

        foreach (var t in Targets)
        {
            var go = FindTarget(scene, t);
            if (!go) { log.AppendLine($"{t}: NOT FOUND"); continue; }
            SetupObject(go, log);
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        File.WriteAllText(Path.Combine(Application.dataPath, "../ClaudeGrabSetupLog.txt"), log.ToString() + "\n" + Verify());
        Debug.Log("Claude hand grab setup finished. See ClaudeGrabSetupLog.txt");
    }

    static GameObject FindTarget(Scene scene, string t)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == t) return root;
            if (root.name == t + " (grab)") { var c = root.transform.Find(t); if (c) return c.gameObject; }
        }
        return null;
    }

    static void SetupObject(GameObject obj, StringBuilder log)
    {
        string name = obj.name;
        // 1. Body = unscaled transform that carries Rigidbody + Grabbable.
        Transform body = obj.transform;
        if (Mathf.Abs(obj.transform.lossyScale.x - 1f) > 0.001f)
        {
            if (obj.transform.parent != null && obj.transform.parent.name == name + " (grab)")
            {
                body = obj.transform.parent;
            }
            else
            {
                var wrapper = new GameObject(name + " (grab)");
                Undo.RegisterCreatedObjectUndo(wrapper, "wrapper");
                wrapper.transform.SetParent(obj.transform.parent, false);
                wrapper.transform.SetPositionAndRotation(obj.transform.position, obj.transform.rotation);
                wrapper.transform.SetSiblingIndex(obj.transform.GetSiblingIndex());
                Undo.SetTransformParent(obj.transform, wrapper.transform, "reparent");
                body = wrapper.transform;
            }
        }

        // 2. Turn off the old building-block cubes (kept, just disabled).
        foreach (Transform c in obj.transform.Cast<Transform>().ToArray())
        {
            if (c.name.StartsWith("[BuildingBlock] Cube") && c.gameObject.activeSelf)
            {
                Undo.RecordObject(c.gameObject, "disable");
                c.gameObject.SetActive(false);
                log.AppendLine($"{name}: disabled old {c.name}");
            }
        }

        // 3. Remove a previous run.
        var old = body.Find(RigName);
        if (old) Undo.DestroyObjectImmediate(old.gameObject);

        // 4. Physics.
        var rb = body.GetComponent<Rigidbody>();
        if (!rb) rb = Undo.AddComponent<Rigidbody>(body.gameObject);
        Undo.RecordObject(rb, "rb");
        rb.isKinematic = false; rb.useGravity = true; rb.mass = 0.4f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        var mfObj = obj.GetComponent<MeshFilter>();
        if (!obj.GetComponents<Collider>().Any(c => !c.isTrigger) && mfObj)
        {
            var mc = Undo.AddComponent<MeshCollider>(obj);
            mc.sharedMesh = mfObj.sharedMesh;
            mc.convex = true;
            log.AppendLine($"{name}: added convex MeshCollider");
        }

        // 5. Grabbable on the body.
        var grabbable = body.GetComponent<Grabbable>();
        if (!grabbable) grabbable = Undo.AddComponent<Grabbable>(body.gameObject);
        var gso = new SerializedObject(grabbable);
        gso.FindProperty("_targetTransform").objectReferenceValue = body;
        gso.FindProperty("_rigidbody").objectReferenceValue = rb;
        gso.FindProperty("_kinematicWhileSelected").boolValue = true;
        gso.FindProperty("_throwWhenUnselected").boolValue = true;
        gso.ApplyModifiedPropertiesWithoutUndo();

        // 6. Container for the grab interactables.
        var rig = new GameObject(RigName);
        Undo.RegisterCreatedObjectUndo(rig, "rig");
        rig.transform.SetParent(body, false);

        foreach (var e in EntriesFor(name))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SampleRoot + e.sample);
            var temp = (GameObject)Object.Instantiate(prefab);
            temp.transform.SetPositionAndRotation(body.TransformPoint(e.offset), body.rotation * e.rot);
            temp.transform.localScale = Vector3.one;
            var sampleInteractables = temp.GetComponentsInChildren<HandGrabInteractable>(true);
            foreach (int idx in e.interactables)
            {
                var src = sampleInteractables[idx];
                src.transform.SetParent(rig.transform, true);
                src.gameObject.name = $"{name} grab {idx} ({(src.HandGrabPoses.FirstOrDefault()?.HandPose?.Handedness.ToString() ?? "?")})";
                FixInteractable(src, body, rb, grabbable, temp.transform, e);
                log.AppendLine($"{name}: added {src.gameObject.name} with {src.HandGrabPoses.Count} poses");
                if (e.mirror)
                {
                    var m = Mirror(src, rb, grabbable);
                    if (m) log.AppendLine($"{name}: added mirrored {m.name}");
                }
            }
            Object.DestroyImmediate(temp);
        }
        EditorUtility.SetDirty(body.gameObject);
    }

    static void FixInteractable(HandGrabInteractable hgi, Transform body, Rigidbody rb, Grabbable grabbable, Transform sampleRoot, Entry e)
    {
        var so = new SerializedObject(hgi);
        so.FindProperty("_rigidbody").objectReferenceValue = rb;
        so.FindProperty("_pointableElement").objectReferenceValue = grabbable;
        so.FindProperty("_supportedGrabTypes").intValue = 3; // pinch + palm (grip button)
        so.ApplyModifiedPropertiesWithoutUndo();

        foreach (var pose in hgi.HandGrabPoses)
        {
            var cyl = pose.GetComponent<CylinderGrabSurface>();
            // surface data is stored relative to the old relativeTo (sample root) -> convert to body space
            if (cyl)
            {
                var cso = new SerializedObject(cyl);
                var d = cso.FindProperty("_data");
                var sp = d.FindPropertyRelative("startPoint");
                var ep = d.FindPropertyRelative("endPoint");
                Vector3 ws = sampleRoot.TransformPoint(sp.vector3Value), we = sampleRoot.TransformPoint(ep.vector3Value);
                sp.vector3Value = body.InverseTransformPoint(ws);
                ep.vector3Value = body.InverseTransformPoint(we);
                cso.FindProperty("_relativeTo").objectReferenceValue = body;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
            if (e.radialPush > 0f)
            {
                Vector3 lp = body.InverseTransformPoint(pose.transform.position);
                Vector3 radial = new Vector3(lp.x - e.axisCenter.x, 0, lp.z - e.axisCenter.z).normalized;
                pose.transform.position = body.TransformPoint(lp + radial * e.radialPush);
            }
            if (e.verticalCylinder)
            {
                if (!cyl) cyl = pose.gameObject.AddComponent<CylinderGrabSurface>();
                var cso = new SerializedObject(cyl);
                var d = cso.FindProperty("_data");
                d.FindPropertyRelative("startPoint").vector3Value = e.axisCenter + new Vector3(0, 0.001f, 0);
                d.FindPropertyRelative("endPoint").vector3Value = e.axisCenter - new Vector3(0, 0.001f, 0);
                d.FindPropertyRelative("arcOffset").floatValue = 0f;
                d.FindPropertyRelative("arcLength").floatValue = 360f;
                cso.FindProperty("_relativeTo").objectReferenceValue = body;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
            var pso = new SerializedObject(pose);
            pso.FindProperty("_relativeTo").objectReferenceValue = body;
            if (cyl) pso.FindProperty("_surface").objectReferenceValue = cyl;
            var ghost = pso.FindProperty("_handGhostProvider");
            if (ghost != null && ghost.objectReferenceValue == null)
                ghost.objectReferenceValue = AssetDatabase.LoadAssetAtPath<HandGhostProvider>(AssetDatabase.GUIDToAssetPath(GhostGuid));
            pso.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static HandGrabInteractable Mirror(HandGrabInteractable target, Rigidbody rb, Grabbable grabbable)
    {
        HandGrabInteractable mirror = HandGrabUtils.CreateHandGrabInteractable(target.transform.parent, target.name.Replace("(Right)", "(Left, mirrored)"));
        var data = HandGrabUtils.SaveData(target);
        data.poses = null;
        HandGrabUtils.LoadData(mirror, data);
        var so = new SerializedObject(mirror);
        so.FindProperty("_rigidbody").objectReferenceValue = rb;
        so.FindProperty("_pointableElement").objectReferenceValue = grabbable;
        so.FindProperty("_supportedGrabTypes").intValue = 3;
        so.FindProperty("_handAligment").intValue = new SerializedObject(target).FindProperty("_handAligment").intValue;
        so.ApplyModifiedPropertiesWithoutUndo();
        var list = new List<HandGrabPose>();
        foreach (HandGrabPose point in target.HandGrabPoses)
        {
            HandGrabPose mp = HandGrabUtils.CreateHandGrabPose(mirror.transform, rb.transform);
            HandGrabUtils.MirrorHandGrabPose(point, mp, rb.transform);
            mp.transform.SetParent(mirror.transform);
            var cyl = mp.GetComponent<CylinderGrabSurface>();
            if (cyl)
            {
                var cso = new SerializedObject(cyl);
                cso.FindProperty("_relativeTo").objectReferenceValue = rb.transform;
                cso.ApplyModifiedPropertiesWithoutUndo();
            }
            var pso = new SerializedObject(mp);
            pso.FindProperty("_relativeTo").objectReferenceValue = rb.transform;
            if (cyl) pso.FindProperty("_surface").objectReferenceValue = cyl;
            var ghost = pso.FindProperty("_handGhostProvider");
            if (ghost != null && ghost.objectReferenceValue == null)
                ghost.objectReferenceValue = AssetDatabase.LoadAssetAtPath<HandGhostProvider>(AssetDatabase.GUIDToAssetPath(GhostGuid));
            pso.ApplyModifiedPropertiesWithoutUndo();
            list.Add(mp);
        }
        var mso = new SerializedObject(mirror);
        var posesProp = mso.FindProperty("_handGrabPoses");
        posesProp.arraySize = list.Count;
        for (int i = 0; i < list.Count; i++) posesProp.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        mso.ApplyModifiedPropertiesWithoutUndo();
        Undo.RegisterCreatedObjectUndo(mirror.gameObject, "mirror");
        return mirror;
    }

    // ---------- verification: where do the ghost fingertips end up relative to the object? ----------
    static readonly string[] Tips = { "XRHand_ThumbTip", "XRHand_IndexTip", "XRHand_MiddleTip", "XRHand_RingTip", "XRHand_LittleTip", "XRHand_Palm" };

    [MenuItem("Tools/Claude Grab Setup/4 - Verify")]
    public static void VerifyMenu()
    {
        File.WriteAllText(Path.Combine(Application.dataPath, "../ClaudeGrabVerify.txt"), Verify());
        Debug.Log("verify written");
    }

    static string Verify()
    {
        var sb = new StringBuilder("=== VERIFY (distance from fingertip to object surface in mm; negative = inside)\n");
        Physics.SyncTransforms();
        var provider = AssetDatabase.LoadAssetAtPath<HandGhostProvider>(AssetDatabase.GUIDToAssetPath(GhostGuid));
        var scene = SceneManager.GetActiveScene();
        foreach (var t in Targets)
        {
            var obj = FindTarget(scene, t);
            if (!obj) continue;
            var cols = obj.GetComponents<Collider>().Where(c => !c.isTrigger).ToArray();
            var mc = obj.GetComponent<MeshCollider>();
            bool wasConvex = mc && mc.convex;
            foreach (var hgi in obj.GetComponentsInParent<Transform>().Last().GetComponentsInChildren<HandGrabInteractable>(false))
            {
                foreach (var p in hgi.HandGrabPoses)
                {
                    if (!p || Mathf.Abs(p.transform.lossyScale.x - 1f) > 0.01f) continue;
                    var ghost = Object.Instantiate(provider.GetHand(p.HandPose.Handedness));
                    ghost.SetPose(p);
                    var parts = new List<string>();
                    foreach (var tn in Tips)
                    {
                        var tr = ghost.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == tn);
                        if (!tr) continue;
                        float best = float.MaxValue;
                        foreach (var c in cols)
                        {
                            var cp = c.ClosestPoint(tr.position);
                            float d = Vector3.Distance(cp, tr.position);
                            if (d < 1e-5f) d = -1f; // inside convex collider
                            best = Mathf.Min(best, d);
                        }
                        parts.Add($"{tn.Replace("XRHand_", "")}={best * 1000f:F0}");
                    }
                    sb.AppendLine($"{t} / {hgi.name} / {p.HandPose.Handedness}: {string.Join(" ", parts)}");
                    Object.DestroyImmediate(ghost.gameObject);
                }
            }
        }
        return sb.ToString();
    }
}
#endif
