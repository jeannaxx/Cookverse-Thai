using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class IngredientCollector : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private CollectionFeedbackUI feedbackUI;

    private readonly HashSet<IngredientIdentity> ingredientsInside = new();

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
        IngredientIdentity ingredient = FindIngredient(other);

        if (ingredient == null)
        {
            return;
        }

        if (!ingredientsInside.Add(ingredient))
        {
            return;
        }

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
            StopIngredientAtCollectionZone(ingredient);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        IngredientIdentity ingredient = FindIngredient(other);

        if (ingredient != null)
        {
            ingredientsInside.Remove(ingredient);
        }
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

        ingredientBody.linearVelocity = Vector3.zero;
        ingredientBody.angularVelocity = Vector3.zero;
        ingredientBody.isKinematic = true;
    }

    public void ResetCollector()
    {
        ingredientsInside.Clear();
    }
}
