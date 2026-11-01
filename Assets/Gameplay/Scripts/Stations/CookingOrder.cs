using System.Collections.Generic;

// กฎลำดับการปรุงผัดกะเพราไก่ (GAMEPLAY_CONTENT_V1 "ลำดับการปรุงที่ระบบกฎตรวจ")
// น้ำมัน → กระเทียม+พริก → คน → ไก่ → (ไก่สุก) → ซอสหอยนางรม น้ำปลา น้ำตาล → ใบกะเพรา → คน
// ใส่ผิดลำดับยังใส่ได้ Evaluate คืนรหัสข้อผิดพลาดไว้บันทึก ActionLog และคิดคะแนนตาม Rubric
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

    private static readonly string[] Sauces =
    {
        ObjectiveManager.OysterSauceIngredientId,
        ObjectiveManager.FishSauceIngredientId,
        ObjectiveManager.SugarIngredientId
    };

    private readonly HashSet<string> added = new();

    public bool AromaticsStirred { get; set; }
    public bool ChickenCooked { get; set; }
    public bool FinalStirred { get; set; }

    public IReadOnlyCollection<string> Added => added;
    public bool Has(string ingredientId) => added.Contains(ingredientId);

    public bool AromaticsInPan =>
        Has(ObjectiveManager.GarlicIngredientId) && Has(ObjectiveManager.ChiliIngredientId);

    public bool AllSaucesAdded
    {
        get
        {
            foreach (string sauce in Sauces)
            {
                if (!Has(sauce))
                {
                    return false;
                }
            }

            return true;
        }
    }

    // เรียกก่อน MarkAdded · คืน null ถ้าลำดับถูก
    public string Evaluate(string ingredientId)
    {
        switch (ingredientId)
        {
            case ObjectiveManager.GarlicIngredientId:
            case ObjectiveManager.ChiliIngredientId:
                if (!Has(ObjectiveManager.CookingOilIngredientId)) return AromaticsBeforeOil;
                if (Has(ObjectiveManager.ChickenIngredientId)) return AromaticsAfterChicken;
                return null;

            case ObjectiveManager.ChickenIngredientId:
                if (!Has(ObjectiveManager.CookingOilIngredientId)) return ChickenBeforeOil;
                if (!AromaticsInPan) return ChickenBeforeAromatics;
                if (!AromaticsStirred) return ChickenBeforeStir;
                return null;

            case ObjectiveManager.OysterSauceIngredientId:
            case ObjectiveManager.FishSauceIngredientId:
            case ObjectiveManager.SugarIngredientId:
                return ChickenCooked ? null : SauceBeforeChickenCooked;

            case ObjectiveManager.KrapowLeafIngredientId:
                if (!ChickenCooked) return KrapowBeforeChickenCooked;
                if (!AllSaucesAdded) return KrapowBeforeSauce;
                return null;

            default:
                return null;
        }
    }

    public void MarkAdded(string ingredientId)
    {
        added.Add(ingredientId);
    }

    public void Clear()
    {
        added.Clear();
        AromaticsStirred = false;
        ChickenCooked = false;
        FinalStirred = false;
    }

    public static string DescribeThai(string code)
    {
        return code switch
        {
            AromaticsBeforeOil => "ควรใส่น้ำมันก่อนเครื่องหอม",
            AromaticsAfterChicken => "ควรผัดกระเทียมกับพริกก่อนใส่ไก่",
            ChickenBeforeOil => "ใส่ไก่ก่อนน้ำมัน ควรใส่น้ำมันก่อน",
            ChickenBeforeAromatics => "ควรใส่กระเทียมและพริกก่อนไก่",
            ChickenBeforeStir => "ควรคนเครื่องหอมให้หอมก่อนใส่ไก่",
            SauceBeforeChickenCooked => "ควรรอให้ไก่สุกก่อนปรุงรส",
            KrapowBeforeChickenCooked => "ใส่ใบกะเพราก่อนไก่สุก ควรใส่เป็นลำดับท้าย",
            KrapowBeforeSauce => "ควรปรุงรสให้ครบก่อนใส่ใบกะเพรา",
            Duplicate => "ใส่ซ้ำ",
            _ => code
        };
    }
}
