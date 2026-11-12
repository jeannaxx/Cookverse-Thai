using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// เพิ่มมือตอนถือจอย (M1ControllerHands) ใน GameplayTest_M1 · รันซ้ำได้
public static class M1ControllerHandsEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string Folder = "Assets/Gameplay/Art/Hands";

    [MenuItem("Tools/Cookverse Thai/M1/Setup Controller Hands (Hand Holding Controller)")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");

        var left = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/M1CustomLeftHand.prefab");
        var right = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/M1CustomRightHand.prefab");
        if (left == null || right == null)
        {
            Debug.LogError("[มือถือจอย] ไม่พบ M1CustomLeft/RightHand.prefab · รัน Apply Custom Hands ก่อน");
            return;
        }

        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        // ลบตัวเดิมแล้วสร้างใหม่ ให้ได้ค่าท่ามือล่าสุดจากโค้ด (ค่าที่บันทึกในฉากจะไม่ทับ)
        M1ControllerHands hands = all.Select(t => t.GetComponent<M1ControllerHands>()).FirstOrDefault(c => c != null);
        if (hands != null)
        {
            GameObject host = hands.gameObject;
            Undo.DestroyObjectImmediate(hands);
            hands = Undo.AddComponent<M1ControllerHands>(host);
        }
        else
        {
            Transform parent = all.FirstOrDefault(t => t.name == "GameplaySystems");
            var go = new GameObject("ControllerHands");
            Undo.RegisterCreatedObjectUndo(go, "สร้างมือถือจอย");
            if (parent != null) go.transform.SetParent(parent, false);
            hands = go.AddComponent<M1ControllerHands>();
        }
        hands.Configure(left, right);
        EditorUtility.SetDirty(hands);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[มือถือจอย] เสร็จแล้ว · กด Play แล้วกด Y เลือก Controllers ใน Simulator (หรือถือจอยในแว่น) · " +
                  "ปรับท่าได้ที่ ControllerHands ใน Inspector ระหว่าง Play");
    }
}
