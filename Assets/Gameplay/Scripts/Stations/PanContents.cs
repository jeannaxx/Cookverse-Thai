using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// พื้นที่ในกระทะบนเตา
// - ปล่อยวัตถุดิบที่เตรียมแล้ว (ล้าง/หั่น) ลงในกระทะตอนถึงขั้นปรุง → AddedToPan
// - ไก่ในกระทะสะสมความร้อนตามระดับไฟของ GasStove เฉพาะตอนเปิดไฟและไก่อยู่ในกระทะ
//   0 = ดิบ · >0 = กำลังสุก · ≥100 = สุก · ≥160 = ไหม้ (ค่าร่าง ปรับใน Inspector)
// ลำดับการใส่ของ เครื่องปรุง และการคน ทำในขั้นถัดไป
[RequireComponent(typeof(Collider))]
public class PanContents : MonoBehaviour, IMissionResettable
{
    [Header("Required References")]
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private GasStove stove;
    [SerializeField] private CollectionFeedbackUI feedbackUI;

    [Header("Doneness")]
    [SerializeField] private string trackedIngredientId = ObjectiveManager.ChickenIngredientId;
    [SerializeField] private float cookedAtHeat = 100f;
    [SerializeField] private float burnedAtHeat = 160f;

    private readonly Dictionary<Collider, IngredientIdentity> colliders = new();
    private readonly HashSet<IngredientIdentity> inPan = new();
    private readonly HashSet<IngredientIdentity> rejected = new();
    private readonly Dictionary<IngredientIdentity, float> heat = new();

    public IReadOnlyCollection<IngredientIdentity> Contents => inPan;

    public float GetHeat(IngredientIdentity ingredient)
    {
        return ingredient != null && heat.TryGetValue(ingredient, out float value) ? value : 0f;
    }

    public void Configure(ObjectiveManager objective, GasStove gasStove, CollectionFeedbackUI feedback)
    {
        objectiveManager = objective;
        stove = gasStove;
        feedbackUI = feedback;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        IngredientIdentity ingredient = FindIngredient(other);

        if (ingredient != null && !ingredient.IsResetting)
        {
            colliders[other] = ingredient;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!colliders.TryGetValue(other, out IngredientIdentity ingredient))
        {
            return;
        }

        colliders.Remove(other);

        if (!colliders.ContainsValue(ingredient))
        {
            rejected.Remove(ingredient);
        }
    }

    private void Update()
    {
        IngredientIdentity[] inside = colliders.Values
            .Where(i => i != null)
            .Distinct()
            .ToArray();

        foreach (IngredientIdentity ingredient in inside)
        {
            if (!inPan.Contains(ingredient) && !rejected.Contains(ingredient) && !IsHeld(ingredient))
            {
                TryAdd(ingredient);
            }
        }

        float rate = stove != null ? stove.HeatRate : 0f;

        if (rate <= 0f)
        {
            return;
        }

        // ร้อนเฉพาะของที่ยังอยู่ในกระทะ ยกออกแล้วหยุดสุก
        foreach (IngredientIdentity ingredient in inside)
        {
            if (!inPan.Contains(ingredient) || ingredient.IngredientId != trackedIngredientId)
            {
                continue;
            }

            heat.TryGetValue(ingredient, out float value);
            value += rate * Time.deltaTime;
            heat[ingredient] = value;
            UpdateDoneness(ingredient, value);
        }
    }

    private void TryAdd(IngredientIdentity ingredient)
    {
        string stepId = objectiveManager != null ? objectiveManager.CurrentStepId : string.Empty;
        IngredientState before = ingredient.CurrentState;

        string reason = "ระบบเป้าหมายยังไม่พร้อม";

        if (objectiveManager == null || !objectiveManager.CanAddToPan(ingredient, out reason))
        {
            rejected.Add(ingredient);
            ActionLog.Record(ActionLog.Actions.IngredientAddedToPan, stepId, ingredient,
                before, before, isCorrect: false, value: reason);
            Show($"{ingredient.DisplayNameThai}: {reason}", false);
            return;
        }

        ingredient.TrySetState(IngredientState.AddedToPan);
        inPan.Add(ingredient);
        heat[ingredient] = 0f;

        ActionLog.Record(ActionLog.Actions.IngredientAddedToPan, stepId, ingredient,
            before, IngredientState.AddedToPan, isCorrect: true,
            value: stove != null ? $"heat={stove.Level}" : null);
        Show($"ใส่{ingredient.DisplayNameThai}ลงกระทะ", true);
    }

    private void UpdateDoneness(IngredientIdentity ingredient, float value)
    {
        IngredientState current = ingredient.CurrentState;

        if (value >= burnedAtHeat)
        {
            if (current == IngredientState.Burned)
            {
                return;
            }

            ingredient.TrySetState(IngredientState.Burned);
            LogFoodState(ingredient, current, IngredientState.Burned, value, false);
            Show($"{ingredient.DisplayNameThai}ไหม้แล้ว เริ่มการปรุงใหม่อีกครั้ง", false);
            return;
        }

        if (value >= cookedAtHeat)
        {
            if (current == IngredientState.Cooked || current == IngredientState.Burned)
            {
                return;
            }

            // ให้ ObjectiveManager นับขั้น "ผัดไก่จนสุก" และบันทึก Log
            IngredientCollectionResult result =
                objectiveManager.TryAdvanceIngredient(ingredient, IngredientState.Cooked);

            if (result != IngredientCollectionResult.Correct)
            {
                ingredient.TrySetState(IngredientState.Cooked);
                LogFoodState(ingredient, current, IngredientState.Cooked, value, true);
            }

            Show($"{ingredient.DisplayNameThai}สุกแล้ว อย่าปล่อยไว้นานจนไหม้", true);
            return;
        }

        if (current == IngredientState.AddedToPan)
        {
            ingredient.TrySetState(IngredientState.Cooking);
            LogFoodState(ingredient, current, IngredientState.Cooking, value, true);
            Show($"กำลังผัด{ingredient.DisplayNameThai}...", true);
        }
    }

    private void LogFoodState(
        IngredientIdentity ingredient,
        IngredientState before,
        IngredientState after,
        float value,
        bool isCorrect)
    {
        ActionLog.Record(
            ActionLog.Actions.FoodStateChanged,
            objectiveManager != null ? objectiveManager.CurrentStepId : string.Empty,
            ingredient, before, after, isCorrect, $"heat={value:0}");
    }

    private void Show(string message, bool success)
    {
        Debug.Log($"[กระทะ] {message}", this);
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
        colliders.Clear();
        inPan.Clear();
        rejected.Clear();
        heat.Clear();
    }
}
