using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class IngredientCollector : MonoBehaviour
{
    [Header("Validation Rule")]
    [SerializeField] private string expectedIngredientName = "Krapow";
    [SerializeField] private bool acceptOnlyOnce = true;

    [Header("Result Events")]
    [SerializeField] private UnityEvent onCorrectIngredient;
    [SerializeField] private UnityEvent onWrongIngredient;

    public bool HasAcceptedIngredient { get; private set; }
    public string LastDetectedIngredient { get; private set; } = string.Empty;

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (acceptOnlyOnce && HasAcceptedIngredient)
            return;

        Rigidbody ingredientBody = other.attachedRigidbody;
        if (ingredientBody == null)
            return;

        GameObject ingredient = ingredientBody.gameObject;
        LastDetectedIngredient = NormalizeName(ingredient.name);

        bool isCorrect = string.Equals(
            LastDetectedIngredient,
            expectedIngredientName.Trim(),
            StringComparison.OrdinalIgnoreCase);

        if (isCorrect)
        {
            HasAcceptedIngredient = true;
            Debug.Log($"[IngredientCollector] Correct ingredient: {LastDetectedIngredient}", this);
            onCorrectIngredient?.Invoke();
            return;
        }

        Debug.LogWarning(
            $"[IngredientCollector] Wrong ingredient: {LastDetectedIngredient}. Expected: {expectedIngredientName}",
            this);
        onWrongIngredient?.Invoke();
    }

    private static string NormalizeName(string objectName)
    {
        return objectName.Replace("(Clone)", string.Empty).Trim();
    }

    private void EnsureTriggerCollider()
    {
        if (TryGetComponent(out Collider zoneCollider))
            zoneCollider.isTrigger = true;
    }
}
