using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public enum StationAction
{
    Wash,
    Cut
}

// จุดทำงานแบบต้นแบบ ใช้ได้ทั้งอ่างล้างและเขียง ต่างกันที่การตั้งค่า
// - ล้าง: ถือหรือวางผักไว้ใต้ก๊อกครบ washSeconds วินาที → Washed
// - หั่น: วางวัตถุดิบบนเขียง แล้วใช้มีด (KitchenKnife) สับลงในโซน chopsToCut ครั้ง → Cut
// ผลถูกหรือผิดตัดสินโดย ObjectiveManager ตามตาราง Mission Steps
[RequireComponent(typeof(Collider))]
public class StationZone : MonoBehaviour, IMissionResettable
{
    [Header("Required References")]
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private CollectionFeedbackUI feedbackUI;

    [Header("Station")]
    [SerializeField] private StationAction action = StationAction.Wash;
    [Tooltip("วัตถุที่แสดงเฉพาะตอนกำลังทำงาน เช่น สายน้ำจากก๊อก")]
    [SerializeField] private GameObject workingVisual;

    [Header("Wash")]
    [SerializeField] private float washSeconds = 2f;

    [Header("Cut")]
    [SerializeField] private int chopsToCut = 3;
    [SerializeField] private float minSecondsBetweenChops = 0.2f;

    private readonly Dictionary<Collider, IngredientIdentity> ingredientColliders = new();
    private readonly Dictionary<IngredientIdentity, float> washProgress = new();
    private readonly Dictionary<IngredientIdentity, int> chopCounts = new();
    private readonly HashSet<IngredientIdentity> handled = new();
    private readonly HashSet<Collider> knifeColliders = new();
    private float lastChopTime = -10f;

    private string Verb => action == StationAction.Wash ? "ล้าง" : "หั่น";

    private IngredientState TargetState =>
        action == StationAction.Wash ? IngredientState.Washed : IngredientState.Cut;

    public void Configure(
        ObjectiveManager objective,
        CollectionFeedbackUI feedback,
        StationAction stationAction,
        GameObject visual)
    {
        objectiveManager = objective;
        feedbackUI = feedback;
        action = stationAction;
        workingVisual = visual;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        SetWorking(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        KitchenKnife knife = other.GetComponentInParent<KitchenKnife>();

        if (knife != null)
        {
            bool firstContact = knifeColliders.Count == 0;
            knifeColliders.Add(other);

            if (firstContact && action == StationAction.Cut)
            {
                Chop(knife);
            }

            return;
        }

        IngredientIdentity ingredient = FindIngredient(other);

        if (ingredient != null && !ingredient.IsResetting)
        {
            ingredientColliders[other] = ingredient;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (knifeColliders.Remove(other))
        {
            return;
        }

        if (!ingredientColliders.TryGetValue(other, out IngredientIdentity ingredient))
        {
            return;
        }

        ingredientColliders.Remove(other);

        // วัตถุดิบหลายชิ้นส่วน (เช่น ต้นกะเพรา) ยังอยู่ในโซนจนกว่าชิ้นสุดท้ายจะออก
        if (!ingredientColliders.ContainsValue(ingredient))
        {
            washProgress.Remove(ingredient);
            chopCounts.Remove(ingredient);
            handled.Remove(ingredient);
        }
    }

    private void Update()
    {
        if (action != StationAction.Wash)
        {
            return;
        }

        IngredientIdentity[] inside = ingredientColliders.Values
            .Where(i => i != null)
            .Distinct()
            .ToArray();

        SetWorking(inside.Length > 0);

        foreach (IngredientIdentity ingredient in inside)
        {
            if (handled.Contains(ingredient))
            {
                continue;
            }

            washProgress.TryGetValue(ingredient, out float seconds);
            seconds += Time.deltaTime;
            washProgress[ingredient] = seconds;

            if (seconds >= washSeconds)
            {
                handled.Add(ingredient);
                Apply(ingredient);
            }
        }
    }

    private void Chop(KitchenKnife knife)
    {
        if (!knife.IsHeld || Time.time - lastChopTime < minSecondsBetweenChops)
        {
            return;
        }

        lastChopTime = Time.time;

        // หั่นชิ้นที่วางอยู่บนเขียง (ไม่ได้ถืออยู่) ที่ใกล้ใบมีดที่สุด
        IngredientIdentity target = ingredientColliders.Values
            .Where(i => i != null && !handled.Contains(i) && !IsHeld(i))
            .Distinct()
            .OrderBy(i => Vector3.Distance(i.transform.position, knife.transform.position))
            .FirstOrDefault();

        if (target == null)
        {
            Show("วางวัตถุดิบบนเขียงก่อน แล้วค่อยหั่น", false);
            return;
        }

        chopCounts.TryGetValue(target, out int chops);
        chops++;
        chopCounts[target] = chops;
        StartCoroutine(FlashWorking());

        if (chops < chopsToCut)
        {
            Show($"หั่น{target.DisplayNameThai} {chops}/{chopsToCut}", true);
            return;
        }

        chopCounts.Remove(target);
        handled.Add(target);
        Apply(target);
    }

    private void Apply(IngredientIdentity ingredient)
    {
        if (objectiveManager == null)
        {
            Debug.LogError("[จุดทำงาน] ยังไม่ได้เชื่อมระบบจัดการเป้าหมาย", this);
            return;
        }

        IngredientCollectionResult result =
            objectiveManager.TryAdvanceIngredient(ingredient, TargetState);
        string name = ingredient.DisplayNameThai;

        switch (result)
        {
            case IngredientCollectionResult.Correct:
                Show($"{Verb}{name}เรียบร้อย", true);
                break;
            case IngredientCollectionResult.Duplicate:
                Show($"{name}{Verb}แล้ว", false);
                break;
            case IngredientCollectionResult.WrongIngredient:
                Show($"{name}ไม่ต้อง{Verb}ตอนนี้", false);
                break;
            case IngredientCollectionResult.WrongObjective:
                string step = objectiveManager.CurrentStep?.titleThai ?? "ขั้นก่อนหน้า";
                Show($"ยังไม่ถึงขั้น{Verb} ตอนนี้: {step}", false);
                break;
            default:
                Show("ไม่พบข้อมูลของวัตถุดิบ", false);
                break;
        }
    }

    private System.Collections.IEnumerator FlashWorking()
    {
        SetWorking(true);
        yield return new WaitForSeconds(0.15f);
        SetWorking(false);
    }

    private void SetWorking(bool working)
    {
        if (workingVisual != null && workingVisual.activeSelf != working)
        {
            workingVisual.SetActive(working);
        }
    }

    private void Show(string message, bool success)
    {
        Debug.Log($"[จุดทำงาน:{Verb}] {message}", this);
        feedbackUI?.ShowStationMessage(message, success);
    }

    private static bool IsHeld(IngredientIdentity ingredient)
    {
        var grab = ingredient.GetComponent<XRGrabInteractable>();
        return grab != null && grab.isSelected;
    }

    private static IngredientIdentity FindIngredient(Collider other)
    {
        IngredientIdentity ingredient = other.GetComponentInParent<IngredientIdentity>();

        if (ingredient != null || other.attachedRigidbody == null)
        {
            return ingredient;
        }

        return other.attachedRigidbody.GetComponent<IngredientIdentity>();
    }

    public void ResetForMission()
    {
        ingredientColliders.Clear();
        washProgress.Clear();
        chopCounts.Clear();
        handled.Clear();
        knifeColliders.Clear();
        SetWorking(false);
    }
}
