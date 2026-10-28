using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// แก้ตัวผู้เล่นสูงเกินใน WebXR
// เดิม XR Origin ตั้ง Tracking Origin Mode = Not Specified + Camera Y Offset 1.6 ม.
// WebXR (local-floor) ส่งความสูงหัวจริงมาให้อยู่แล้ว เลยถูกบวก 1.6 ซ้ำ → สายตาสูง ~3.2 ม.
// ตั้งเป็น Floor แล้ว Camera Y Offset จะไม่ถูกใช้ ใช้ความสูงจริงจากแว่นแทน (ใช้ได้กับ Quest APK ด้วย)
public static class M1TrackingOriginEditor
{
    [MenuItem("Tools/Cookverse Thai/M1/Fix Player Height (Tracking Origin = Floor)")]
    public static void Fix()
    {
        var origins = Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (origins.Length == 0)
        {
            EditorUtility.DisplayDialog("Fix Player Height", "ไม่พบ XR Origin ในฉากที่เปิดอยู่", "OK");
            return;
        }
        foreach (var origin in origins)
        {
            Undo.RecordObject(origin, "Tracking Origin = Floor");
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            PrefabUtility.RecordPrefabInstancePropertyModifications(origin);
            Debug.Log($"[Fix Player Height] {origin.name}: Tracking Origin Mode = Floor (Camera Y Offset {origin.CameraYOffset} ม. จะไม่ถูกใช้แล้ว)");
        }
        var scene = origins[0].gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Fix Player Height] บันทึกฉาก " + scene.name + " แล้ว");
    }
}
