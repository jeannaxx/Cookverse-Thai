using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CookVessel
{
    Pan,   // กระทะ (ผัด)
    Pot    // หม้อ (ต้ม/แกง)
}

// กฎลำดับการปรุง 1 ข้อ: ตอนใส่ ingredientId ต้องเป็นตามเงื่อนไข ไม่งั้นบันทึก mistakeCode
// ตรวจตามลำดับในรายการ ข้อแรกที่ผิดคือรหัสที่บันทึก
[Serializable]
public class OrderRule
{
    public string ingredientId;
    [Tooltip("ต้องใส่สิ่งเหล่านี้ลงภาชนะแล้ว (ครบทุกตัว)")]
    public string[] requiresAdded = Array.Empty<string>();
    [Tooltip("ต้องคน (stirId) นี้แล้ว")]
    public string requiresStir;
    [Tooltip("วัตถุดิบหลักต้องสุกแล้ว")]
    public bool requiresMainCooked;
    [Tooltip("น้ำในหม้อต้องเดือดแล้ว (เมนูต้ม)")]
    public bool requiresBoiling;
    [Tooltip("ต้องปิดเตาก่อนใส่ (เช่น มะนาวในต้มยำ ใส่ตอนเดือดแล้วขม)")]
    public bool requiresHeatOff;
    [Tooltip("ห้ามมีสิ่งเหล่านี้ในภาชนะแล้ว (ตัวใดตัวหนึ่ง)")]
    public string[] forbidsAdded = Array.Empty<string>();
    public string mistakeCode;
    [Tooltip("ข้อความเตือนตอนใส่ผิด (ป้ายสถานี)")]
    public string warnThai;
    [Tooltip("ข้อความในหน้าผล Rubric")]
    public string rubricThai;
}

// จุดที่ต้องคน: คนครั้งแรกหลังใส่ afterAdded ครบ = ทำจุดนี้ (นับในตารางภารกิจด้วย stirId สถานะ Cooking)
[Serializable]
public class StirCheckpoint
{
    public string stirId;
    public string logValue;              // ค่าใน ActionLog FoodStirred เช่น aromatics / final
    public string[] afterAdded = Array.Empty<string>();
    public string doneMessageThai;       // ป้ายตอนคนสำเร็จ
    public string missingRubricThai;     // หน้าผลเมื่อไม่ได้คน
}

// ข้อมูลเมนูหนึ่งเมนู: ขั้นภารกิจ (วัตถุดิบ/จำนวน/สถานะ) + กฎลำดับ + จุดคน + วัตถุดิบหลักที่ต้องสุก
// ระบบเกม (ObjectiveManager, PanContents, CookingOrder, RubricScorer) อ่านจากที่นี่ ไม่ผูกกับเมนูใดเมนูหนึ่ง
// สร้าง/อัปเดต asset ด้วย Tools › Cookverse Thai › M1 › Create Menu Assets (ค่าจาก MenuCatalog)
[CreateAssetMenu(menuName = "Cookverse Thai/Menu", fileName = "Menu_")]
public class MenuDefinition : ScriptableObject
{
    public string menuId;
    public string nameThai;
    [TextArea] public string descriptionThai;
    [Range(1, 3)] public int difficulty = 1;
    public CookVessel vessel = CookVessel.Pan;
    [Tooltip("วัตถุดิบที่ต้องสุก (ใช้ตัดสินดิบ/สุก/ไหม้) เช่น chicken, shrimp")]
    public string mainIngredientId;
    [Tooltip("วัตถุดิบที่ห้ามล้างใต้ก๊อก (เนื้อสัตว์ดิบ) · ล้าง = หักคะแนนความปลอดภัยอาหาร")]
    public string[] noWashIds = Array.Empty<string>();
    [Tooltip("ความร้อนสะสมที่วัตถุดิบหลักสุก / ไหม้(สุกเกิน) · ไก่ 100/160 · กุ้ง 50/120")]
    public float mainCookedAtHeat = 100f;
    public float mainBurnedAtHeat = 160f;
    [Tooltip("คำเรียกตอนสุกเกิน เช่น ไหม้ / สุกเกินจนแข็ง")]
    public string burnedWordThai = "ไหม้";
    [Tooltip("เครื่องปรุงที่เทลงถ้วยผสมก่อน (กะเพรา: ซอส 3 อย่าง) · ว่าง = เทลงภาชนะตรง")]
    public string[] bowlSauceIds = Array.Empty<string>();
    [Tooltip("เมนูต้ม: วัตถุดิบที่เป็นน้ำ (เช่น water) · ใส่แล้วเปิดไฟจนเดือด")]
    public string boilIngredientId;
    [Tooltip("รายการในตารางภารกิจที่นับเมื่อน้ำเดือด (สถานะ Cooking)")]
    public string boilMarkerId;
    public float boilAtHeat = 30f;
    public List<MissionStepDefinition> steps = new();
    public List<OrderRule> orderRules = new();
    public List<StirCheckpoint> stirCheckpoints = new();

    // สำเนาขั้นภารกิจสำหรับรอบเล่น (นับจำนวนแยกจาก asset)
    public List<MissionStepDefinition> CloneSteps()
    {
        var copy = new List<MissionStepDefinition>();
        foreach (MissionStepDefinition s in steps)
        {
            MissionStepDefinition c = JsonUtility.FromJson<MissionStepDefinition>(JsonUtility.ToJson(s));
            if (c.requirements != null) c.requirements.ForEach(r => r.currentCount = 0);
            copy.Add(c);
        }
        return copy;
    }

    private bool IsStirId(string id) => stirCheckpoints.Any(s => s.stirId == id);

    private IEnumerable<IngredientRequirement> Reqs(IngredientState state) =>
        steps.Where(s => s.requirements != null).SelectMany(s => s.requirements).Where(r => r.targetState == state);

    // วัตถุดิบที่ต้องอยู่ในจาน = ทุกตัวที่ต้องใส่ลงภาชนะหรือต้องสุก เรียงตามขั้น (ไม่รวมการคน)
    public string[] DishIngredientIds =>
        steps.Where(s => s.requirements != null).SelectMany(s => s.requirements)
            .Where(r => r.targetState == IngredientState.AddedToPan || r.targetState == IngredientState.Cooked)
            .Select(r => r.ingredientId).Where(id => !IsStirId(id)).Distinct().ToArray();

    public string[] WashIds => Reqs(IngredientState.Washed).Select(r => r.ingredientId).Distinct().ToArray();
    public string[] CutIds => Reqs(IngredientState.Cut).Select(r => r.ingredientId).Distinct().ToArray();

    public static bool IsCookState(IngredientState s) =>
        s == IngredientState.AddedToPan || s == IngredientState.Cooking || s == IngredientState.Cooked;

    public bool IsCookStep(string stepId)
    {
        MissionStepDefinition step = steps.FirstOrDefault(s => s.stepId == stepId);
        return step != null && step.requirements != null && step.requirements.Any(r => IsCookState(r.targetState));
    }

    // ขั้นปรุงขั้นสุดท้าย (ปรุงครบ = ตักได้แม้ยังไม่ปิดเตา)
    public string FinalCookStepId => steps.LastOrDefault(s => IsCookStep(s.stepId))?.stepId;

    // สถานะเตรียมที่ต้องถึงก่อนใส่ภาชนะ: Cut ถ้าเมนูให้หั่น · Washed ถ้าให้ล้าง · ไม่งั้น Collected
    public IngredientState PrepStateFor(string id)
    {
        if (CutIds.Contains(id)) return IngredientState.Cut;
        if (WashIds.Contains(id)) return IngredientState.Washed;
        return IngredientState.Collected;
    }

    public string DisplayName(string id)
    {
        IngredientRequirement r = steps.Where(s => s.requirements != null).SelectMany(s => s.requirements)
            .FirstOrDefault(x => x.Matches(id));
        return r != null && !string.IsNullOrEmpty(r.displayNameThai) ? r.displayNameThai : null;
    }

    public string VesselThai => vessel == CookVessel.Pot ? "หม้อ" : "กระทะ";
    public bool UsesBowl(string id) => bowlSauceIds != null && bowlSauceIds.Contains(id);

    public OrderRule RuleFor(string code) => orderRules.FirstOrDefault(r => r.mistakeCode == code);
}
