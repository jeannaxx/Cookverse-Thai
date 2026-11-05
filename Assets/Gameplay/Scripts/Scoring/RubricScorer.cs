using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// คะแนนตาม Recipe Rubric v1 (GAMEPLAY_CONTENT_V1.md) · ระบบกฎล้วน อ่านจาก ActionLog เท่านั้น
// AI (ภายหลัง) ใช้ผลนี้อธิบาย ไม่ได้ให้คะแนนเอง · ค่าหักแต่ละข้ออยู่ในค่าคงที่ด้านล่าง ปรับได้ที่เดียว
[Serializable]
public class RubricCategory
{
    public string id;
    public string nameThai;
    public int maxScore;
    public int score;
    public List<string> notes = new();   // เหตุผลที่หักคะแนน (ภาษาไทย สั้น)
}

[Serializable]
public class RubricResult
{
    public string sessionId;
    public string mode;
    public int totalScore;
    public int maxScore;
    public string grade;           // excellent / pass / practice / fail
    public string gradeThai;
    public bool passed;
    public string forcedFailReason; // ไม่ว่างเมื่อผิดเงื่อนไขบังคับ (ไก่ดิบ/ไหม้/ไม่ได้เสิร์ฟ)
    public float elapsedSeconds;
    public List<RubricCategory> categories = new();

    public string ToJson(bool pretty = true) => JsonUtility.ToJson(this, pretty);

    // ข้อแนะนำสั้น ๆ เรียงตามหมวดที่เสียคะแนนมากสุด (ใช้บนหน้าผลและให้ป้าแสงพูด)
    public List<string> TopTips(int count)
    {
        var tips = new List<string>();
        if (!string.IsNullOrEmpty(forcedFailReason)) tips.Add(forcedFailReason);
        foreach (RubricCategory c in categories.OrderByDescending(c => c.maxScore - c.score))
        {
            foreach (string n in c.notes)
            {
                if (tips.Count >= count) return tips;
                if (!tips.Contains(n)) tips.Add(n);
            }
        }
        return tips;
    }
}

public static class RubricScorer
{
    public const string Ingredients = "ingredients";
    public const string Preparation = "preparation";
    public const string Order = "order";
    public const string Cooking = "cooking";
    public const string Serving = "serving";
    public const string Efficiency = "efficiency";

    // ---------- ค่าหักคะแนน ----------
    private const int MissingIngredientPenalty = 4;   // ขาดวัตถุดิบในจาน /ชนิด
    private const int WrongPickPenalty = 2;           // เก็บของผิดชนิด /ครั้ง
    private const int WrongPickMax = 8;
    private const int NotWashedPenalty = 4;           // ไม่ได้ล้างผัก /ชนิด (กะเพรา พริก)
    private const int NotCutPenalty = 2;              // ไม่ได้หั่น /ชนิด (พริก กระเทียม ไก่)
    private const int WashedChickenPenalty = 3;       // พยายามล้างไก่ดิบ (ความปลอดภัยอาหาร)
    private const int PrepRejectPenalty = 1;          // ใส่กระทะไม่ได้เพราะยังไม่เตรียม /ครั้ง
    private const int PrepRejectMax = 3;
    private const int OrderMistakePenalty = 5;        // ใส่ผิดลำดับ /ครั้ง
    private const int MissingStirPenalty = 3;         // ไม่ได้คน /ครั้งที่ต้องคน
    private const int HeatOnPenalty = 8;              // ไม่ปิดเตาก่อนตักเสิร์ฟ
    private const int OtherBurnedPenalty = 5;         // วัตถุดิบอื่นไหม้
    private const int DuplicatePenalty = 1;           // ทำซ้ำไม่จำเป็น /ครั้ง
    private const int DuplicateMax = 4;
    private const int ChallengeHintPenalty = 2;       // ขอคำใบ้ (Challenge เท่านั้น) /ครั้ง
    private const int ChallengeHintMax = 6;
    private const float TargetMinutes = 12f;          // เกินนี้เริ่มหัก
    private const float MinutesPerPenalty = 2f;       // ทุก 2 นาทีที่เกิน หัก 1
    private const int TimeMax = 4;

    public static readonly string[] DishIngredients =
    {
        ObjectiveManager.CookingOilIngredientId, ObjectiveManager.GarlicIngredientId, ObjectiveManager.ChiliIngredientId,
        ObjectiveManager.ChickenIngredientId, ObjectiveManager.OysterSauceIngredientId, ObjectiveManager.FishSauceIngredientId,
        ObjectiveManager.SugarIngredientId, ObjectiveManager.KrapowLeafIngredientId,
    };
    private static readonly string[] WashIds = { ObjectiveManager.KrapowLeafIngredientId, ObjectiveManager.ChiliIngredientId };
    private static readonly string[] CutIds = { ObjectiveManager.ChiliIngredientId, ObjectiveManager.GarlicIngredientId, ObjectiveManager.ChickenIngredientId };

    public static RubricResult Score(IReadOnlyList<ActionLogEntry> log, string sessionId = "", string mode = "Training")
    {
        log ??= Array.Empty<ActionLogEntry>();
        var result = new RubricResult { sessionId = sessionId, mode = mode };
        bool challenge = string.Equals(mode, "Challenge", StringComparison.OrdinalIgnoreCase);

        ActionLogEntry served = log.LastOrDefault(e => e.action == ActionLog.Actions.DishServed);
        ActionLogEntry plated = log.LastOrDefault(e => e.action == ActionLog.Actions.DishPlated);
        ActionLogEntry dish = served ?? plated;
        Dictionary<string, string> dishValues = ParseValues(dish?.value);
        var missing = new HashSet<string>(
            dish == null ? DishIngredients : Split(dishValues.TryGetValue("missing", out string m) ? m : ""));
        string chickenState = dishValues.TryGetValue("chicken", out string cs) ? cs : IngredientState.Raw.ToString();
        int heatAtPlate = dishValues.TryGetValue("heat", out string hs) && int.TryParse(hs, out int h) ? h : 0;
        result.elapsedSeconds = served != null ? served.elapsedSeconds : (log.Count > 0 ? log[log.Count - 1].elapsedSeconds : 0f);

        // 1) วัตถุดิบถูกชนิดและครบจำนวน (20)
        var c1 = Category(result, Ingredients, "วัตถุดิบถูกชนิดและครบ", 20);
        foreach (string id in DishIngredients.Where(missing.Contains))
        {
            Deduct(c1, MissingIngredientPenalty, $"ในจานขาด{DishSummaryThai(id)}");
        }
        int wrongPicks = log.Count(e => e.action == ActionLog.Actions.IngredientCollected && !e.isCorrect &&
                                        e.value == IngredientCollectionResult.WrongIngredient.ToString());
        if (wrongPicks > 0) Deduct(c1, Mathf.Min(wrongPicks * WrongPickPenalty, WrongPickMax), $"เก็บวัตถุดิบผิดชนิด {wrongPicks} ครั้ง");

        // 2) การล้างและเตรียม (15)
        var c2 = Category(result, Preparation, "การล้างและเตรียม", 15);
        foreach (string id in WashIds)
        {
            if (!log.Any(e => e.action == ActionLog.Actions.IngredientWashed && e.isCorrect && e.ingredientId == id))
                Deduct(c2, NotWashedPenalty, $"ยังไม่ได้ล้าง{DishSummaryThai(id)}");
        }
        foreach (string id in CutIds)
        {
            if (!log.Any(e => e.action == ActionLog.Actions.IngredientCut && e.isCorrect && e.ingredientId == id))
                Deduct(c2, NotCutPenalty, $"ยังไม่ได้หั่น{DishSummaryThai(id)}");
        }
        if (log.Any(e => e.action == ActionLog.Actions.IngredientWashed && e.ingredientId == ObjectiveManager.ChickenIngredientId))
            Deduct(c2, WashedChickenPenalty, "ไม่ควรล้างไก่ดิบใต้ก๊อก (น้ำกระเด็นพาเชื้อโรค)");
        int prepRejects = log.Count(e => e.action == ActionLog.Actions.IngredientAddedToPan && !e.isCorrect &&
                                         !IsOrderMistake(e) && !IsDuplicate(e));
        if (prepRejects > 0) Deduct(c2, Mathf.Min(prepRejects * PrepRejectPenalty, PrepRejectMax), $"ใส่ของที่ยังไม่เตรียมลงกระทะ {prepRejects} ครั้ง");

        // 3) ลำดับการปรุง (25)
        var c3 = Category(result, Order, "ลำดับการปรุง", 25);
        foreach (ActionLogEntry e in log.Where(IsOrderMistake))
        {
            Deduct(c3, OrderMistakePenalty, OrderMistakeThai(ParseValues(e.value).TryGetValue("order", out string code) ? code : ""));
        }
        if (dish != null && missing.Contains(ObjectiveManager.StirAromaticsId)) Deduct(c3, MissingStirPenalty, "ไม่ได้คนเครื่องหอมก่อนใส่ไก่");
        if (dish != null && missing.Contains(ObjectiveManager.StirFinalId)) Deduct(c3, MissingStirPenalty, "ไม่ได้คนให้เข้ากันหลังใส่กะเพรา");

        // 4) ความสุกและการใช้ไฟ (20) · ไก่ดิบ/ไหม้ = ไม่ผ่าน
        var c4 = Category(result, Cooking, "ความสุกและการใช้ไฟ", 20);
        if (chickenState == IngredientState.Burned.ToString())
        {
            Deduct(c4, c4.maxScore, "ไก่ไหม้ ลดไฟลงหรือตักออกเร็วขึ้น");
            result.forcedFailReason = "ไก่ไหม้ (เงื่อนไขบังคับ ไม่ผ่าน)";
        }
        else if (chickenState != IngredientState.Cooked.ToString() && chickenState != IngredientState.Served.ToString())
        {
            Deduct(c4, c4.maxScore, "ไก่ยังไม่สุก ผัดต่ออีกสักพักก่อนตัก");
            result.forcedFailReason = "ไก่ยังไม่สุก (เงื่อนไขบังคับ ไม่ผ่าน)";
        }
        if (heatAtPlate > 0) Deduct(c4, HeatOnPenalty, "ปิดเตาก่อนตักใส่จาน");
        if (log.Any(e => e.action == ActionLog.Actions.FoodStateChanged && e.stateAfter == IngredientState.Burned.ToString() &&
                         e.ingredientId != ObjectiveManager.ChickenIngredientId))
            Deduct(c4, OtherBurnedPenalty, "มีวัตถุดิบไหม้ในกระทะ");

        // 5) การเสิร์ฟและจบภารกิจ (10)
        var c5 = Category(result, Serving, "การเสิร์ฟและจบภารกิจ", 10);
        if (plated == null) Deduct(c5, 4, "ยังไม่ได้ตักใส่จาน");
        if (served == null) Deduct(c5, 4, "ยังไม่ได้นำจานไปเสิร์ฟป้าแสง");
        if (!log.Any(e => e.action == ActionLog.Actions.MissionCompleted)) Deduct(c5, 2, "ภารกิจยังไม่จบ");
        if (served == null && string.IsNullOrEmpty(result.forcedFailReason)) result.forcedFailReason = "ยังไม่ได้เสิร์ฟอาหาร";

        // 6) เวลาและการทำซ้ำที่ไม่จำเป็น (10)
        var c6 = Category(result, Efficiency, "เวลาและการทำซ้ำ", 10);
        int dups = log.Count(IsDuplicate);
        if (dups > 0) Deduct(c6, Mathf.Min(dups * DuplicatePenalty, DuplicateMax), $"ทำซ้ำไม่จำเป็น {dups} ครั้ง");
        int hints = log.Count(e => e.action == ActionLog.Actions.HintRequested);
        if (challenge && hints > 0) Deduct(c6, Mathf.Min(hints * ChallengeHintPenalty, ChallengeHintMax), $"ขอคำใบ้ {hints} ครั้ง");
        float minutes = result.elapsedSeconds / 60f;
        if (minutes > TargetMinutes)
        {
            int over = Mathf.Min(TimeMax, Mathf.CeilToInt((minutes - TargetMinutes) / MinutesPerPenalty));
            Deduct(c6, over, $"ใช้เวลา {minutes:0} นาที (เป้าหมายไม่เกิน {TargetMinutes:0} นาที)");
        }

        result.maxScore = result.categories.Sum(c => c.maxScore);
        result.totalScore = result.categories.Sum(c => c.score);
        SetGrade(result);
        return result;
    }

    private static void SetGrade(RubricResult r)
    {
        if (!string.IsNullOrEmpty(r.forcedFailReason) || r.totalScore < 50) { r.grade = "fail"; r.gradeThai = "ไม่ผ่าน"; }
        else if (r.totalScore >= 80) { r.grade = "excellent"; r.gradeThai = "ผ่านดีมาก"; }
        else if (r.totalScore >= 70) { r.grade = "pass"; r.gradeThai = "ผ่าน"; }
        else { r.grade = "practice"; r.gradeThai = "ต้องฝึกเพิ่ม"; }
        r.passed = r.grade == "excellent" || r.grade == "pass";
    }

    private static RubricCategory Category(RubricResult r, string id, string name, int max)
    {
        var c = new RubricCategory { id = id, nameThai = name, maxScore = max, score = max };
        r.categories.Add(c);
        return c;
    }

    private static void Deduct(RubricCategory c, int points, string note)
    {
        c.score = Mathf.Max(0, c.score - Mathf.Max(0, points));
        if (!string.IsNullOrEmpty(note) && !c.notes.Contains(note)) c.notes.Add(note);
    }

    private static bool IsOrderMistake(ActionLogEntry e) =>
        e.action == ActionLog.Actions.IngredientAddedToPan && !e.isCorrect && e.value != null &&
        e.value.Contains("order=") && !e.value.Contains("order=" + CookingOrder.Duplicate);

    private static bool IsDuplicate(ActionLogEntry e) =>
        !e.isCorrect && e.value != null &&
        (e.value.Contains(CookingOrder.Duplicate) || e.value == IngredientCollectionResult.Duplicate.ToString());

    // "a=1;b=2" → {a:1, b:2}
    public static Dictionary<string, string> ParseValues(string value)
    {
        var d = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(value)) return d;
        foreach (string part in value.Split(';'))
        {
            int i = part.IndexOf('=');
            if (i > 0) d[part.Substring(0, i).Trim()] = part.Substring(i + 1).Trim();
        }
        return d;
    }

    private static IEnumerable<string> Split(string csv) =>
        string.IsNullOrEmpty(csv) ? Array.Empty<string>() : csv.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0);

    public static string DishSummaryThai(string id) => id switch
    {
        ObjectiveManager.CookingOilIngredientId => "น้ำมัน",
        ObjectiveManager.GarlicIngredientId => "กระเทียม",
        ObjectiveManager.ChiliIngredientId => "พริก",
        ObjectiveManager.ChickenIngredientId => "ไก่",
        ObjectiveManager.OysterSauceIngredientId => "ซอสหอยนางรม",
        ObjectiveManager.FishSauceIngredientId => "น้ำปลา",
        ObjectiveManager.SugarIngredientId => "น้ำตาล",
        ObjectiveManager.KrapowLeafIngredientId => "ใบกะเพรา",
        _ => id
    };

    private static string OrderMistakeThai(string code) => code switch
    {
        CookingOrder.AromaticsBeforeOil => "ใส่กระเทียม/พริกก่อนน้ำมัน",
        CookingOrder.AromaticsAfterChicken => "ใส่กระเทียม/พริกหลังไก่ (ควรผัดเครื่องหอมก่อน)",
        CookingOrder.ChickenBeforeOil => "ใส่ไก่ก่อนน้ำมัน",
        CookingOrder.ChickenBeforeAromatics => "ใส่ไก่ก่อนกระเทียมและพริก",
        CookingOrder.ChickenBeforeStir => "ใส่ไก่ก่อนคนเครื่องหอมให้หอม",
        CookingOrder.SauceBeforeChickenCooked => "ปรุงรสก่อนไก่สุก",
        CookingOrder.KrapowBeforeChickenCooked => "ใส่กะเพราก่อนไก่สุก",
        CookingOrder.KrapowBeforeSauce => "ใส่กะเพราก่อนปรุงรส (กะเพราใส่ท้ายสุด)",
        _ => "ใส่วัตถุดิบผิดลำดับ"
    };
}
