using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// กระดานเริ่มเกม (ใส่ชื่อ + เลือกเมนู) ใน GameplayTest_M1 · รันซ้ำได้ (อัปเดตรายการเมนู)
public static class M1StartBoardEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";

    [MenuItem("Tools/Cookverse Thai/M1/Setup Start Board (Name + Menu)")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");

        M1MenuAssetsEditor.CreateMenuAssets();
        MenuDefinition[] menus = MenuCatalog.All()
            .Select(m => AssetDatabase.LoadAssetAtPath<MenuDefinition>(
                $"Assets/Gameplay/Data/Menus/Menu_{Pascal(m.menuId)}.asset"))
            .Where(m => m != null)
            .ToArray();

        var all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        ObjectiveManager objective = all.Select(t => t.GetComponent<ObjectiveManager>()).FirstOrDefault(c => c != null);
        SessionManager session = all.Select(t => t.GetComponent<SessionManager>()).FirstOrDefault(c => c != null);
        Transform gameplayUI = all.FirstOrDefault(t => t.name == "GameplayUI");
        if (objective == null || session == null || gameplayUI == null)
        {
            Debug.LogError("[กระดานเริ่ม] ไม่พบ ObjectiveManager / SessionManager / GameplayUI");
            return;
        }

        StartBoardUI board = all.Select(t => t.GetComponent<StartBoardUI>()).FirstOrDefault(c => c != null);
        if (board == null)
        {
            var go = new GameObject("StartBoardUI");
            Undo.RegisterCreatedObjectUndo(go, "สร้างกระดานเริ่ม");
            go.transform.SetParent(gameplayUI, false);
            board = go.AddComponent<StartBoardUI>();
        }
        board.transform.localScale = new Vector3(1f / gameplayUI.lossyScale.x, 1f / gameplayUI.lossyScale.y, 1f / gameplayUI.lossyScale.z);
        board.Configure(objective, session, menus);
        EditorUtility.SetDirty(board);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[กระดานเริ่ม] เสร็จแล้ว: เมนู {menus.Length} เมนู ({string.Join(", ", menus.Select(m => m.nameThai))}) · กด Play แล้วกระดานขึ้นตรงหน้า");
    }

    private static string Pascal(string id) =>
        string.Concat(id.Split('_').Where(p => p.Length > 0).Select(p => char.ToUpper(p[0]) + p.Substring(1)));
}
