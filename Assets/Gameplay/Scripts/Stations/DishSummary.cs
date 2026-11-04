using System.Collections.Generic;

// สรุปอาหารในจาน ณ ตอนตักจากกระทะ · ใช้บันทึก DishPlated/DishServed และให้ป้าแสงพูดตามผลจริง
// คะแนนจริงคิดจาก ActionLog ตาม Rubric (G5) ไม่ใช่จากคลาสนี้
public class DishSummary
{
    public int HeatLevel;
    public IngredientState ChickenState = IngredientState.Raw;
    public int OrderMistakes;
    public readonly List<string> Missing = new();

    public bool ChickenCooked => ChickenState == IngredientState.Cooked || ChickenState == IngredientState.Served;
    public bool ChickenBurned => ChickenState == IngredientState.Burned;
    public bool IsCorrect => HeatLevel == 0 && ChickenCooked && Missing.Count == 0;

    public string ToLogValue()
    {
        return $"heat={HeatLevel};chicken={ChickenState};missing={string.Join(",", Missing)}";
    }

    // ข้อสังเกตสั้น ๆ สำหรับบทพูดป้าแสง (ไม่มีข้อผิด = ว่าง)
    public string IssuesThai()
    {
        var issues = new List<string>();

        if (ChickenBurned) issues.Add("ไก่ไหม้");
        else if (!ChickenCooked) issues.Add("ไก่ยังไม่สุก");
        if (HeatLevel > 0) issues.Add("ยังไม่ได้ปิดเตา");

        var missingThai = new List<string>();
        foreach (string id in Missing)
        {
            string name = ThaiName(id);
            if (!string.IsNullOrEmpty(name)) missingThai.Add(name);
        }

        if (missingThai.Count > 0) issues.Add("ขาด" + string.Join(" ", missingThai));
        return string.Join(" · ", issues);
    }

    private static string ThaiName(string id)
    {
        return id switch
        {
            ObjectiveManager.CookingOilIngredientId => "น้ำมัน",
            ObjectiveManager.GarlicIngredientId => "กระเทียม",
            ObjectiveManager.ChiliIngredientId => "พริก",
            ObjectiveManager.ChickenIngredientId => "ไก่",
            ObjectiveManager.OysterSauceIngredientId => "ซอสหอยนางรม",
            ObjectiveManager.FishSauceIngredientId => "น้ำปลา",
            ObjectiveManager.SugarIngredientId => "น้ำตาล",
            ObjectiveManager.KrapowLeafIngredientId => "ใบกะเพรา",
            ObjectiveManager.StirAromaticsId => "การคนเครื่องหอม",
            ObjectiveManager.StirFinalId => "การคนให้เข้ากัน",
            _ => id
        };
    }
}
