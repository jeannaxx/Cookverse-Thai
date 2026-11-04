#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// ทดสอบขั้นปรุง: ลำดับใส่ของ เครื่องปรุงจากขวด การคน ไก่สุก และ Reset
// รันจากเมนู Tools > Cookverse Thai > M1 > Test Cooking Round (Play Mode) ระหว่าง Play
public sealed class M1CookingSmokeTest : MonoBehaviour
{
    private readonly List<string> results = new();
    private bool passed = true;
    private ObjectiveManager objective;
    private PanContents pan;
    private GasStove stove;
    private SpatulaStirrer spatula;
    private SauceBowl bowl;
    private Dictionary<string, SeasoningBottle> bottles;
    private IngredientIdentity[] ingredients;
    private Vector3 panCenter;
    private int dropIndex;

    private void Check(bool condition, string label)
    {
        results.Add((condition ? "ผ่าน: " : "ไม่ผ่าน: ") + label);
        passed &= condition;
    }

    private IEnumerator Start()
    {
        IEnumerator routine = Run();
        while (true)
        {
            bool next;
            try { next = routine.MoveNext(); }
            catch (Exception e) { Check(false, "ข้อผิดพลาด " + e); break; }
            if (!next) break;
            yield return routine.Current;
        }

        Time.timeScale = 1f;
        if (stove != null) stove.SetLevel(0);
        if (objective != null) objective.ResetObjectives();
        results.Add(passed ? "ผลรวม: ผ่าน" : "ผลรวม: ไม่ผ่าน");
        File.WriteAllLines("/private/tmp/m1-cooking-test.txt", results);
        Debug.Log("[ทดสอบปรุง M1]\n" + string.Join("\n", results));
        Destroy(gameObject);
    }

    private static IEnumerator Tick()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(0.15f);
    }

    private IEnumerator Run()
    {
        objective = FindAnyObjectByType<ObjectiveManager>();
        pan = FindAnyObjectByType<PanContents>();
        stove = FindAnyObjectByType<GasStove>();
        spatula = FindAnyObjectByType<SpatulaStirrer>();
        bowl = FindAnyObjectByType<SauceBowl>();
        bottles = FindObjectsByType<SeasoningBottle>(FindObjectsInactive.Exclude)
            .ToDictionary(b => b.IngredientId);

        Check(objective != null && pan != null && stove != null, "มี ObjectiveManager กระทะ และเตา");
        Check(bottles.Count == 4 && spatula != null, "มีขวดเครื่องปรุง 4 ขวดและตะหลิว (รัน Setup Pan Cooking แล้ว)");
        Check(bowl != null, "มีถ้วยผสมซอส");
        if (objective == null || pan == null || stove == null || bottles.Count < 4 || spatula == null || bowl == null) yield break;
        Vector3 bowlStart = bowl.transform.position;
        DishPlate plate = FindAnyObjectByType<DishPlate>();
        ServeZone serve = FindAnyObjectByType<ServeZone>();
        Check(plate != null && serve != null, "มีจานบนโต๊ะจัดจานและจุดเสิร์ฟ");
        if (plate == null || serve == null) yield break;
        Vector3 plateStart = plate.transform.position;
        Check(objective.FindStep(ObjectiveManager.CookAromaticsStepId) != null &&
              objective.FindStep(ObjectiveManager.CookSeasonStepId) != null, "ตารางมีขั้นปรุง 3 ขั้น");

        Bounds panZone = pan.GetComponent<Collider>().bounds;
        panCenter = panZone.center;
        Vector3 oilStart = bottles[ObjectiveManager.CookingOilIngredientId].transform.position;

        // ขวดอยู่ชั้นเครื่องปรุงที่เดิม · ถ้วยผสมซอสอยู่บนโต๊ะเตรียม (SauceMixPoint)
        GameObject mixPoint = GameObject.Find("SauceMixPoint");
        Check(mixPoint != null && bowl != null &&
              Vector2.Distance(new Vector2(bowl.transform.position.x, bowl.transform.position.z),
                  new Vector2(mixPoint.transform.position.x, mixPoint.transform.position.z)) < 0.3f,
            "ถ้วยผสมซอสอยู่บนโต๊ะเตรียม");

        // ---------- รอบที่ 1: ทำถูกลำดับ ----------
        yield return PrepareToCook();
        Check(objective.CurrentStepId == ObjectiveManager.CookAromaticsStepId, "เตรียมครบแล้วถึงขั้นผัดเครื่องหอม");

        // ผลทดสอบแว่น v9: ชิ้นไก่หั่นใหญ่กว่ากระทะ → หลังหั่นต้องกว้างไม่เกินครึ่งกระทะ
        IngredientIdentity cutChicken = Find(ObjectiveManager.ChickenIngredientId).First();
        Bounds chickenBounds = cutChicken.GetComponent<Collider>().bounds;
        float chickenWidth = Mathf.Max(chickenBounds.size.x, chickenBounds.size.z);
        Check(chickenWidth <= panZone.size.x * 0.5f,
            $"ไก่หั่นแล้วกว้าง {chickenWidth * 100f:0} ซม. ไม่เกินครึ่งกระทะ ({panZone.size.x * 50f:0} ซม.)");
        int logStart = ActionLog.Entries.Count;
        stove.SetLevel(2);

        Check(Pour(ObjectiveManager.CookingOilIngredientId), "เทน้ำมันเหนือกระทะได้");

        // ผสมซอสลงถ้วยระหว่างผัดเครื่องหอม (ยังไม่ลงกระทะ จึงไม่ผิดลำดับ)
        Check(PourIntoBowl(ObjectiveManager.OysterSauceIngredientId) &&
              PourIntoBowl(ObjectiveManager.FishSauceIngredientId) &&
              PourIntoBowl(ObjectiveManager.SugarIngredientId) && bowl.Count == 3, "เทซอส 3 อย่างลงถ้วยครบ 3/3");
        Check(!PourIntoBowl(ObjectiveManager.OysterSauceIngredientId) && bowl.Count == 3, "เทซอสเดิมลงถ้วยซ้ำไม่นับ");
        Check(objective.GetCollectedCount(ObjectiveManager.CookSeasonStepId, ObjectiveManager.OysterSauceIngredientId) == 0,
            "ซอสในถ้วยยังไม่นับจนกว่าจะเทลงกระทะ");
        Check(!PourIntoBowl(ObjectiveManager.CookingOilIngredientId) && bowl.Count == 3, "น้ำมันไม่เทลงถ้วย");
        foreach (IngredientIdentity garlic in Find(ObjectiveManager.GarlicIngredientId)) yield return Drop(garlic);
        foreach (IngredientIdentity chili in Find(ObjectiveManager.ChiliIngredientId)) yield return Drop(chili);
        Check(objective.GetCollectedCount(ObjectiveManager.CookAromaticsStepId, ObjectiveManager.GarlicIngredientId) == 3 &&
              objective.GetCollectedCount(ObjectiveManager.CookAromaticsStepId, ObjectiveManager.ChiliIngredientId) == 3,
            "ใส่กระเทียม 3 พริก 3 นับครบ");
        Check(Stir(), "คนด้วยตะหลิวนับ 1 ครั้ง");
        Check(objective.CurrentStepId == ObjectiveManager.CookChickenStepId, "ผัดเครื่องหอมครบแล้วไปขั้นไก่");

        IngredientIdentity chicken = Find(ObjectiveManager.ChickenIngredientId).First();
        yield return Drop(chicken);
        Check(chicken.CurrentState == IngredientState.AddedToPan || chicken.CurrentState == IngredientState.Cooking,
            "ใส่ไก่ลงกระทะแล้ว");
        yield return CookUntil(chicken, IngredientState.Cooked);
        Check(chicken.CurrentState == IngredientState.Cooked, "ไก่สุกบนไฟแรง");
        Check(objective.CurrentStepId == ObjectiveManager.CookSeasonStepId, "ไก่สุกแล้วไปขั้นปรุงรส");

        PlaceBowl(panCenter + Vector3.up * 0.15f);
        Check(!bowl.UpdateTilt(80f, true, 0.1f), "เอียงถ้วยยังไม่ถึงเวลา ยังไม่เท");
        Check(bowl.UpdateTilt(80f, true, 0.2f) && bowl.Count == 0, "เอียงถ้วยเหนือกระทะ เทซอสลงทีเดียว ถ้วยว่าง");
        Check(objective.GetCollectedCount(ObjectiveManager.CookSeasonStepId, ObjectiveManager.OysterSauceIngredientId) == 1 &&
              objective.GetCollectedCount(ObjectiveManager.CookSeasonStepId, ObjectiveManager.FishSauceIngredientId) == 1 &&
              objective.GetCollectedCount(ObjectiveManager.CookSeasonStepId, ObjectiveManager.SugarIngredientId) == 1,
            "เทถ้วยครั้งเดียวนับซอสหอยนางรม น้ำปลา น้ำตาลครบ");
        Check(ActionLog.Entries.Skip(logStart).Count(e => e.action == ActionLog.Actions.SauceMixed && e.isCorrect) == 3,
            "บันทึก SauceMixed 3 ครั้งใน Log");
        Check(!Pour(ObjectiveManager.CookingOilIngredientId), "เทน้ำมันซ้ำไม่นับ");
        Check(MistakesSince(logStart).Any(e => e.value.Contains(CookingOrder.Duplicate)), "เทซ้ำบันทึกว่าใส่ซ้ำ");
        yield return Drop(Find(ObjectiveManager.KrapowLeafIngredientId).First());
        Check(Stir(), "คนให้เข้ากันหลังใส่กะเพรา");
        Check(objective.IsStepComplete(ObjectiveManager.CookSeasonStepId), "ขั้นปรุงรสและกะเพราครบ");
        Check(MistakesSince(logStart).Count(e => !e.value.Contains(CookingOrder.Duplicate)) == 0,
            "ทำถูกลำดับ ไม่มีข้อผิดพลาดลำดับใน Log");
        Check(ActionLog.Entries.Skip(logStart).Count(e => e.action == ActionLog.Actions.FoodStirred) == 2,
            "บันทึกการคน 2 ครั้งใน Log");

        // ---------- เสิร์ฟ (ทำถูก) ----------
        Check(objective.CurrentStepId == ObjectiveManager.ServePlateStepId, "ปรุงครบแล้วไปขั้นตักใส่จาน");
        stove.SetLevel(0);
        Check(!spatula.ProcessScoop(panCenter, 0.5f, 0.6f) && !spatula.Loaded, "ขยับตะหลิวเร็ว (คน) ไม่นับเป็นการตัก");
        Check(!spatula.ProcessScoop(panCenter, 0f, 0.3f) && spatula.ProcessScoop(panCenter, 0f, 0.3f) && spatula.Loaded,
            "ปิดเตาแล้วจุ่มตะหลิวนิ่ง 0.5 วินาที ตักได้");
        Check(spatula.ProcessScoop(plate.transform.position + Vector3.up * 0.03f, 0f, 0f) && plate.Filled && !spatula.Loaded,
            "แตะจาน อาหารลงจาน");
        Check(pan.Plated && pan.Contents.All(i => i.CurrentState == IngredientState.Served), "กระทะว่าง วัตถุดิบเป็นสถานะเสิร์ฟ");
        Check(LastAction(ActionLog.Actions.DishPlated, logStart) is { isCorrect: true }, "บันทึก DishPlated ถูกต้อง (ปิดเตา ไก่สุก ครบ)");
        Check(objective.CurrentStepId == ObjectiveManager.ServeNpcStepId, "ตักใส่จานแล้วไปขั้นเสิร์ฟป้าแสง");
        Check(serve.TryServe(plate) && objective.MissionCompleted && plate.Served, "วางจานที่จุดเสิร์ฟ จบภารกิจ");
        Check(LastAction(ActionLog.Actions.DishServed, logStart) is { isCorrect: true } &&
              LastAction(ActionLog.Actions.MissionCompleted, logStart) != null, "บันทึก DishServed และ MissionCompleted");
        Check(!serve.TryServe(plate), "เสิร์ฟซ้ำไม่ได้");

        SeasoningBottle oil = bottles[ObjectiveManager.CookingOilIngredientId];
        Place(oil, oilStart);
        Check(!oil.TryPour(true), "ขวดไม่อยู่เหนือกระทะ เทไม่ได้");

        // ---------- รอบที่ 2: ทำผิดลำดับ ----------
        yield return PrepareToCook();
        logStart = ActionLog.Entries.Count;
        Stir();
        Check(pan.StirCount == 0 &&
              !ActionLog.Entries.Skip(logStart).Any(e => e.action == ActionLog.Actions.FoodStirred),
            "กระทะว่าง คนแล้วไม่นับ");

        chicken = Find(ObjectiveManager.ChickenIngredientId).First();
        yield return Drop(chicken);
        Check(LastAdded(ObjectiveManager.ChickenIngredientId, logStart) is { isCorrect: false } c1 &&
              c1.value.Contains(CookingOrder.ChickenBeforeOil), "ใส่ไก่ก่อนน้ำมัน บันทึกข้อผิดพลาด");
        yield return Drop(Find(ObjectiveManager.KrapowLeafIngredientId).First());
        Check(LastAdded(ObjectiveManager.KrapowLeafIngredientId, logStart) is { isCorrect: false } k1 &&
              k1.value.Contains(CookingOrder.KrapowBeforeChickenCooked), "ใส่กะเพราก่อนไก่สุก บันทึกข้อผิดพลาด");
        Check(objective.GetCollectedCount(ObjectiveManager.CookSeasonStepId, ObjectiveManager.KrapowLeafIngredientId) == 1 &&
              objective.CurrentStepId == ObjectiveManager.CookAromaticsStepId,
            "ใส่ผิดลำดับยังนับให้ขั้นของมัน แต่ขั้นปัจจุบันไม่ข้าม");

        SeasoningBottle fish = bottles[ObjectiveManager.FishSauceIngredientId];
        Place(fish, panCenter + Vector3.up * 0.25f);
        Check(!fish.UpdateTilt(60f, true, 1f), "เอียงน้อย (60°) ยังไม่เท");
        Check(!fish.UpdateTilt(80f, true, 0.1f), "เอียง 80° ยังไม่ถึงเวลา ยังไม่เท");
        Check(fish.UpdateTilt(80f, true, 0.2f), "เอียง 80° ค้างครบเวลา เทได้");
        Check(!fish.UpdateTilt(80f, true, 1f), "เอียงค้างต่อไม่เทซ้ำ");
        Check(LastAdded(ObjectiveManager.FishSauceIngredientId, logStart) is { isCorrect: false } f1 &&
              f1.value.Contains(CookingOrder.SauceBeforeChickenCooked), "ปรุงรสก่อนไก่สุก บันทึกข้อผิดพลาด");

        // ผสมไม่ครบแล้วเทก่อนไก่สุก: นับเฉพาะที่อยู่ในถ้วย และบันทึกว่าปรุงรสก่อนไก่สุก
        Check(PourIntoBowl(ObjectiveManager.SugarIngredientId) && bowl.Count == 1, "เทน้ำตาลลงถ้วย 1/3");
        Check(bowl.PourIntoPan() && bowl.Count == 0, "เทถ้วยลงกระทะ");
        Check(LastAdded(ObjectiveManager.SugarIngredientId, logStart) is { isCorrect: false } s1 &&
              s1.value.Contains(CookingOrder.SauceBeforeChickenCooked) &&
              objective.GetCollectedCount(ObjectiveManager.CookSeasonStepId, ObjectiveManager.OysterSauceIngredientId) == 0,
            "เทถ้วยก่อนไก่สุก บันทึกผิดลำดับ และไม่นับซอสที่ไม่ได้ใส่");
        Check(PourIntoBowl(ObjectiveManager.OysterSauceIngredientId) && bowl.Count == 1, "ใส่ซอสหอยนางรมลงถ้วยค้างไว้ก่อน Reset");

        // ปากขวดเลยขอบกระทะเล็กน้อย (10 ซม.) ยังเทลงกระทะได้
        SeasoningBottle oyster = bottles[ObjectiveManager.OysterSauceIngredientId];
        Place(oyster, new Vector3(panZone.max.x + 0.08f, panZone.max.y + 0.05f, panCenter.z));
        Check(pan.IsAbovePan(oyster.SpoutPosition), "ปากขวดเลยขอบกระทะ 8 ซม. ยังนับว่าเหนือกระทะ");

        // ---------- เสิร์ฟจานที่ทำไม่ครบ ----------
        stove.SetLevel(2);
        spatula.ProcessScoop(panCenter, 0f, 0.3f);
        Check(!spatula.ProcessScoop(panCenter, 0f, 0.6f) && !spatula.Loaded, "ไฟยังเปิดและปรุงไม่ครบ ตักไม่ได้");
        stove.SetLevel(0);
        spatula.ProcessScoop(panCenter, 0f, 0.3f);
        spatula.ProcessScoop(panCenter, 0f, 0.3f);
        Check(spatula.Loaded && spatula.ProcessScoop(plate.transform.position + Vector3.up * 0.03f, 0f, 0f),
            "ปิดเตาแล้วตักจานที่ไม่ครบได้");
        Check(LastAction(ActionLog.Actions.DishPlated, logStart) is { isCorrect: false } p2 &&
              p2.value.Contains("missing=") && p2.value.Contains(ObjectiveManager.CookingOilIngredientId),
            "จานไม่ครบ บันทึกสิ่งที่ขาด");
        Check(serve.TryServe(plate) && objective.MissionCompleted, "เสิร์ฟจานที่ไม่ครบได้ (จบรอบ)");
        Check(LastAction(ActionLog.Actions.DishServed, logStart) is { isCorrect: false }, "บันทึก DishServed ว่าไม่ถูกต้อง");

        // ---------- Reset ----------
        objective.ResetObjectives();
        yield return new WaitForEndOfFrame();
        yield return Tick();
        Check(pan.Order.Added.Count == 0 && pan.StirCount == 0 && pan.Contents.Count == 0, "Reset ล้างของในกระทะ");
        Check(objective.CurrentStepId == ObjectiveManager.AcceptMissionStepId &&
              objective.GetCollectedCount(ObjectiveManager.CookAromaticsStepId, ObjectiveManager.CookingOilIngredientId) == 0,
            "Reset คืนขั้นภารกิจและจำนวน");
        Check(Vector3.Distance(oil.transform.position, oilStart) < 0.02f, "Reset คืนขวดกลับที่เดิม");
        Check(bowl.Count == 0 && Vector3.Distance(bowl.transform.position, bowlStart) < 0.02f, "Reset ถ้วยว่างและกลับที่เดิม");
        float plateMoved = Vector3.Distance(plate.transform.position, plateStart);
        Check(!plate.Filled && !plate.Served, $"Reset จานว่าง (Filled={plate.Filled}, Served={plate.Served})");
        Check(!serve.HasServed && !objective.MissionCompleted,
            $"Reset เสิร์ฟได้อีกครั้ง (HasServed={serve.HasServed}, MissionCompleted={objective.MissionCompleted})");
        Check(plateMoved < 0.02f, $"Reset จานกลับที่เดิม (ห่าง {plateMoved * 100f:0.0} ซม.)");
        Check(stove.Level == 0, "Reset ปิดเตา");
    }

    // เก็บ → ล้าง → หั่น ผ่าน ObjectiveManager โดยตรง (ส่วนนี้ทดสอบแล้วในชุดเก็บวัตถุดิบ)
    private IEnumerator PrepareToCook()
    {
        objective.ResetObjectives();
        yield return new WaitForEndOfFrame();
        yield return Tick();
        dropIndex = 0;
        ingredients = FindObjectsByType<IngredientIdentity>(FindObjectsInactive.Exclude);
        FindAnyObjectByType<MissionGiver>().GiveMission();

        foreach (string id in new[] { ObjectiveManager.KrapowLeafIngredientId, ObjectiveManager.ChiliIngredientId,
                     ObjectiveManager.ChickenIngredientId, ObjectiveManager.GarlicIngredientId })
            foreach (IngredientIdentity item in Find(id)) objective.TryCollectIngredient(item);
        foreach (string id in new[] { ObjectiveManager.KrapowLeafIngredientId, ObjectiveManager.ChiliIngredientId })
            foreach (IngredientIdentity item in Find(id)) objective.TryAdvanceIngredient(item, IngredientState.Washed);
        foreach (string id in new[] { ObjectiveManager.ChickenIngredientId, ObjectiveManager.ChiliIngredientId,
                     ObjectiveManager.GarlicIngredientId })
            foreach (IngredientIdentity item in Find(id)) objective.TryAdvanceIngredient(item, IngredientState.Cut);
    }

    private IEnumerable<IngredientIdentity> Find(string id)
    {
        int count = id is ObjectiveManager.ChiliIngredientId or ObjectiveManager.GarlicIngredientId ? 3 : 1;
        return ingredients.Where(i => i.IngredientId == id).Take(count).ToArray();
    }

    // วางวัตถุดิบลงในกระทะ (แยกตำแหน่งเล็กน้อย) แล้วตรึงไว้ไม่ให้กระเด็น
    private IEnumerator Drop(IngredientIdentity item)
    {
        int n = dropIndex++;
        Vector3 offset = new Vector3((n % 3 - 1) * 0.06f, 0.02f, (n / 3 % 3 - 1) * 0.06f);
        Vector3 target = panCenter + offset;
        Collider[] parts = item.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
        Bounds bounds = parts[0].bounds;
        foreach (Collider part in parts) bounds.Encapsulate(part.bounds);
        item.transform.position += target - bounds.center;
        var body = item.GetComponent<Rigidbody>();
        if (body != null)
        {
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
            body.isKinematic = true;
            body.position = item.transform.position;
        }
        Physics.SyncTransforms();
        yield return Tick();
    }

    private void Place(SeasoningBottle bottle, Vector3 position)
    {
        var body = bottle.GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true;
        bottle.transform.SetPositionAndRotation(position, Quaternion.identity);
        if (body != null) { body.position = position; body.rotation = Quaternion.identity; }
    }

    private bool PourIntoBowl(string id)
    {
        SeasoningBottle bottle = bottles[id];
        Place(bottle, bowl.transform.position + Vector3.up * 0.2f);
        return bottle.TryPour(true);
    }

    private void PlaceBowl(Vector3 position)
    {
        var body = bowl.GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true;
        bowl.transform.SetPositionAndRotation(position, Quaternion.identity);
        if (body != null) { body.position = position; body.rotation = Quaternion.identity; }
    }

    private bool Pour(string id)
    {
        SeasoningBottle bottle = bottles[id];
        Place(bottle, panCenter + Vector3.up * 0.25f);
        return bottle.TryPour(true);
    }

    // ขยับปลายตะหลิวไป-กลับในกระทะจนครบระยะ 1 ครั้ง
    private bool Stir()
    {
        Vector3 a = panCenter + Vector3.left * 0.06f;
        Vector3 b = panCenter + Vector3.right * 0.06f;
        bool counted = false;
        int moves = Mathf.CeilToInt(spatula.StirDistance / 0.12f) + 1;
        for (int i = 0; i < moves && !counted; i++)
            counted = spatula.ProcessTipMovement(i % 2 == 0 ? a : b, i % 2 == 0 ? b : a);
        spatula.ResetStroke();
        return counted;
    }

    // เร่งเวลาให้ไก่สุกเร็วขึ้น แล้วปิดไฟทันทีกันไหม้
    private IEnumerator CookUntil(IngredientIdentity item, IngredientState state)
    {
        stove.SetLevel(3);
        Time.timeScale = 4f;
        float limit = Time.realtimeSinceStartup + 15f;
        while (item.CurrentState != state && item.CurrentState != IngredientState.Burned &&
               Time.realtimeSinceStartup < limit)
            yield return null;
        stove.SetLevel(0);
        Time.timeScale = 1f;
    }

    private List<ActionLogEntry> MistakesSince(int start)
    {
        return ActionLog.Entries.Skip(start)
            .Where(e => e.action == ActionLog.Actions.IngredientAddedToPan && !e.isCorrect)
            .ToList();
    }

    private static ActionLogEntry LastAction(string action, int start)
    {
        return ActionLog.Entries.Skip(start).LastOrDefault(e => e.action == action);
    }

    private static ActionLogEntry LastAdded(string id, int start)
    {
        return ActionLog.Entries.Skip(start)
            .LastOrDefault(e => e.action == ActionLog.Actions.IngredientAddedToPan && e.ingredientId == id);
    }
}
#endif
