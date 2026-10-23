using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
public class IngredientCollector : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private CollectionFeedbackUI feedbackUI;

    private readonly Dictionary<Collider, IngredientIdentity> collidersInside = new();
    private readonly HashSet<IngredientIdentity> attempted = new();

    // วัตถุดิบที่วางถูกและหยุดนิ่งอยู่ในจุดนี้แล้ว การตั้ง isKinematic ตอนหยุดของ
    // ทำให้ Physics ส่ง Exit/Enter ซ้ำในเฟรมเดียวกัน จึงต้องไม่ตรวจซ้ำ
    // จนกว่าผู้เล่นจะหยิบชิ้นนั้นขึ้นมาใหม่
    private readonly HashSet<IngredientIdentity> settled = new();

    public void Configure(
        ObjectiveManager objective,
        CollectionFeedbackUI feedback)
    {
        objectiveManager = objective;
        feedbackUI = feedback;
    }

    private void Reset()
    {
        Collider zoneCollider = GetComponent<Collider>();
        zoneCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        var ingredient = FindIngredient(other);
        if (ingredient != null) collidersInside[other] = ingredient;
    }

    private void OnTriggerStay(Collider other)
    {
        var ingredient = FindIngredient(other);
        if (ingredient == null || ingredient.IsResetting) return;
        collidersInside[other] = ingredient;
        var grab = ingredient.GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected)
        {
            attempted.Remove(ingredient);
            settled.Remove(ingredient);
            return;
        }

        if (settled.Contains(ingredient)) return;
        // Evaluate on a physics overlap after release, not in selectExited:
        // release outside the zone must never be counted from a stale overlap.
        if (!attempted.Add(ingredient)) return;

        if (objectiveManager == null)
        {
            Debug.LogError(
                "[จุดรับวัตถุดิบ] ยังไม่ได้เชื่อมระบบจัดการเป้าหมาย",
                this);
            feedbackUI?.ShowSystemError();
            return;
        }

        IngredientCollectionResult result =
            objectiveManager.TryCollectIngredient(ingredient);

        feedbackUI?.ShowResult(result, ingredient);

        if (result == IngredientCollectionResult.Correct)
        {
            settled.Add(ingredient);
            StopIngredientAtCollectionZone(ingredient);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!collidersInside.TryGetValue(other, out var ingredient)) return;
        collidersInside.Remove(other);
        // A multi-collider item is still inside until its last collider leaves.
        if (!collidersInside.ContainsValue(ingredient)) attempted.Remove(ingredient);
    }

    private IngredientIdentity FindIngredient(Collider other)
    {
        IngredientIdentity ingredient =
            other.GetComponentInParent<IngredientIdentity>();

        if (ingredient != null)
        {
            return ingredient;
        }

        if (other.attachedRigidbody == null)
        {
            return null;
        }

        return other.attachedRigidbody.GetComponent<IngredientIdentity>();
    }

    private void StopIngredientAtCollectionZone(
        IngredientIdentity ingredient)
    {
        Rigidbody ingredientBody = ingredient.GetComponent<Rigidbody>();

        if (ingredientBody == null)
        {
            return;
        }

        if (!ingredientBody.isKinematic)
        {
            ingredientBody.linearVelocity = Vector3.zero;
            ingredientBody.angularVelocity = Vector3.zero;
        }

        ingredientBody.isKinematic = true;
    }

    public void ResetCollector()
    {
        collidersInside.Clear();
        attempted.Clear();
        settled.Clear();
    }

    private void OnDisable() => ResetCollector();
}
