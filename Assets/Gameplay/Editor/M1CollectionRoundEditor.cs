using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class M1CollectionRoundEditor
{
    [MenuItem("Tools/Cookverse Thai/M1/Setup Three Chilies")]
    public static void Setup()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/GameplayTest_M1.unity")
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");
        var ingredients = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<IngredientIdentity>(true)).ToArray();
        var chilies = ingredients.Where(i => i.IngredientId == IngredientIdentity.ChiliId).ToArray();
        if (chilies.Length == 0 || chilies.Length > 3)
            throw new InvalidOperationException("จำนวนพริกในฉากไม่ตรงกับการตั้งค่านี้ กรุณาตรวจฉาก");
        var source = chilies[0];
        for (int n = chilies.Length; n < 3; n++)
        {
            var copy = UnityEngine.Object.Instantiate(source.gameObject, source.transform.parent);
            copy.name = "Chili_Collectible_" + (n + 1);
            copy.transform.position = source.transform.position + new Vector3(n * 0.5f, 0, 0);
            Undo.RegisterCreatedObjectUndo(copy, "เพิ่มพริก M1");
            var identity = new SerializedObject(copy.GetComponent<IngredientIdentity>());
            identity.FindProperty("instanceId").stringValue = string.Empty;
            identity.FindProperty("currentState").enumValueIndex = 0;
            identity.ApplyModifiedProperties();
        }
        var objective = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<ObjectiveManager>(true)).Single();
        Undo.RecordObject(objective, "ตั้งจำนวนวัตถุดิบ M1");
        objective.SetRequiredCount(ObjectiveManager.CollectGardenStepId, ObjectiveManager.KrapowLeafIngredientId, 1);
        objective.SetRequiredCount(ObjectiveManager.CollectGardenStepId, ObjectiveManager.ChiliIngredientId, 3);
        EditorUtility.SetDirty(objective);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ตั้งค่า M1] ใบกะเพรา 1 กำ พริก 3 ชิ้น พร้อมทดสอบแล้ว");
    }

    [MenuItem("Tools/Cookverse Thai/M1/Test Collection Round (Play Mode)")]
    public static void Test()
    {
        if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/GameplayTest_M1.unity")
            throw new InvalidOperationException("กด Play ใน GameplayTest_M1 ก่อนทดสอบ");
        if (UnityEngine.Object.FindFirstObjectByType<M1CollectionSmokeTest>() != null) return;
        new GameObject("M1 Collection Test Runner").AddComponent<M1CollectionSmokeTest>();
    }
}
