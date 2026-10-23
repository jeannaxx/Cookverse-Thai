#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Runs only when explicitly invoked by the M1 editor menu during Play Mode.
public sealed class M1CollectionSmokeTest : MonoBehaviour
{
    private readonly List<string> results = new();
    private ObjectiveManager objective;
    private IngredientIdentity[] ingredients;
    private Vector3[] starts;
    private GameObject testHand;
    private GameObject wrong;
    private bool passed = true;
    private void Check(bool condition, string label)
    {
        results.Add((condition ? "ผ่าน: " : "ไม่ผ่าน: ") + label);
        passed &= condition;
    }
    private IEnumerator Start()
    {
        var routine = Run();
        while (true)
        {
            bool next;
            try { next = routine.MoveNext(); }
            catch (Exception e) { Check(false, "ข้อผิดพลาด " + e); break; }
            if (!next) break;
            yield return routine.Current;
        }
        if (objective != null) objective.ResetObjectives();
        if (testHand != null) Destroy(testHand);
        if (wrong != null) Destroy(wrong);
        results.Add(passed ? "ผลรวม: ผ่าน" : "ผลรวม: ไม่ผ่าน");
        File.WriteAllLines("/private/tmp/m1-collection-test.txt", results);
        Debug.Log("[ทดสอบ M1]\n" + string.Join("\n", results));
        Destroy(gameObject);
    }
    private IEnumerator Tick()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(0.15f);
    }
    private void Move(IngredientIdentity item, Vector3 position)
    {
        var colliders = item.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
        var bounds = colliders[0].bounds;
        foreach (var collider in colliders) bounds.Encapsulate(collider.bounds);
        item.transform.position += position - bounds.center;
        var body = item.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = item.transform.position;
            if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
        }
        Physics.SyncTransforms();
    }
    private IEnumerator Run()
    {
        objective = FindFirstObjectByType<ObjectiveManager>();
        ingredients = FindObjectsByType<IngredientIdentity>(FindObjectsInactive.Exclude);
        starts = ingredients.Select(i => i.transform.position).ToArray();
        var chilies = ingredients.Where(i => i.IngredientId == "chili").ToArray();
        var leaf = ingredients.First(i => i.IngredientId == "krapow_leaf");
        var zone = FindFirstObjectByType<IngredientCollector>();
        // วัตถุดิบในสวนอาจตั้งให้อยู่นิ่ง (kinematic) ตั้งแต่ต้น Reset ต้องคืนค่าเดิม
        bool leafStartKinematic = leaf.GetComponent<Rigidbody>().isKinematic;
        var target = zone.GetComponent<Collider>().bounds.center;
        objective.ResetObjectives();
        yield return new WaitForEndOfFrame();
        Check(chilies.Length == 3 && objective.RequiredChiliCount == 3, "พริกในฉากและเป้าหมายครบ 3");
        Check(ingredients.Select(i => i.InstanceId).Distinct().Count() == ingredients.Length, "รหัสวัตถุดิบแต่ละชิ้นไม่ซ้ำ");
        FindFirstObjectByType<MissionGiver>().GiveMission();
        Check(objective.CurrentStepId == ObjectiveManager.CollectGardenStepId, "รับภารกิจผ่านป้าแสง");

        wrong = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wrong.name = "M1 wrong ingredient test";
        wrong.transform.position = target + Vector3.up * 3;
        wrong.transform.localScale = Vector3.one * 0.1f;
        wrong.AddComponent<Rigidbody>().useGravity = false;
        var invalid = wrong.AddComponent<IngredientIdentity>();
        var data = new UnityEditor.SerializedObject(invalid);
        data.FindProperty("ingredientId").stringValue = "wrong_ingredient";
        data.ApplyModifiedPropertiesWithoutUndo();
        Move(invalid, target);
        yield return Tick();
        Check(objective.CollectedKrapowCount == 0 && objective.CollectedChiliCount == 0 && invalid.CurrentState == IngredientState.Raw, "วางของผิดไม่เพิ่มจำนวน");
        Move(invalid, target + Vector3.up * 3);

        testHand = new GameObject("M1 test interactor");
        testHand.transform.position = target;
        testHand.AddComponent<SphereCollider>().isTrigger = true;
        var interactor = testHand.AddComponent<XRDirectInteractor>();
        var grab = leaf.GetComponent<XRGrabInteractable>();
        var manager = grab.interactionManager;
        yield return null;
        manager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)grab);
        Move(leaf, target);
        yield return Tick();
        Check(grab.isSelected && objective.CollectedKrapowCount == 0, "ถืออยู่ในจุดรับยังไม่นับ");
        manager.SelectExit((IXRSelectInteractor)interactor, (IXRSelectInteractable)grab);
        yield return new WaitForEndOfFrame();
        Move(leaf, target);
        yield return Tick();
        Check(objective.CollectedKrapowCount == 1, "ปล่อยกะเพราในจุดรับแล้วนับ");
        Move(leaf, target + Vector3.up * 3);
        yield return Tick();
        Move(leaf, target);
        yield return Tick();
        Check(objective.CollectedKrapowCount == 1, "วางชิ้นเดิมซ้ำไม่เพิ่มจำนวน");
        // Clear the test placement so rigidbodies do not overlap each other.
        Move(leaf, target + Vector3.up * 3 + Vector3.right * 2);
        yield return Tick();
        for (int n = 0; n < chilies.Length; n++)
        {
            Move(chilies[n], target);
            yield return Tick();
            Check(objective.CollectedChiliCount == n + 1, "นับพริกชิ้นที่ " + (n + 1));
            results.Add($"ข้อมูลตรวจสอบ: {chilies[n].name} จำนวน={objective.CollectedChiliCount} ตำแหน่ง={chilies[n].GetComponent<Collider>().bounds.center} จุดรับ={target} ขนาดจุดรับ={zone.GetComponent<Collider>().bounds.size} ถือ={chilies[n].GetComponent<XRGrabInteractable>().isSelected}");
            if (n < 2) Check(objective.CurrentStepId == ObjectiveManager.CollectGardenStepId, "ยังไม่ผ่านเมื่อพริกไม่ครบ");
            Move(chilies[n], target + Vector3.up * 3 + Vector3.right * (3 + n));
            yield return Tick();
        }
        Check(objective.CurrentStepId == ObjectiveManager.CollectKitchenStepId, "ผ่านเมื่อกะเพรา 1 พริก 3 ครบ");
        var ui = FindFirstObjectByType<MissionChecklistUI>();
        var text = ui.GetComponentInChildren<Text>().text;
        Check(text.Contains("3/3") && text.Contains("วัตถุดิบครบแล้ว"), "UI แสดงพริก 3/3 และวัตถุดิบครบ");

        manager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)grab);
        Check(grab.isSelected, "เตรียมทดสอบ Reset ขณะถือ");
        objective.ResetObjectives();
        yield return new WaitForEndOfFrame();
        yield return Tick();
        Check(!grab.isSelected, "Reset ปล่อยวัตถุดิบออกจากมือ");
        Check(objective.CollectedKrapowCount == 0 && objective.CollectedChiliCount == 0 &&
            objective.CurrentStepId == ObjectiveManager.AcceptMissionStepId, "Reset คืนจำนวนและเป้าหมาย");
        Check(ingredients.All(i => i.CurrentState == IngredientState.Raw), "Reset คืนสถานะยังไม่เก็บ");
        Check(ingredients.Select((i,n) => Vector3.Distance(i.transform.position, starts[n]) < 0.02f).All(b => b), "Reset คืนตำแหน่งทุกชิ้น");
        var leafBody = grab.GetComponent<Rigidbody>();
        Check(leafBody.isKinematic == leafStartKinematic && (leafBody.isKinematic || leafBody.linearVelocity.sqrMagnitude < 0.01f), "Reset คืนฟิสิกส์และไม่เหวี่ยงของ");
        FindFirstObjectByType<MissionGiver>().GiveMission();
        Move(leaf, target);
        yield return Tick();
        Check(objective.CollectedKrapowCount == 1, "เริ่มรอบใหม่แล้วนับชิ้นเดิมได้");
    }
}
#endif
