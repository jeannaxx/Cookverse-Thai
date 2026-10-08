using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum IngredientCollectionResult
{
    Correct,
    WrongIngredient,
    Duplicate,
    WrongObjective,
    InvalidIngredient
}

public class ObjectiveManager : MonoBehaviour
{
    public const string AcceptMissionStepId = "accept_mission";
    public const string CollectGardenStepId = "collect_garden";
    public const string KrapowLeafIngredientId = "krapow_leaf";
    public const string LegacyKrapowName = "Krapow";

    [Header("Current Objective")]
    [SerializeField] private string currentStepId = AcceptMissionStepId;
    [SerializeField] private bool missionAccepted;

    [Header("Garden Collection")]
    [SerializeField] private int requiredKrapowCount = 1;
    [SerializeField] private int collectedKrapowCount;

    [Header("Events")]
    [SerializeField] private UnityEvent onObjectiveChanged;

    private readonly HashSet<string> collectedInstanceIds = new();

    public string CurrentStepId => currentStepId;
    public bool MissionAccepted => missionAccepted;
    public int CollectedKrapowCount => collectedKrapowCount;
    public int RequiredKrapowCount => requiredKrapowCount;

    public bool TryAcceptMission()
    {
        if (missionAccepted || currentStepId != AcceptMissionStepId)
        {
            Debug.LogWarning(
                "[เป้าหมาย] รับภารกิจนี้ไปแล้ว จึงไม่สามารถรับซ้ำได้",
                this);
            return false;
        }

        missionAccepted = true;
        currentStepId = CollectGardenStepId;

        Debug.Log(
            "[เป้าหมาย] รับภารกิจสำเร็จ เป้าหมายปัจจุบัน: " +
            "ไปเก็บวัตถุดิบที่สวน",
            this);

        onObjectiveChanged?.Invoke();
        return true;
    }

    public IngredientCollectionResult TryCollectIngredient(
        IngredientIdentity ingredient)
    {
        if (ingredient == null)
        {
            Debug.LogError(
                "[เป้าหมาย] ไม่พบข้อมูลประจำตัวของวัตถุดิบ",
                this);
            return IngredientCollectionResult.InvalidIngredient;
        }

        return EvaluateCollection(
            ingredient.IngredientId,
            ingredient.InstanceId,
            ingredient);
    }

    private IngredientCollectionResult EvaluateCollection(
        string ingredientId,
        string instanceId,
        IngredientIdentity ingredient)
    {
        if (!missionAccepted || currentStepId != CollectGardenStepId)
        {
            Debug.LogWarning(
                "[เป้าหมาย] ยังไม่ถึงขั้นเก็บวัตถุดิบจากสวน",
                this);
            return IngredientCollectionResult.WrongObjective;
        }

        if (!IsKrapowLeaf(ingredientId))
        {
            Debug.LogWarning(
                $"[เป้าหมาย] วัตถุดิบไม่ถูกต้อง: {ingredientId} " +
                "ขั้นนี้ต้องเก็บใบกะเพรา",
                this);
            return IngredientCollectionResult.WrongIngredient;
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            Debug.LogError(
                "[เป้าหมาย] วัตถุดิบไม่มี Instance ID จึงไม่สามารถนับได้",
                this);
            return IngredientCollectionResult.InvalidIngredient;
        }

        if (collectedInstanceIds.Contains(instanceId))
        {
            Debug.LogWarning(
                "[เป้าหมาย] ใบกะเพราชิ้นนี้ถูกนับแล้ว จึงไม่นับซ้ำ",
                this);
            return IngredientCollectionResult.Duplicate;
        }

        if (ingredient != null && !ingredient.TryCollect())
        {
            Debug.LogWarning(
                "[เป้าหมาย] ใบกะเพราชิ้นนี้เก็บไปแล้ว จึงไม่นับซ้ำ",
                this);
            return IngredientCollectionResult.Duplicate;
        }

        collectedInstanceIds.Add(instanceId);
        collectedKrapowCount++;

        Debug.Log(
            $"[เป้าหมาย] เก็บใบกะเพราถูกต้อง: " +
            $"{collectedKrapowCount}/{requiredKrapowCount} กำ",
            this);

        if (collectedKrapowCount >= requiredKrapowCount)
        {
            Debug.Log(
                "[เป้าหมาย] เก็บใบกะเพราครบแล้ว",
                this);
        }

        onObjectiveChanged?.Invoke();
        return IngredientCollectionResult.Correct;
    }

    private bool IsKrapowLeaf(string ingredientId)
    {
        return string.Equals(
                   ingredientId,
                   KrapowLeafIngredientId,
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   ingredientId,
                   LegacyKrapowName,
                   StringComparison.OrdinalIgnoreCase);
    }

    public void ResetObjectives()
    {
        missionAccepted = false;
        currentStepId = AcceptMissionStepId;
        collectedKrapowCount = 0;
        collectedInstanceIds.Clear();

        IngredientIdentity[] ingredients =
            FindObjectsByType<IngredientIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (IngredientIdentity ingredient in ingredients)
        {
            ingredient.ResetIngredient();
        }

        Debug.Log(
            "[เป้าหมาย] รีเซ็ตแล้ว: จำนวนกะเพรา 0/1 กำ " +
            "และกลับไปที่ขั้นรับภารกิจ",
            this);

        onObjectiveChanged?.Invoke();
    }

    [ContextMenu("Test/Accept Mission")]
    private void TestAcceptMission()
    {
        TryAcceptMission();
    }

    [ContextMenu("Test/Collect Krapow")]
    private void TestCollectKrapow()
    {
        IngredientIdentity[] ingredients =
            FindObjectsByType<IngredientIdentity>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        foreach (IngredientIdentity ingredient in ingredients)
        {
            if (!ingredient.MatchesIngredientId(KrapowLeafIngredientId))
            {
                continue;
            }

            TryCollectIngredient(ingredient);
            return;
        }

        Debug.LogError(
            "[ทดสอบ] ไม่พบวัตถุดิบที่มี ingredientId = krapow_leaf",
            this);
    }

    [ContextMenu("Test/Wrong Ingredient")]
    private void TestWrongIngredient()
    {
        EvaluateCollection(
            "wrong_ingredient",
            "test-wrong-ingredient",
            null);
    }

    [ContextMenu("Test/Reset Objectives")]
    private void TestResetObjectives()
    {
        ResetObjectives();
    }
}
