using System;
using UnityEngine;

public class IngredientIdentity : MonoBehaviour
{
    public const string KrapowLeafId = "krapow_leaf";

    [Header("Ingredient Information")]
    [SerializeField] private string ingredientId = KrapowLeafId;
    [SerializeField] private string legacyPrefabName = "Krapow";
    [SerializeField] private string displayNameThai = "ใบกะเพรา";

    [Header("Runtime State")]
    [SerializeField] private string instanceId;
    [SerializeField] private IngredientState currentState =
        IngredientState.Raw;

    public string IngredientId => ingredientId;
    public string InstanceId
    {
        get
        {
            CreateInstanceIdIfNeeded();
            return instanceId;
        }
    }
    public IngredientState CurrentState => currentState;

    private void Awake()
    {
        CreateInstanceIdIfNeeded();
        ApplyLegacyCompatibility();
    }

    private void CreateInstanceIdIfNeeded()
    {
        if (!string.IsNullOrEmpty(instanceId))
        {
            return;
        }

        instanceId = Guid.NewGuid().ToString("N");
    }

    private void ApplyLegacyCompatibility()
    {
        if (string.Equals(
                ingredientId,
                legacyPrefabName,
                StringComparison.OrdinalIgnoreCase))
        {
            ingredientId = KrapowLeafId;
        }
    }

    public bool MatchesIngredientId(string requestedId)
    {
        if (string.IsNullOrWhiteSpace(requestedId))
        {
            return false;
        }

        if (string.Equals(
                requestedId,
                ingredientId,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return requestedId.StartsWith(
            legacyPrefabName,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool TryCollect()
    {
        if (currentState != IngredientState.Raw)
        {
            Debug.LogWarning(
                $"[วัตถุดิบ] {displayNameThai} ชิ้นนี้ถูกเก็บแล้ว " +
                "จึงไม่นับซ้ำ",
                this);

            return false;
        }

        return TrySetState(IngredientState.Collected);
    }

    public bool TrySetState(IngredientState newState)
    {
        if (currentState == newState)
        {
            Debug.LogWarning(
                $"[วัตถุดิบ] {displayNameThai} อยู่ในสถานะ " +
                $"{GetStateNameThai(currentState)} แล้ว",
                this);

            return false;
        }

        currentState = newState;

        Debug.Log(
            $"[วัตถุดิบ] {displayNameThai} ({ingredientId}): " +
            $"{GetStateNameThai(currentState)}",
            this);

        return true;
    }

    public void ResetIngredient()
    {
        currentState = IngredientState.Raw;

        Debug.Log(
            $"[วัตถุดิบ] {displayNameThai}: ยังไม่เก็บ",
            this);
    }

    private string GetStateNameThai(IngredientState state)
    {
        return state switch
        {
            IngredientState.Raw => "ยังไม่เก็บ",
            IngredientState.Collected => "เก็บแล้ว",
            IngredientState.Washed => "ล้างแล้ว",
            IngredientState.Cut => "หั่นแล้ว",
            IngredientState.AddedToPan => "ใส่กระทะแล้ว",
            IngredientState.Cooking => "กำลังปรุง",
            IngredientState.Cooked => "ปรุงสุกแล้ว",
            IngredientState.Burned => "ไหม้",
            IngredientState.Served => "เสิร์ฟแล้ว",
            _ => "ไม่ทราบสถานะ"
        };
    }

    [ContextMenu("Test/Collect Ingredient")]
    private void TestCollectIngredient()
    {
        TryCollect();
    }

    [ContextMenu("Test/Reset Ingredient")]
    private void TestResetIngredient()
    {
        ResetIngredient();
    }
}
