#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.HandGrab.Visuals;
using Oculus.Interaction.Grab.GrabSurfaces;

// Temporary helper written by Claude (geometry analysis). Safe to delete.
public static class GrabPoseReport2
{
    const string SampleRoot = "Packages/com.meta.xr.sdk.interaction/Runtime/Sample/Objects/Props/";
    static readonly string[] Samples = { "Mug/Mug.prefab", "Torch/Torch.prefab", "Key/Key.prefab" };
    static readonly string[] Targets = { "mug", "knife1", "knife2", "plate", "bowl" };
    static readonly string[] Tips = { "XRHand_ThumbTip", "XRHand_IndexTip", "XRHand_MiddleTip", "XRHand_RingTip", "XRHand_LittleTip", "XRHand_Palm", "XRHand_IndexProximal", "XRHand_Wrist" };

    static string V(Vector3 v) => $"({v.x:F4},{v.y:F4},{v.z:F4})";

    [MenuItem("Tools/Claude Grab Setup/2 - Geometry Report")]
    public static void Write()
    {
        var sb = new StringBuilder();
        var provider = AssetDatabase.LoadAssetAtPath<HandGhostProvider>(AssetDatabase.GUIDToAssetPath("9b9df4f150839de48af887d742aabf53"));
        sb.AppendLine($"ghostProvider={(provider ? provider.name : "null")}");
        foreach (var s in Samples)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SampleRoot + s);
            var inst = (GameObject)Object.Instantiate(prefab);
            inst.transform.position = new Vector3(100, 0, 0);
            sb.AppendLine($"=== SAMPLE {s}");
            int idx = 0;
            foreach (var hgi in inst.GetComponentsInChildren<HandGrabInteractable>(true))
            {
                sb.AppendLine($"  interactable #{idx++} {hgi.name}");
                foreach (var p in hgi.HandGrabPoses)
                {
                    if (Mathf.Abs(p.transform.lossyScale.x - 1f) > 0.01f) continue;
                    sb.AppendLine($"    pose hand={p.HandPose?.Handedness} wristLocal={V(inst.transform.InverseTransformPoint(p.transform.position))}");
                    var cyl = p.GetComponent<CylinderGrabSurface>();
                    if (cyl)
                    {
                        var so = new SerializedObject(cyl);
                        var d = so.FindProperty("_data");
                        var rel = so.FindProperty("_relativeTo").objectReferenceValue as Transform;
                        sb.AppendLine($"      cyl start={V(d.FindPropertyRelative("startPoint").vector3Value)} end={V(d.FindPropertyRelative("endPoint").vector3Value)} arcOff={d.FindPropertyRelative("arcOffset").floatValue} arcLen={d.FindPropertyRelative("arcLength").floatValue} rel={(rel ? rel.name : "none")}");
                    }
                    if (provider != null && p.HandPose != null)
                    {
                        var proto = provider.GetHand(p.HandPose.Handedness);
                        var ghost = Object.Instantiate(proto);
                        ghost.SetPose(p);
                        foreach (var tn in Tips)
                        {
                            var t = ghost.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == tn);
                            if (t) sb.AppendLine($"      {tn} {V(inst.transform.InverseTransformPoint(t.position))}");
                        }
                        Object.DestroyImmediate(ghost.gameObject);
                    }
                }
            }
            var mf = inst.GetComponentsInChildren<MeshFilter>().FirstOrDefault();
            if (mf) Profile(sb, mf, inst.transform);
            Object.DestroyImmediate(inst);
        }
        foreach (var t in Targets)
        {
            var go = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == t);
            if (!go) continue;
            sb.AppendLine($"=== TARGET {t}");
            Profile(sb, go.GetComponent<MeshFilter>(), go.transform);
        }
        File.WriteAllText(Path.Combine(Application.dataPath, "../ClaudeGrabReport2.txt"), sb.ToString());
        Debug.Log("Claude grab report 2 written");
    }

    // Slice profiles in root-local space (meters, unscaled world units relative to root).
    static void Profile(StringBuilder sb, MeshFilter mf, Transform root)
    {
        var verts = mf.sharedMesh.vertices.Select(v => root.InverseTransformPoint(mf.transform.TransformPoint(v))).ToArray();
        // convert to world-scale offsets relative to root (undo root scale)
        var ls = root.lossyScale;
        verts = verts.Select(v => Vector3.Scale(v, ls)).ToArray();
        var min = new Vector3(verts.Min(v => v.x), verts.Min(v => v.y), verts.Min(v => v.z));
        var max = new Vector3(verts.Max(v => v.x), verts.Max(v => v.y), verts.Max(v => v.z));
        var cen = verts.Aggregate(Vector3.zero, (a, b) => a + b) / verts.Length;
        sb.AppendLine($"  verts={verts.Length} min={V(min)} max={V(max)} centroid={V(cen)}");
        for (int axis = 0; axis < 3; axis++)
        {
            sb.AppendLine($"  slices along {"xyz"[axis]}:");
            int n = 12;
            for (int i = 0; i < n; i++)
            {
                float a = min[axis] + (max[axis] - min[axis]) * i / n, b = min[axis] + (max[axis] - min[axis]) * (i + 1) / n;
                var sl = verts.Where(v => v[axis] >= a && v[axis] <= b).ToArray();
                if (sl.Length == 0) { sb.AppendLine($"    [{a:F3},{b:F3}] empty"); continue; }
                var smin = new Vector3(sl.Min(v => v.x), sl.Min(v => v.y), sl.Min(v => v.z));
                var smax = new Vector3(sl.Max(v => v.x), sl.Max(v => v.y), sl.Max(v => v.z));
                sb.AppendLine($"    [{a:F3},{b:F3}] n={sl.Length} min={V(smin)} max={V(smax)}");
            }
        }
    }
}
#endif
