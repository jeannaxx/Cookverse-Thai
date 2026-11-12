using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// สร้างไฟล์เมนู (ScriptableObject) จาก MenuCatalog แล้วผูกเข้า ObjectiveManager ในฉาก
// Tools > Cookverse Thai > M1 > Create Menu Assets
public static class M1MenuAssetsEditor
{
    private const string Folder = "Assets/Gameplay/Data/Menus";
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";

    [MenuItem("Tools/Cookverse Thai/M1/Create Menu Assets")]
    public static void CreateMenuAssets()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[เมนู] หยุด Play ก่อน");
            return;
        }

        Directory.CreateDirectory(Folder);
        MenuDefinition first = null;
        int count = 0;

        foreach (MenuDefinition source in MenuCatalog.All())
        {
            string path = $"{Folder}/Menu_{ToPascal(source.menuId)}.asset";
            MenuDefinition asset = AssetDatabase.LoadAssetAtPath<MenuDefinition>(path);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<MenuDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            EditorUtility.CopySerialized(source, asset);
            asset.name = Path.GetFileNameWithoutExtension(path);
            EditorUtility.SetDirty(asset);
            first ??= asset;
            count++;
            Debug.Log($"[เมนู] บันทึก {source.nameThai} → {path} (ขั้น {asset.steps.Count} · กฎลำดับ {asset.orderRules.Count} · จุดคน {asset.stirCheckpoints.Count})");
        }

        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.GetActiveScene();
        ObjectiveManager om = Object.FindAnyObjectByType<ObjectiveManager>();

        if (scene.path == ScenePath && om != null && first != null)
        {
            var so = new SerializedObject(om);
            SerializedProperty menuProp = so.FindProperty("menu");
            if (menuProp.objectReferenceValue != null)
            {
                Debug.Log($"[เมนู] ObjectiveManager ใช้ {((MenuDefinition)menuProp.objectReferenceValue).nameThai} อยู่แล้ว ไม่เปลี่ยน");
                Debug.Log($"[เมนู] เสร็จ {count} เมนู");
                return;
            }
            menuProp.objectReferenceValue = first;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[เมนู] ผูก {first.nameThai} เข้า ObjectiveManager แล้ว และบันทึกฉาก");
        }
        else
        {
            Debug.LogWarning("[เมนู] สร้างไฟล์แล้ว แต่ไม่ได้ผูกเข้าฉาก (เปิด GameplayTest_M1 แล้วกดอีกครั้ง)");
        }

        Debug.Log($"[เมนู] เสร็จ {count} เมนู");
    }

    private static string ToPascal(string id)
    {
        var parts = id.Split('_');
        for (int i = 0; i < parts.Length; i++)
            if (parts[i].Length > 0) parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
        return string.Join("", parts);
    }
}
