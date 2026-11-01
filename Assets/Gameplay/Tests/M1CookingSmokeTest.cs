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
        bottles = FindObjectsByType<SeasoningBottle>(FindObjectsInactive.Exclude)
            .ToDictionary(b => b.IngredientId);

        Check(objective != null && pan != null && stove != null, "มี ObjectiveManager กระทะ และเตา");
        Check(bottles.Count == 4 && spatula != null, "มีขวดเครื่องปรุง 4 ขวดและตะหลิว (รัน Setup Pan Cooking แล้ว)");
        if (objective == null || pan == null || stove == null || bottles.Count < 4 || spatula == null) yield break;
        Check(objective.FindStep(ObjectiveManager.CookAromaticsStepId) != null &&
              objective.FindStep(ObjectiveManager.CookSeasonStepId) != null, "ตารางมีขั้นปรุง 3 ขั้น");

        panCenter = pan.GetComponent<Collider>().bounds.center;
        Vector3 oilStart = bottles[ObjectiveManager.CookingOilIngredientId].transform.position;

        // ---------- รอบที่ 1: ทำถูกลำดับ ----------
        yield return PrepareToCook();
        Check(objective.CurrentStepId == ObjectiveManager.CookAromaticsStepId, "เตรียมครบแล้วถึงขั้นผัดเครื่องหอม");
        int logStart = ActionLog.Entries.Count;
        stove.SetLevel(2);

        Check(Pour(ObjectiveManager.CookingOilIngredientId), "เทน้ำมันเหนือกระทะได้");
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

        Check(Pour(ObjectiveManager.OysterSauceIngredientId) &&
              Pour(ObjectiveManager.FishSauceIngredientId) &&
              Pour(ObjectiveManager.SugarIngredientId), "เทซอสหอยนางรม น้ำปลา น้ำตาล");
        yield return Drop(Find(ObjectiveManager.KrapowLeafIngredientId).First());
        Check(Stir(), "คนให้เข้ากันหลังใส่กะเพรา");
        Check(objective.IsStepComplete(ObjectiveManager.CookSeasonStepId), "ขั้นปรุงรสและกะเพราครบ");
        Check(MistakesSince(logStart).Count == 0, "ทำถูกลำดับ ไม่มีข้อผิดพลาดลำดับใน Log");
        Check(ActionLog.Entries.Skip(logStart).Count(e => e.action == ActionLog.Actions.FoodStirred) == 2,
            "บันทึกการคน 2 ครั้งใน Log");

        Check(!Pour(ObjectiveManager.CookingOilIngredientId), "เทน้ำมันซ้ำไม่นับ");
        Check(MistakesSince(logStart).Any(e => e.value.Contains(CookingOrder.Duplicate)), "เทซ้ำบันทึกว่าใส่ซ้ำ");
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
        Check(!fish.UpdateTilt(120f, true, 0.2f), "เอียงขวดยังไม่ถึงเวลา ยังไม่เท");
        Check(fish.UpdateTilt(120f, true, 0.3f), "เอียงขวดค้างครบเวลา เทได้");
        Check(!fish.UpdateTilt(120f, true, 1f), "เอียงค้างต่อไม่เทซ้ำ");
        Check(LastAdded(ObjectiveManager.FishSauceIngredientId, logStart) is { isCorrect: false } f1 &&
              f1.value.Contains(CookingOrder.SauceBeforeChickenCooked), "ปรุงรสก่อนไก่สุก บันทึกข้อผิดพลาด");

        // ---------- Reset ----------
        objective.ResetObjectives();
        yield return new WaitForEndOfFrame();
        yield return Tick();
        Check(pan.Order.Added.Count == 0 && pan.StirCount == 0 && pan.Contents.Count == 0, "Reset ล้างของในกระทะ");
        Check(objective.CurrentStepId == ObjectiveManager.AcceptMissionStepId &&
              objective.GetCollectedCount(ObjectiveManager.CookAromaticsStepId, ObjectiveManager.CookingOilIngredientId) == 0,
            "Reset คืนขั้นภารกิจและจำนวน");
        Check(Vector3.Distance(oil.transform.position, oilStart) < 0.02f, "Reset คืนขวดกลับชั้น");
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

    private static ActionLogEntry LastAdded(string id, int start)
    {
        return ActionLog.Entries.Skip(start)
            .LastOrDefault(e => e.action == ActionLog.Actions.IngredientAddedToPan && e.ingredientId == id);
    }
}
#endif
