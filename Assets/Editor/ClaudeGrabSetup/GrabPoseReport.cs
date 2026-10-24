#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Interaction.HandGrab;

// Temporary helper written by Claude: dumps info about sample grab poses and the
// kitchen objects so the custom grab poses can be set up. Safe to delete.
public static class GrabPoseReport
{
    const string SampleRoot = "Packages/com.meta.xr.sdk.interaction/Runtime/Sample/Objects/Props/";
    static readonly string[] Samples = { "Mug/Mug.prefab", "Torch/Torch.prefab", "Key/Key.prefab", "PictureFrame/PictureFrame.prefab", "Box/Box.prefab", "ChessPiece/ChessPiece.prefab" };
    static readonly string[] Targets = { "mug", "knife1", "knife2", "plate", "bowl" };

    [MenuItem("Tools/Claude Grab Setup/1 - Write Report")]
    public static void Write()
    {
        var sb = new StringBuilder();
        foreach (var s in Samples)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SampleRoot + s);
            sb.AppendLine($"=== SAMPLE {s} loaded={(prefab != null)}");
            if (prefab == null) continue;
            DumpObject(sb, prefab.transform, prefab.transform);
        }
        foreach (var t in Targets)
        {
            var go = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == t);
            sb.AppendLine($"=== TARGET {t} found={(go != null)}");
            if (go == null) continue;
            sb.AppendLine($"  worldPos={V(go.transform.position)} worldRot={V(go.transform.eulerAngles)} lossyScale={V(go.transform.lossyScale)}");
            DumpObject(sb, go.transform, go.transform);
        }
        File.WriteAllText(Path.Combine(Application.dataPath, "../ClaudeGrabReport.txt"), sb.ToString());
        Debug.Log("Claude grab report written");
    }

    static string V(Vector3 v) => $"({v.x:F4},{v.y:F4},{v.z:F4})";
    static string Q(Quaternion q) => $"({q.x:F5},{q.y:F5},{q.z:F5},{q.w:F5})";

    static void DumpObject(StringBuilder sb, Transform root, Transform t, int depth = 0)
    {
        string ind = new string(' ', 2 + depth * 2);
        Vector3 lp = root.InverseTransformPoint(t.position);
        Quaternion lr = Quaternion.Inverse(root.rotation) * t.rotation;
        var comps = t.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name);
        sb.AppendLine($"{ind}- {t.name} rootLocalPos={V(lp)} rootLocalRot={V(lr.eulerAngles)} localScale={V(t.localScale)} [{string.Join(",", comps)}]");
        var mf = t.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            sb.AppendLine($"{ind}    meshBounds center={V(mf.sharedMesh.bounds.center)} size={V(mf.sharedMesh.bounds.size)} mesh={mf.sharedMesh.name}");
        var col = t.GetComponent<Collider>();
        if (col is BoxCollider bc) sb.AppendLine($"{ind}    box center={V(bc.center)} size={V(bc.size)} trigger={bc.isTrigger}");
        if (col is CapsuleCollider cc) sb.AppendLine($"{ind}    capsule c={V(cc.center)} r={cc.radius} h={cc.height} dir={cc.direction}");
        if (col is SphereCollider sc) sb.AppendLine($"{ind}    sphere c={V(sc.center)} r={sc.radius}");
        foreach (var hgp in t.GetComponents<HandGrabPose>())
        {
            var so = new SerializedObject(hgp);
            var hp = so.FindProperty("_targetHandPose");
            var ff = hp.FindPropertyRelative("_fingersFreedom");
            var fs = string.Join(",", Enumerable.Range(0, ff.arraySize).Select(i => ff.GetArrayElementAtIndex(i).intValue));
            sb.AppendLine($"{ind}    HandGrabPose hand={hp.FindPropertyRelative("_handedness").intValue} freedom=[{fs}] joints={hp.FindPropertyRelative("_jointRotations").arraySize} ovrOffset={so.FindProperty("_ovrOffsetMode").intValue} surface={(so.FindProperty("_surface").objectReferenceValue ? so.FindProperty("_surface").objectReferenceValue.GetType().Name : "none")} relTo={(so.FindProperty("_relativeTo").objectReferenceValue ? so.FindProperty("_relativeTo").objectReferenceValue.name : "none")} localRot={Q(t.localRotation)}");
        }
        foreach (var hgi in t.GetComponents<HandGrabInteractable>())
        {
            var so = new SerializedObject(hgi);
            var poses = so.FindProperty("_handGrabPoses");
            var names = string.Join(",", Enumerable.Range(0, poses.arraySize).Select(i => poses.GetArrayElementAtIndex(i).objectReferenceValue ? poses.GetArrayElementAtIndex(i).objectReferenceValue.name : "null"));
            var rb = so.FindProperty("_rigidbody").objectReferenceValue;
            sb.AppendLine($"{ind}    HandGrabInteractable poses=[{names}] rb={(rb ? rb.name : "none")} align={so.FindProperty("_handAligment").intValue} types={so.FindProperty("_supportedGrabTypes").intValue}");
        }
        foreach (var g in t.GetComponents<Oculus.Interaction.Grabbable>())
        {
            var so = new SerializedObject(g);
            var tt = so.FindProperty("_targetTransform").objectReferenceValue;
            var rb = so.FindProperty("_rigidbody").objectReferenceValue;
            sb.AppendLine($"{ind}    Grabbable target={(tt ? tt.name : "none")} rb={(rb ? rb.name : "none")}");
        }
        var body = t.GetComponent<Rigidbody>();
        if (body) sb.AppendLine($"{ind}    Rigidbody kinematic={body.isKinematic} gravity={body.useGravity}");
        foreach (Transform c in t) DumpObject(sb, root, c, depth + 1);
    }
}
#endif
