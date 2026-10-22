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

// ควบคุมขั้นภารกิจจากตาราง Mission Steps
// แต่ละขั้นกำหนดว่าต้องการวัตถุดิบอะไร กี่ชิ้น และอยู่ในสถานะไหน
// เพิ่มขั้นใหม่ได้ในตารางโดยไม่ต้องเขียนเงื่อนไขแยกตามชนิดวัตถุดิบ
public class ObjectiveManager : MonoBehaviour
{
    public const string AcceptMissionStepId = "accept_mission";
    public const string CollectGardenStepId = "collect_garden";
    public const string CollectKitchenStepId = "collect_kitchen";
    public const string KrapowLeafIngredientId = "krapow_leaf";
    public const string ChiliIngredientId = "chili";
    public const string ChickenIngredientId = "chicken";
    public const string GarlicIngredientId = "garlic";
    public const string LegacyKrapowName = "Krapow";
    public const string LegacyChiliName = "Chili";

    [Header("Current Objective")]
    [SerializeField] private string currentStepId = AcceptMissionStepId;
    [SerializeField] private bool missionAccepted;

    [Header("Mission Steps")]
    [Tooltip("ลำดับขั้นภารกิจ เรียงจากขั้นแรกถึงขั้นสุดท้าย")]
    [SerializeField] private List<MissionStepDefinition> steps =
        CreateDefaultSteps();

    [Header("Events")]
    [SerializeField] private UnityEvent onObjectiveChanged;

    private readonly HashSet<string> collectedInstanceIds = new();

    public event Action ObjectiveChanged;

    public string CurrentStepId => currentStepId;
    public bool MissionAccepted => missionAccepted;
    public IReadOnlyList<MissionStepDefinition> Steps => steps;
    public MissionStepDefinition CurrentStep => FindStep(currentStepId);

    // ค่าเดิมที่ MissionChecklistUI และชุดทดสอบใช้อยู่
    public int CollectedKrapowCount =>
        GetCollectedCount(CollectGardenStepId, KrapowLeafIngredientId);
    public int RequiredKrapowCount =>
        GetRequiredCount(CollectGardenStepId, KrapowLeafIngredientId);
    public int CollectedChiliCount =>
        GetCollectedCount(CollectGardenStepId, ChiliIngredientId);
    public int RequiredChiliCount =>
        GetRequiredCount(CollectGardenStepId, ChiliIngredientId);
    public bool IsGardenCollectionComplete =>
        IsStepComplete(CollectGardenStepId);

    private void Awake()
    {
        if (steps == null || steps.Count == 0)
        {
            Debug.LogWarning(
                "[เป้าหมาย] ตารางขั้นภารกิจว่าง จึงใช้ค่าเริ่มต้นผัดกะเพราไก่",
                this);
            steps = CreateDefaultSteps();
        }
    }

    public static List<MissionStepDefinition> CreateDefaultSteps()
    {
        return new List<MissionStepDefinition>
        {
            new MissionStepDefinition(
                AcceptMissionStepId,
                "คุยกับป้าแสงเพื่อรับภารกิจ"),
            new MissionStepDefinition(
                CollectGardenStepId,
                "เก็บใบกะเพราและพริกจากสวน",
                new IngredientRequirement(
                    KrapowLeafIngredientId,
                    LegacyKrapowName,
                    "ใบกะเพรา",
                    "กำ",
                    1),
                new IngredientRequirement(
                    ChiliIngredientId,
                    LegacyChiliName,
                    "พริก",
                    "เม็ด",
                    3)),
            new MissionStepDefinition(
                CollectKitchenStepId,
                "หยิบไก่และกระเทียมในครัว",
                new IngredientRequirement(
                    ChickenIngredientId,
                    string.Empty,
                    "เนื้อไก่",
                    "ชิ้น",
                    1),
                new IngredientRequirement(
                    GarlicIngredientId,
                    string.Empty,
                    "กระเทียม",
                    "กลีบ",
                    3))
        };
    }

    public MissionStepDefinition FindStep(string stepId)
    {
        if (steps == null || string.IsNullOrEmpty(stepId))
        {
            return null;
        }

        return steps.Find(s => s.stepId == stepId);
    }

    public bool IsStepComplete(string stepId)
    {
        MissionStepDefinition step = FindStep(stepId);
        return step != null && step.IsComplete;
    }

    public int GetCollectedCount(string stepId, string ingredientId)
    {
        IngredientRequirement requirement =
            FindRequirementInStep(stepId, ingredientId);
        return requirement != null ? requirement.currentCount : 0;
    }

    public int GetRequiredCount(string stepId, string ingredientId)
    {
        IngredientRequirement requirement =
            FindRequirementInStep(stepId, ingredientId);
        return requirement != null ? requirement.requiredCount : 0;
    }

    // ใช้จากเครื่องมือ Editor เพื่อตั้งจำนวนที่ต้องการในตาราง
    public bool SetRequiredCount(
        string stepId,
        string ingredientId,
        int requiredCount)
    {
        IngredientRequirement requirement =
            FindRequirementInStep(stepId, ingredientId);

        if (requirement == null)
        {
            return false;
        }

        requirement.requiredCount = Mathf.Max(1, requiredCount);
        return true;
    }

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
        MissionStepDefinition nextStep = GetNextStep(AcceptMissionStepId);
        currentStepId = nextStep != null ? nextStep.stepId : currentStepId;

        Debug.Log(
            "[เป้าหมาย] รับภารกิจสำเร็จ เป้าหมายปัจจุบัน: " +
            (nextStep != null ? nextStep.titleThai : currentStepId),
            this);

        ActionLog.Record(
            ActionLog.Actions.MissionAccepted,
            AcceptMissionStepId);

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
            ActionLog.Record(
                ActionLog.Actions.IngredientCollected,
                currentStepId,
                isCorrect: false,
                value: IngredientCollectionResult.InvalidIngredient.ToString());
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
        string stepId = currentStepId;
        MissionStepDefinition step = CurrentStep;
        IngredientState? stateBefore = ingredient != null
            ? (IngredientState?)ingredient.CurrentState
            : null;

        if (missionAccepted &&
            !string.IsNullOrWhiteSpace(instanceId) &&
            collectedInstanceIds.Contains(instanceId))
        {
            Debug.LogWarning(
                "[เป้าหมาย] วัตถุดิบชิ้นนี้ถูกนับแล้ว จึงไม่นับซ้ำ",
                this);
            return RecordRejected(
                IngredientCollectionResult.Duplicate,
                stepId, ingredientId, instanceId, stateBefore);
        }

        if (!missionAccepted ||
            step == null ||
            !step.HasRequirementsFor(IngredientState.Collected))
        {
            Debug.LogWarning(
                "[เป้าหมาย] ขั้นปัจจุบันยังไม่ใช่ขั้นเก็บวัตถุดิบ",
                this);
            return RecordRejected(
                IngredientCollectionResult.WrongObjective,
                stepId, ingredientId, instanceId, stateBefore);
        }

        IngredientRequirement requirement =
            step.FindRequirement(ingredientId, IngredientState.Collected);

        if (requirement == null)
        {
            Debug.LogWarning(
                $"[เป้าหมาย] วัตถุดิบไม่ถูกต้อง: {ingredientId} " +
                $"ขั้นนี้ต้องเก็บ{step.DescribeRequirements(IngredientState.Collected)}",
                this);
            return RecordRejected(
                IngredientCollectionResult.WrongIngredient,
                stepId, ingredientId, instanceId, stateBefore);
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            Debug.LogError(
                "[เป้าหมาย] วัตถุดิบไม่มี Instance ID จึงไม่สามารถนับได้",
                this);
            return RecordRejected(
                IngredientCollectionResult.InvalidIngredient,
                stepId, ingredientId, instanceId, stateBefore);
        }

        if (ingredient != null && !ingredient.TryCollect())
        {
            Debug.LogWarning(
                "[เป้าหมาย] วัตถุดิบชิ้นนี้เก็บไปแล้ว จึงไม่นับซ้ำ",
                this);
            return RecordRejected(
                IngredientCollectionResult.Duplicate,
                stepId, ingredientId, instanceId, stateBefore);
        }

        collectedInstanceIds.Add(instanceId);
        requirement.currentCount++;

        Debug.Log(
            $"[เป้าหมาย] เก็บ{requirement.displayNameThai}ถูกต้อง: " +
            $"{requirement.currentCount}/{requirement.requiredCount} " +
            requirement.unitThai,
            this);

        ActionLog.Record(
            ActionLog.Actions.IngredientCollected,
            stepId,
            ingredientId,
            instanceId,
            stateBefore,
            ingredient != null ? ingredient.CurrentState : IngredientState.Collected,
            isCorrect: true,
            value: $"{requirement.currentCount}/{requirement.requiredCount}");

        if (step.IsComplete)
        {
            AdvanceFrom(step);
        }

        NotifyObjectiveChanged();
        return IngredientCollectionResult.Correct;
    }

    private IngredientCollectionResult RecordRejected(
        IngredientCollectionResult result,
        string stepId,
        string ingredientId,
        string instanceId,
        IngredientState? state)
    {
        ActionLog.Record(
            ActionLog.Actions.IngredientCollected,
            stepId,
            ingredientId,
            instanceId,
            state,
            state,
            isCorrect: false,
            value: result.ToString());
        return result;
    }

    private void AdvanceFrom(MissionStepDefinition completedStep)
    {
        MissionStepDefinition nextStep = GetNextStep(completedStep.stepId);

        if (nextStep == null)
        {
            Debug.Log(
                $"[เป้าหมาย] {completedStep.titleThai}ครบแล้ว " +
                "และเป็นขั้นสุดท้ายในตาราง",
                this);
            return;
        }

        currentStepId = nextStep.stepId;
        Debug.Log(
            $"[เป้าหมาย] {completedStep.titleThai}ครบแล้ว " +
            $"เป้าหมายถัดไป: {nextStep.titleThai}",
            this);
    }

    private MissionStepDefinition GetNextStep(string stepId)
    {
        if (steps == null)
        {
            return null;
        }

        int index = steps.FindIndex(s => s.stepId == stepId);
        return index >= 0 && index + 1 < steps.Count ? steps[index + 1] : null;
    }

    private IngredientRequirement FindRequirementInStep(
        string stepId,
        string ingredientId)
    {
        MissionStepDefinition step = FindStep(stepId);

        if (step == null || !step.HasRequirements)
        {
            return null;
        }

        return step.requirements.Find(r => r.Matches(ingredientId));
    }

    // ล้างเฉพาะความคืบหน้าของตัวนี้ ไม่แตะวัตถุอื่นในฉาก
    // ใช้ได้ทั้งตอนเล่นและจากเครื่องมือ Editor
    public void ClearProgress()
    {
        missionAccepted = false;
        currentStepId = steps != null && steps.Count > 0
            ? steps[0].stepId
            : AcceptMissionStepId;
        collectedInstanceIds.Clear();

        if (steps == null)
        {
            return;
        }

        foreach (MissionStepDefinition step in steps)
        {
            if (step.requirements == null)
            {
                continue;
            }

            foreach (IngredientRequirement requirement in step.requirements)
            {
                requirement.currentCount = 0;
            }
        }
    }

    public void ResetObjectives()
    {
        ClearProgress();

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
            "[เป้าหมาย] รีเซ็ตแล้ว: จำนวนทุกขั้นกลับเป็น 0 " +
            "วัตถุดิบกลับตำแหน่งเริ่มต้น " +
            "และกลับไปที่ขั้นรับภารกิจ",
            this);

        ActionLog.Record(
            ActionLog.Actions.MissionReset,
            currentStepId);

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
        TestCollect(KrapowLeafIngredientId);
    }

    [ContextMenu("Test/Collect Chili")]
    private void TestCollectChili()
    {
        TestCollect(ChiliIngredientId);
    }

    private void TestCollect(string ingredientId)
    {
        IngredientIdentity[] ingredients =
            FindObjectsByType<IngredientIdentity>(
                FindObjectsInactive.Include);

        foreach (IngredientIdentity ingredient in ingredients)
        {
            if (!ingredient.MatchesIngredientId(ingredientId) ||
                ingredient.CurrentState != IngredientState.Raw)
            {
                continue;
            }

            TryCollectIngredient(ingredient);
            return;
        }

        Debug.LogError(
            $"[ทดสอบ] ไม่พบวัตถุดิบที่ยังไม่เก็บซึ่งมี ingredientId = {ingredientId}",
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

    [ContextMenu("Test/Restore Default Steps")]
    private void RestoreDefaultSteps()
    {
        steps = CreateDefaultSteps();
        ClearProgress();
        Debug.Log("[เป้าหมาย] คืนตารางขั้นภารกิจเป็นค่าเริ่มต้นแล้ว", this);
    }

    private void NotifyObjectiveChanged()
    {
        onObjectiveChanged?.Invoke();
        ObjectiveChanged?.Invoke();
    }
}
