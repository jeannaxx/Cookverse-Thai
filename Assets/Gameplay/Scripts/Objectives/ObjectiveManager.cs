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
    public const string IngredientsReadyStepId = "ingredients_ready";
    public const string KrapowLeafIngredientId = "krapow_leaf";
    public const string ChiliIngredientId = "chili";
    public const string LegacyKrapowName = "Krapow";
    public const string LegacyChiliName = "Chili";

    [Header("Current Objective")]
    [SerializeField] private string currentStepId = AcceptMissionStepId;
    [SerializeField] private bool missionAccepted;

    [Header("Garden Collection")]
    [SerializeField] private int requiredKrapowCount = 1;
    [SerializeField] private int collectedKrapowCount;
    [SerializeField] private int requiredChiliCount = 1;
    [SerializeField] private int collectedChiliCount;

    [Header("Events")]
    [SerializeField] private UnityEvent onObjectiveChanged;

    private readonly HashSet<string> collectedInstanceIds = new();

    public event Action ObjectiveChanged;

    public string CurrentStepId => currentStepId;
    public bool MissionAccepted => missionAccepted;
    public int CollectedKrapowCount => collectedKrapowCount;
    public int RequiredKrapowCount => requiredKrapowCount;
    public int CollectedChiliCount => collectedChiliCount;
    public int RequiredChiliCount => requiredChiliCount;
    public bool IsGardenCollectionComplete =>
        collectedKrapowCount >= requiredKrapowCount &&
        collectedChiliCount >= requiredChiliCount;

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

        NotifyObjectiveChanged();
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

        bool isKrapowLeaf = IsKrapowLeaf(ingredientId);
        bool isChili = IsChili(ingredientId);

        if (!isKrapowLeaf && !isChili)
        {
            Debug.LogWarning(
                $"[เป้าหมาย] วัตถุดิบไม่ถูกต้อง: {ingredientId} " +
                "ขั้นนี้ต้องเก็บใบกะเพราและพริก",
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
                "[เป้าหมาย] วัตถุดิบชิ้นนี้ถูกนับแล้ว จึงไม่นับซ้ำ",
                this);
            return IngredientCollectionResult.Duplicate;
        }

        if (ingredient != null && !ingredient.TryCollect())
        {
            Debug.LogWarning(
                "[เป้าหมาย] วัตถุดิบชิ้นนี้เก็บไปแล้ว จึงไม่นับซ้ำ",
                this);
            return IngredientCollectionResult.Duplicate;
        }

        collectedInstanceIds.Add(instanceId);

        if (isKrapowLeaf)
        {
            collectedKrapowCount++;
            Debug.Log(
                $"[เป้าหมาย] เก็บใบกะเพราถูกต้อง: " +
                $"{collectedKrapowCount}/{requiredKrapowCount} กำ",
                this);
        }
        else
        {
            collectedChiliCount++;
            Debug.Log(
                $"[เป้าหมาย] เก็บพริกถูกต้อง: " +
                $"{collectedChiliCount}/{requiredChiliCount} ชิ้น",
                this);
        }

        if (IsGardenCollectionComplete)
        {
            currentStepId = IngredientsReadyStepId;
            Debug.Log(
                "[เป้าหมาย] เก็บใบกะเพราและพริกครบแล้ว " +
                "พร้อมเข้าสู่ขั้นเตรียมวัตถุดิบ",
                this);
        }

        NotifyObjectiveChanged();
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

    private bool IsChili(string ingredientId)
    {
        return string.Equals(
                   ingredientId,
                   ChiliIngredientId,
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   ingredientId,
                   LegacyChiliName,
                   StringComparison.OrdinalIgnoreCase);
    }

    public void ResetObjectives()
    {
        missionAccepted = false;
        currentStepId = AcceptMissionStepId;
        collectedKrapowCount = 0;
        collectedChiliCount = 0;
        collectedInstanceIds.Clear();

        IngredientIdentity[] ingredients =
            FindObjectsByType<IngredientIdentity>(
                FindObjectsInactive.Include);

        foreach (IngredientIdentity ingredient in ingredients)
        {
            ingredient.ResetIngredient();
        }

        IngredientCollector[] collectors =
            FindObjectsByType<IngredientCollector>(
                FindObjectsInactive.Include);

        foreach (IngredientCollector collector in collectors)
        {
            collector.ResetCollector();
        }

        CollectionFeedbackUI[] feedbackUIs =
            FindObjectsByType<CollectionFeedbackUI>(
                FindObjectsInactive.Include);

        foreach (CollectionFeedbackUI feedbackUI in feedbackUIs)
        {
            feedbackUI.ResetFeedback();
        }

        Debug.Log(
            "[เป้าหมาย] รีเซ็ตแล้ว: ใบกะเพรา 0/1 กำ พริก 0/1 ชิ้น " +
            "วัตถุดิบกลับตำแหน่งเริ่มต้น " +
            "และกลับไปที่ขั้นรับภารกิจ",
            this);

        NotifyObjectiveChanged();
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
                FindObjectsInactive.Include);

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

    [ContextMenu("Test/Collect Chili")]
    private void TestCollectChili()
    {
        IngredientIdentity[] ingredients =
            FindObjectsByType<IngredientIdentity>(
                FindObjectsInactive.Include);

        foreach (IngredientIdentity ingredient in ingredients)
        {
            if (!ingredient.MatchesIngredientId(ChiliIngredientId))
            {
                continue;
            }

            TryCollectIngredient(ingredient);
            return;
        }

        Debug.LogError(
            "[ทดสอบ] ไม่พบวัตถุดิบที่มี ingredientId = chili",
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

    private void NotifyObjectiveChanged()
    {
        onObjectiveChanged?.Invoke();
        ObjectiveChanged?.Invoke();
    }
}
