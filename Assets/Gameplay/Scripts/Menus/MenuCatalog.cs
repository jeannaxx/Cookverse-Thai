using System.Collections.Generic;
using State = IngredientState;
using UnityEngine;

// ค่าตั้งต้นของทุกเมนู (โค้ดเป็นแหล่งเดียว) → Tools › Cookverse Thai › M1 › Create Menu Assets เขียนเป็นไฟล์ asset
// ระหว่างเล่น ถ้าฉากยังไม่ได้ตั้ง MenuDefinition ให้ ObjectiveManager ใช้ KrapowChicken() จากที่นี่
public static class MenuCatalog
{
    public const string KrapowChickenId = "krapow_chicken";
    public const string TomYumKungId = "tom_yum_kung";

    // วัตถุดิบเมนูต้ม (id ผักสวนตรงกับ VeggiePicks.json ที่อยู่ในฉากแล้ว)
    public const string LemongrassId = "lemongrass";
    public const string GalangalId = "galangal";
    public const string KaffirLeafId = "kaffir_lime_leaf";
    public const string LimeId = "lime";
    public const string CorianderId = "coriander";
    public const string ShrimpId = "shrimp";
    public const string StrawMushroomId = "straw_mushroom";
    public const string WaterId = "water";
    public const string NamPrikPaoId = "nam_prik_pao";
    public const string EvaporatedMilkId = "evaporated_milk";
    public const string WaterBoilingId = "water_boiling";

    // ขั้นปรุงของเมนูต้ม (ขั้นเก็บ/ล้าง/หั่น/เสิร์ฟ ใช้ id เดิม)
    public const string CookBoilStepId = "cook_boil";
    public const string CookHerbsStepId = "cook_herbs";
    public const string CookShrimpStepId = "cook_shrimp";
    public const string CookSoupSeasonStepId = "cook_soup_season";

    // รหัสข้อผิดลำดับของต้มยำ
    public const string HerbsBeforeBoil = "herbs_before_boil";
    public const string ShrimpBeforeBoil = "shrimp_before_boil";
    public const string ShrimpBeforeHerbs = "shrimp_before_herbs";
    public const string SoupSeasonBeforeShrimpCooked = "soup_season_before_shrimp_cooked";
    public const string LimeWhileBoiling = "lime_while_boiling";
    public const string ChiliWhileBoiling = "chili_while_boiling";
    public const string CorianderTooEarly = "coriander_too_early";

    private static MenuDefinition krapow;
    private static MenuDefinition tomYum;

    public static IReadOnlyList<MenuDefinition> All() => new[] { KrapowChicken(), TomYumKung() };

    public static MenuDefinition Find(string menuId) =>
        menuId == TomYumKungId ? TomYumKung() : KrapowChicken();

    // ผัดกะเพราไก่ (เมนูต้นแบบ) — ขั้นภารกิจเดิมจาก ObjectiveManager.CreateDefaultSteps + กฎลำดับเดิมของ CookingOrder
    public static MenuDefinition KrapowChicken()
    {
        if (krapow != null) return krapow;

        var m = ScriptableObject.CreateInstance<MenuDefinition>();
        m.name = "Menu_KrapowChicken";
        m.menuId = KrapowChickenId;
        m.nameThai = "ผัดกะเพราไก่";
        m.descriptionThai = "ผัดเครื่องหอม ใส่ไก่จนสุก ปรุงรส แล้วใส่ใบกะเพรา";
        m.difficulty = 1;
        m.vessel = CookVessel.Pan;
        m.mainIngredientId = ObjectiveManager.ChickenIngredientId;
        m.noWashIds = new[] { ObjectiveManager.ChickenIngredientId };
        m.bowlSauceIds = new[]
        {
            ObjectiveManager.OysterSauceIngredientId, ObjectiveManager.FishSauceIngredientId, ObjectiveManager.SugarIngredientId
        };
        m.steps = ObjectiveManager.CreateDefaultSteps();

        const string oil = ObjectiveManager.CookingOilIngredientId, garlic = ObjectiveManager.GarlicIngredientId,
            chili = ObjectiveManager.ChiliIngredientId, chicken = ObjectiveManager.ChickenIngredientId,
            oyster = ObjectiveManager.OysterSauceIngredientId, fish = ObjectiveManager.FishSauceIngredientId,
            sugar = ObjectiveManager.SugarIngredientId, krapowLeaf = ObjectiveManager.KrapowLeafIngredientId;

        OrderRule Rule(string id, string code, string warn, string rubric, string[] needs = null, string stir = null,
                       bool cooked = false, string[] forbids = null) => new()
        {
            ingredientId = id, mistakeCode = code, warnThai = warn, rubricThai = rubric,
            requiresAdded = needs ?? new string[0], requiresStir = stir, requiresMainCooked = cooked,
            forbidsAdded = forbids ?? new string[0],
        };

        m.orderRules = new List<OrderRule>();
        foreach (string aromatic in new[] { garlic, chili })
        {
            m.orderRules.Add(Rule(aromatic, CookingOrder.AromaticsBeforeOil, "ควรใส่น้ำมันก่อนเครื่องหอม",
                "ใส่กระเทียม/พริกก่อนน้ำมัน", needs: new[] { oil }));
            m.orderRules.Add(Rule(aromatic, CookingOrder.AromaticsAfterChicken, "ควรผัดกระเทียมกับพริกก่อนใส่ไก่",
                "ใส่กระเทียม/พริกหลังไก่ (ควรผัดเครื่องหอมก่อน)", forbids: new[] { chicken }));
        }
        m.orderRules.Add(Rule(chicken, CookingOrder.ChickenBeforeOil, "ใส่ไก่ก่อนน้ำมัน ควรใส่น้ำมันก่อน",
            "ใส่ไก่ก่อนน้ำมัน", needs: new[] { oil }));
        m.orderRules.Add(Rule(chicken, CookingOrder.ChickenBeforeAromatics, "ควรใส่กระเทียมและพริกก่อนไก่",
            "ใส่ไก่ก่อนกระเทียมและพริก", needs: new[] { garlic, chili }));
        m.orderRules.Add(Rule(chicken, CookingOrder.ChickenBeforeStir, "ควรคนเครื่องหอมให้หอมก่อนใส่ไก่",
            "ใส่ไก่ก่อนคนเครื่องหอมให้หอม", stir: ObjectiveManager.StirAromaticsId));
        foreach (string sauce in new[] { oyster, fish, sugar })
        {
            m.orderRules.Add(Rule(sauce, CookingOrder.SauceBeforeChickenCooked, "ควรรอให้ไก่สุกก่อนปรุงรส",
                "ปรุงรสก่อนไก่สุก", cooked: true));
        }
        m.orderRules.Add(Rule(krapowLeaf, CookingOrder.KrapowBeforeChickenCooked, "ใส่ใบกะเพราก่อนไก่สุก ควรใส่เป็นลำดับท้าย",
            "ใส่กะเพราก่อนไก่สุก", cooked: true));
        m.orderRules.Add(Rule(krapowLeaf, CookingOrder.KrapowBeforeSauce, "ควรปรุงรสให้ครบก่อนใส่ใบกะเพรา",
            "ใส่กะเพราก่อนปรุงรส (กะเพราใส่ท้ายสุด)", needs: new[] { oyster, fish, sugar }));

        m.stirCheckpoints = new List<StirCheckpoint>
        {
            new() { stirId = ObjectiveManager.StirAromaticsId, logValue = "aromatics", afterAdded = new[] { garlic, chili },
                    doneMessageThai = "คนเครื่องหอมแล้ว ใส่ไก่ได้เลย", missingRubricThai = "ไม่ได้คนเครื่องหอมก่อนใส่ไก่" },
            new() { stirId = ObjectiveManager.StirFinalId, logValue = "final", afterAdded = new[] { krapowLeaf },
                    doneMessageThai = "คนให้เข้ากันแล้ว ปิดเตาได้เลย", missingRubricThai = "ไม่ได้คนให้เข้ากันหลังใส่กะเพรา" },
        };

        krapow = m;
        return m;
    }

    private static IngredientRequirement Req(string id, string thai, string unit, int count,
                                             IngredientState state = IngredientState.Collected) =>
        new(id, string.Empty, thai, unit, count, state);

    private static OrderRule SoupRule(string id, string code, string warn, string rubric, string[] needs = null,
                                  bool cooked = false, bool boiling = false, bool heatOff = false,
                                  string[] forbids = null) => new()
    {
        ingredientId = id, mistakeCode = code, warnThai = warn, rubricThai = rubric,
        requiresAdded = needs ?? new string[0], requiresMainCooked = cooked, requiresBoiling = boiling,
        requiresHeatOff = heatOff, forbidsAdded = forbids ?? new string[0],
    };

    // ต้มยำกุ้งน้ำข้น (เมนูหม้อแรก) · ผักจากสวน · กุ้ง/เห็ดจากตู้เย็น · น้ำ/น้ำพริกเผา/นมข้นจืด/น้ำปลาจากชั้นเครื่องปรุง
    // ต้มน้ำจนเดือด → ตะไคร้ ข่า ใบมะกรูด → เห็ด → กุ้งจนสุก → น้ำปลา น้ำพริกเผา นมข้นจืด → ปิดไฟ → มะนาว พริก → ผักชี
    public static MenuDefinition TomYumKung()
    {
        if (tomYum != null) return tomYum;

        const State W = State.Washed, X = State.Cut, A = State.AddedToPan;
        var m = ScriptableObject.CreateInstance<MenuDefinition>();
        m.name = "Menu_TomYumKung";
        m.menuId = TomYumKungId;
        m.nameThai = "ต้มยำกุ้งน้ำข้น";
        m.descriptionThai = "ต้มน้ำให้เดือด ใส่สมุนไพร เห็ด กุ้ง ปรุงรส ปิดไฟแล้วค่อยบีบมะนาว";
        m.difficulty = 2;
        m.vessel = CookVessel.Pot;
        m.mainIngredientId = ShrimpId;
        m.mainCookedAtHeat = 50f;     // กุ้งสุกเร็ว (ไฟกลาง ~20 วิ)
        m.mainBurnedAtHeat = 120f;    // ต้มนานเกินจนกุ้งแข็ง
        m.burnedWordThai = "สุกเกินจนแข็ง";
        m.noWashIds = new string[0];
        m.bowlSauceIds = new string[0];
        m.boilIngredientId = WaterId;
        m.boilMarkerId = WaterBoilingId;
        m.boilAtHeat = 30f;           // ไฟกลาง ~12 วิ · ไฟแรง ~6 วิ

        const string chili = ObjectiveManager.ChiliIngredientId, fish = ObjectiveManager.FishSauceIngredientId;

        m.steps = new List<MissionStepDefinition>
        {
            new(ObjectiveManager.AcceptMissionStepId, "คุยกับป้าแสงเพื่อรับภารกิจ"),
            new(ObjectiveManager.CollectGardenStepId, "เก็บสมุนไพรจากสวน",
                Req(LemongrassId, "ตะไคร้", "ต้น", 2), Req(GalangalId, "ข่า", "แง่ง", 1),
                Req(KaffirLeafId, "ใบมะกรูด", "ใบ", 3), Req(chili, "พริก", "เม็ด", 5),
                Req(LimeId, "มะนาว", "ลูก", 1), Req(CorianderId, "ผักชี", "ต้น", 1)),
            new(ObjectiveManager.CollectKitchenStepId, "หยิบกุ้งและเห็ดฟางในตู้เย็น",
                Req(ShrimpId, "กุ้ง", "ตัว", 5), Req(StrawMushroomId, "เห็ดฟาง", "ดอก", 4)),
            // ไม่บังคับล้างกุ้ง (ล้างแล้วไม่หักคะแนน)
            new(ObjectiveManager.WashStepId, "ล้างผักและเห็ดที่อ่างล้าง",
                Req(LemongrassId, "ตะไคร้", "ต้น", 2, W), Req(GalangalId, "ข่า", "แง่ง", 1, W),
                Req(KaffirLeafId, "ใบมะกรูด", "ใบ", 3, W), Req(chili, "พริก", "เม็ด", 5, W),
                Req(LimeId, "มะนาว", "ลูก", 1, W), Req(CorianderId, "ผักชี", "ต้น", 1, W),
                Req(StrawMushroomId, "เห็ดฟาง", "ดอก", 4, W)),
            new(ObjectiveManager.PrepareStepId, "หั่น/ทุบ/ฉีก วัตถุดิบบนเขียง",
                Req(LemongrassId, "ตะไคร้", "ต้น", 2, X), Req(GalangalId, "ข่า", "แง่ง", 1, X),
                Req(KaffirLeafId, "ใบมะกรูด", "ใบ", 3, X), Req(chili, "พริก", "เม็ด", 5, X),
                Req(StrawMushroomId, "เห็ดฟาง", "ดอก", 4, X), Req(LimeId, "มะนาว", "ลูก", 1, X)),
            new(CookBoilStepId, "เติมน้ำลงหม้อ เปิดไฟจนเดือด",
                Req(WaterId, "น้ำ", "ครั้ง", 1, A), Req(WaterBoilingId, "รอน้ำเดือด", "ครั้ง", 1, State.Cooking)),
            new(CookHerbsStepId, "ใส่ตะไคร้ ข่า ใบมะกรูด และเห็ด",
                Req(LemongrassId, "ตะไคร้", "ต้น", 2, A), Req(GalangalId, "ข่า", "แง่ง", 1, A),
                Req(KaffirLeafId, "ใบมะกรูด", "ใบ", 3, A), Req(StrawMushroomId, "เห็ดฟาง", "ดอก", 4, A)),
            new(CookShrimpStepId, "ใส่กุ้ง ต้มจนสุก", Req(ShrimpId, "กุ้ง", "ตัว", 5, State.Cooked)),
            new(CookSoupSeasonStepId, "ปรุงรส ปิดไฟ แล้วใส่มะนาว พริก ผักชี",
                Req(fish, "น้ำปลา", "ครั้ง", 1, A), Req(NamPrikPaoId, "น้ำพริกเผา", "ครั้ง", 1, A),
                Req(EvaporatedMilkId, "นมข้นจืด", "ครั้ง", 1, A), Req(LimeId, "มะนาว", "ลูก", 1, A),
                Req(chili, "พริก", "เม็ด", 5, A), Req(CorianderId, "ผักชี", "ต้น", 1, A)),
            new(ObjectiveManager.ServePlateStepId, "ปิดเตา แล้วตักใส่ชามที่โต๊ะจัดจาน",
                Req(ObjectiveManager.DishPlatedId, "ตักต้มยำใส่ชาม", "ชาม", 1, State.Served)),
            new(ObjectiveManager.ServeNpcStepId, "นำชามไปเสิร์ฟป้าแสงที่โต๊ะทานข้าว",
                Req(ObjectiveManager.DishServedId, "เสิร์ฟป้าแสง", "ชาม", 1, State.Served)),
        };

        string[] herbs = { LemongrassId, GalangalId, KaffirLeafId };
        m.orderRules = new List<OrderRule>();
        foreach (string herb in herbs)
        {
            m.orderRules.Add(SoupRule(herb, HerbsBeforeBoil, "ควรรอให้น้ำเดือดก่อนใส่สมุนไพร",
                "ใส่สมุนไพรก่อนน้ำเดือด (กลิ่นไม่ออก)", boiling: true));
        }
        m.orderRules.Add(SoupRule(StrawMushroomId, HerbsBeforeBoil, "ควรรอให้น้ำเดือดก่อนใส่เห็ด",
            "ใส่เห็ดก่อนน้ำเดือด", boiling: true));
        m.orderRules.Add(SoupRule(ShrimpId, ShrimpBeforeBoil, "ควรรอให้น้ำเดือดก่อนใส่กุ้ง",
            "ใส่กุ้งก่อนน้ำเดือด", boiling: true));
        m.orderRules.Add(SoupRule(ShrimpId, ShrimpBeforeHerbs, "ควรต้มสมุนไพรให้หอมก่อนใส่กุ้ง",
            "ใส่กุ้งก่อนสมุนไพร", needs: herbs));
        foreach (string s in new[] { fish, NamPrikPaoId, EvaporatedMilkId })
        {
            m.orderRules.Add(SoupRule(s, SoupSeasonBeforeShrimpCooked, "ควรรอให้กุ้งสุกก่อนปรุงรส",
                "ปรุงรสก่อนกุ้งสุก", cooked: true));
        }
        m.orderRules.Add(SoupRule(LimeId, LimeWhileBoiling, "ปิดไฟก่อนบีบมะนาว (ใส่ตอนเดือดน้ำแกงจะขม)",
            "บีบมะนาวตอนยังเปิดไฟ (น้ำแกงขม)", heatOff: true));
        m.orderRules.Add(SoupRule(chili, ChiliWhileBoiling, "ปิดไฟก่อนใส่พริก",
            "ใส่พริกตอนยังเปิดไฟ", heatOff: true));
        m.orderRules.Add(SoupRule(CorianderId, CorianderTooEarly, "ผักชีใส่ท้ายสุด หลังปรุงรสครบ",
            "ใส่ผักชีก่อนปรุงรสครบ", needs: new[] { fish, NamPrikPaoId, EvaporatedMilkId, LimeId }));

        m.stirCheckpoints = new List<StirCheckpoint>();
        tomYum = m;
        return m;
    }
}
