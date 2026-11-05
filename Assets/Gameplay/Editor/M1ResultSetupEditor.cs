using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// G5: หน้าผลหลังเสิร์ฟ + คะแนน Rubric v1
// - Setup Result Board: วาง ResultBoardUI (สร้างกระดานเองตอนเล่น) ข้าง SessionManager ในฉาก
// - Test Rubric Cases: ตรวจตัวคิดคะแนนกับกรณีทดสอบใน GAMEPLAY_CONTENT_V1 (ไม่ต้องกด Play)
public static class M1ResultSetupEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";

    [MenuItem("Tools/Cookverse Thai/M1/Setup Result Board")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath) throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");

        SessionManager session = UnityEngine.Object.FindFirstObjectByType<SessionManager>(FindObjectsInactive.Include);
        if (session == null) { Debug.LogError("[หน้าผล] ไม่พบ SessionManager ในฉาก"); return; }

        ResultBoardUI board = UnityEngine.Object.FindFirstObjectByType<ResultBoardUI>(FindObjectsInactive.Include);
        if (board == null)
        {
            var go = new GameObject("ResultBoard");
            Undo.RegisterCreatedObjectUndo(go, "สร้างหน้าผล");
            go.transform.SetParent(session.transform.parent, false);
            board = go.AddComponent<ResultBoardUI>();
        }
        var so = new SerializedObject(board);
        so.FindProperty("sessionManager").objectReferenceValue = session;
        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[หน้าผล] วาง ResultBoardUI ที่ {board.transform.parent?.name}/{board.name} · เชื่อม SessionManager แล้ว · แสดงหลังเสิร์ฟ 2.5 วินาที");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ---------- กรณีทดสอบคะแนน ----------
    [MenuItem("Tools/Cookverse Thai/M1/Test Rubric Cases")]
    public static void TestCases()
    {
        var lines = new List<string>();
        bool all = true;
        void Case(string name, List<ActionLogEntry> log, Func<RubricResult, bool> expect, string expectText, string mode = "Training")
        {
            RubricResult r = RubricScorer.Score(log, "test", mode);
            bool ok = expect(r);
            all &= ok;
            lines.Add($"{(ok ? "ผ่าน" : "ไม่ผ่าน")}: {name} → {r.totalScore}/{r.maxScore} {r.gradeThai} " +
                      $"[{string.Join(" ", r.categories.Select(c => $"{c.id}={c.score}"))}] คาดว่า {expectText}" +
                      (ok ? "" : $" · หมายเหตุ: {string.Join(" / ", r.TopTips(5))}"));
        }
        int Cat(RubricResult r, string id) => r.categories.First(c => c.id == id).score;

        Case("ทำถูกทุกขั้น", Perfect(), r => r.grade == "excellent" && r.totalScore == 100, "100 ผ่านดีมาก");
        Case("ไม่ล้างผัก", Perfect(skipWash: true), r => Cat(r, RubricScorer.Preparation) < 15 && r.passed, "หักหมวดการล้างและเตรียม");
        Case("ใส่กะเพราก่อนไก่สุก", Perfect(orderMistake: CookingOrder.KrapowBeforeChickenCooked), r => Cat(r, RubricScorer.Order) < 25, "หักหมวดลำดับการปรุง");
        Case("ไม่ปิดไฟก่อนเสิร์ฟ", Perfect(heat: 2), r => Cat(r, RubricScorer.Cooking) < 20, "หักหมวดความสุกและการใช้ไฟ");
        Case("เสิร์ฟไก่ดิบ", Perfect(chicken: IngredientState.Cooking), r => r.grade == "fail" && !string.IsNullOrEmpty(r.forcedFailReason), "ไม่ผ่าน");
        Case("อาหารไหม้", Perfect(chicken: IngredientState.Burned), r => r.grade == "fail", "ไม่ผ่าน");
        Case("ขาดน้ำมันและน้ำตาล", Perfect(missing: "cooking_oil,sugar"), r => Cat(r, RubricScorer.Ingredients) == 12, "หมวดวัตถุดิบเหลือ 12");
        Case("ยังไม่เสิร์ฟ", Perfect(serve: false), r => r.grade == "fail" && Cat(r, RubricScorer.Serving) < 10, "ไม่ผ่าน");
        Case("Challenge ขอคำใบ้ 2 ครั้ง", Perfect(hints: 2), r => Cat(r, RubricScorer.Efficiency) == 6, "หมวดเวลาเหลือ 6", "Challenge");
        Case("Training ขอคำใบ้ไม่หัก", Perfect(hints: 2), r => Cat(r, RubricScorer.Efficiency) == 10, "หมวดเวลาเต็ม 10");

        lines.Add(all ? "ผลรวม: ผ่าน" : "ผลรวม: ไม่ผ่าน");
        Debug.Log("[ทดสอบคะแนน M1]\n" + string.Join("\n", lines));
    }

    private static List<ActionLogEntry> Perfect(bool skipWash = false, string orderMistake = null, int heat = 0,
        IngredientState chicken = IngredientState.Cooked, string missing = "", bool serve = true, int hints = 0)
    {
        var log = new List<ActionLogEntry>();
        float t = 0f;
        void Add(string action, string id = "", bool ok = true, string value = "", string after = "")
        {
            log.Add(new ActionLogEntry { action = action, ingredientId = id, isCorrect = ok, value = value, stateAfter = after, elapsedSeconds = t += 10f });
        }

        Add(ActionLog.Actions.MissionAccepted);
        foreach (string id in new[] { "krapow_leaf", "chili", "chicken", "garlic" }) Add(ActionLog.Actions.IngredientCollected, id);
        if (!skipWash) foreach (string id in new[] { "krapow_leaf", "chili" }) Add(ActionLog.Actions.IngredientWashed, id);
        foreach (string id in new[] { "chili", "garlic", "chicken" }) Add(ActionLog.Actions.IngredientCut, id);
        for (int i = 0; i < hints; i++) Add(ActionLog.Actions.HintRequested);
        foreach (string id in RubricScorer.DishIngredients)
        {
            bool wrong = orderMistake != null && id == "krapow_leaf";
            Add(ActionLog.Actions.IngredientAddedToPan, id, !wrong, wrong ? $"order={orderMistake};heat=2" : "heat=2");
        }
        Add(ActionLog.Actions.FoodStateChanged, "chicken", chicken != IngredientState.Burned, "heat=80", chicken.ToString());
        string dish = $"heat={heat};chicken={chicken};missing={missing}";
        Add(ActionLog.Actions.DishPlated, ok: false, value: dish);
        if (serve)
        {
            Add(ActionLog.Actions.DishServed, ok: false, value: dish);
            Add(ActionLog.Actions.MissionCompleted);
        }
        return log;
    }
}
