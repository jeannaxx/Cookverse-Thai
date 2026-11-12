using System.Collections.Generic;
using System.Linq;

// ตรวจลำดับการปรุงตามกฎของเมนู (MenuDefinition.orderRules) · ใส่ผิดลำดับยังใส่ได้
// Evaluate คืนรหัสข้อผิดพลาดไว้บันทึก ActionLog และคิดคะแนน Rubric · คืน null ถ้าลำดับถูก
// รหัสด้านล่างเป็นของผัดกะเพราไก่ (คงชื่อเดิมให้ชุดทดสอบ/บันทึกเดิมใช้ได้) · เมนูอื่นตั้งรหัสเองในข้อมูลเมนู
public class CookingOrder
{
    public const string AromaticsBeforeOil = "aromatics_before_oil";
    public const string AromaticsAfterChicken = "aromatics_after_chicken";
    public const string ChickenBeforeOil = "chicken_before_oil";
    public const string ChickenBeforeAromatics = "chicken_before_aromatics";
    public const string ChickenBeforeStir = "chicken_before_stir";
    public const string SauceBeforeChickenCooked = "sauce_before_chicken_cooked";
    public const string KrapowBeforeChickenCooked = "krapow_before_chicken_cooked";
    public const string KrapowBeforeSauce = "krapow_before_sauce";
    public const string Duplicate = "duplicate";

    private readonly HashSet<string> added = new();
    private readonly HashSet<string> stirsDone = new();
    private MenuDefinition menu;

    public MenuDefinition Menu
    {
        get => menu != null ? menu : MenuCatalog.KrapowChicken();
        set => menu = value;
    }

    public IReadOnlyCollection<string> Added => added;
    public bool Has(string ingredientId) => added.Contains(ingredientId);
    public bool StirDone(string stirId) => stirsDone.Contains(stirId);
    public void MarkStir(string stirId) => stirsDone.Add(stirId);

    // วัตถุดิบหลักของเมนู (เช่นไก่) สุกแล้ว
    public bool MainCooked { get; set; }
    // เมนูต้ม: น้ำเดือดแล้ว · ระดับไฟตอนใส่ (PanContents ตั้งก่อนเรียก Evaluate)
    public bool Boiling { get; set; }
    public int HeatLevel { get; set; }
    public bool ChickenCooked { get => MainCooked; set => MainCooked = value; }   // ชื่อเดิม

    public void MarkAdded(string ingredientId) => added.Add(ingredientId);

    // เรียกก่อน MarkAdded
    public string Evaluate(string ingredientId)
    {
        foreach (OrderRule r in Menu.orderRules.Where(r => r.ingredientId == ingredientId))
        {
            if (r.forbidsAdded != null && r.forbidsAdded.Any(Has)) return r.mistakeCode;
            if (r.requiresAdded != null && !r.requiresAdded.All(Has)) return r.mistakeCode;
            if (!string.IsNullOrEmpty(r.requiresStir) && !StirDone(r.requiresStir)) return r.mistakeCode;
            if (r.requiresMainCooked && !MainCooked) return r.mistakeCode;
            if (r.requiresBoiling && !Boiling) return r.mistakeCode;
            if (r.requiresHeatOff && HeatLevel > 0) return r.mistakeCode;
        }
        return null;
    }

    // จุดคนถัดไปที่ทำได้ตอนนี้ (ยังไม่ได้คน และใส่ของที่ต้องมีครบแล้ว) · null = คนเฉย ๆ
    public StirCheckpoint NextStir() =>
        Menu.stirCheckpoints.FirstOrDefault(s => !StirDone(s.stirId) && (s.afterAdded == null || s.afterAdded.All(Has)));

    public IEnumerable<StirCheckpoint> MissingStirs() => Menu.stirCheckpoints.Where(s => !StirDone(s.stirId));

    public void Clear()
    {
        added.Clear();
        stirsDone.Clear();
        MainCooked = false;
        Boiling = false;
        HeatLevel = 0;
    }

    public string Describe(string code)
    {
        OrderRule rule = Menu.RuleFor(code);
        return rule != null && !string.IsNullOrEmpty(rule.warnThai) ? rule.warnThai : DescribeThai(code);
    }

    public static string DescribeThai(string code) => code == Duplicate ? "ใส่ซ้ำ" : code;
}
