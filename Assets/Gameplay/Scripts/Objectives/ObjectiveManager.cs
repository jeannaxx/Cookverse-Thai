using System;
using System.Collections.Generic;
using System.Linq;
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
    public const string WashStepId = "wash_ingredients";
    public const string PrepareStepId = "prepare_ingredients";
    // ขั้นปรุงแบ่ง 3 ขั้นตาม "ลำดับการปรุงที่ระบบกฎตรวจ" ใน GAMEPLAY_CONTENT_V1
    // วัตถุดิบที่ใส่ผิดลำดับยังนับให้ขั้นของมัน (CookingOrder บันทึกข้อผิดพลาดแยก)
    public const string CookAromaticsStepId = "cook_aromatics";
    public const string CookChickenStepId = "cook_chicken";
    public const string CookSeasonStepId = "cook_season";
    public const string CookStepId = CookChickenStepId; // ชื่อเดิม คงไว้ให้โค้ดเก่าใช้ได้
    public const string KrapowLeafIngredientId = "krapow_leaf";
    public const string ChiliIngredientId = "chili";
    public const string ChickenIngredientId = "chicken";
    public const string GarlicIngredientId = "garlic";
    public const string CookingOilIngredientId = "cooking_oil";
    public const string OysterSauceIngredientId = "oyster_sauce";
    public const string FishSauceIngredientId = "fish_sauce";
    public const string SugarIngredientId = "sugar";
    // การคนนับเป็นรายการในตารางเหมือนวัตถุดิบ (สถานะ Cooking) เพื่อให้ Checklist แสดงได้
    public const string StirAromaticsId = "stir_aromatics";
    public const string StirFinalId = "stir_final";
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
        else if (FindStep(CookAromaticsStepId) == null)
        {
            // ฉากที่บันทึกตารางรุ่นก่อนมีขั้นปรุง (cook_krapow) ยังเล่นได้
            // รัน Tools > Cookverse Thai > M1 > Setup Pan Cooking เพื่อบันทึกตารางใหม่ลงฉาก
            Debug.LogWarning(
                "[เป้าหมาย] ตารางขั้นภารกิจในฉากเป็นรุ่นเก่า จึงใช้ตารางล่าสุด (มีขั้นปรุง 3 ขั้น)",
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
                    3)),
            // ล้างเฉพาะผัก ไม่ล้างไก่ดิบ (หลักความปลอดภัยอาหาร)
            new MissionStepDefinition(
                WashStepId,
                "ล้างผักที่อ่างล้าง",
                new IngredientRequirement(
                    KrapowLeafIngredientId,
                    LegacyKrapowName,
                    "ใบกะเพรา",
                    "กำ",
                    1,
                    IngredientState.Washed),
                new IngredientRequirement(
                    ChiliIngredientId,
                    LegacyChiliName,
                    "พริก",
                    "เม็ด",
                    3,
                    IngredientState.Washed)),
            new MissionStepDefinition(
                PrepareStepId,
                "หั่นวัตถุดิบบนเขียง",
                new IngredientRequirement(
                    ChickenIngredientId,
                    string.Empty,
                    "เนื้อไก่",
                    "ชิ้น",
                    1,
                    IngredientState.Cut),
                new IngredientRequirement(
                    ChiliIngredientId,
                    LegacyChiliName,
                    "พริก",
                    "เม็ด",
                    3,
                    IngredientState.Cut),
                new IngredientRequirement(
                    GarlicIngredientId,
                    string.Empty,
                    "กระเทียม",
                    "กลีบ",
                    3,
                    IngredientState.Cut)),
            // ปรุง: น้ำมัน → กระเทียม+พริก → คน → ไก่จนสุก → ซอส/น้ำปลา/น้ำตาล → กะเพรา → คน
            new MissionStepDefinition(
                CookAromaticsStepId,
                "เปิดไฟ ผัดเครื่องหอม",
                new IngredientRequirement(
                    CookingOilIngredientId,
                    string.Empty,
                    "น้ำมัน",
                    "ครั้ง",
                    1,
                    IngredientState.AddedToPan),
                new IngredientRequirement(
                    GarlicIngredientId,
                    string.Empty,
                    "กระเทียม",
                    "กลีบ",
                    3,
                    IngredientState.AddedToPan),
                new IngredientRequirement(
                    ChiliIngredientId,
                    LegacyChiliName,
                    "พริก",
                    "เม็ด",
                    3,
                    IngredientState.AddedToPan),
                new IngredientRequirement(
                    StirAromaticsId,
                    string.Empty,
                    "คนเครื่องหอม",
                    "ครั้ง",
                    1,
                    IngredientState.Cooking)),
            new MissionStepDefinition(
                CookChickenStepId,
                "ใส่ไก่ ผัดจนสุก",
                new IngredientRequirement(
                    ChickenIngredientId,
                    string.Empty,
                    "เนื้อไก่",
                    "ชิ้น",
                    1,
                    IngredientState.Cooked)),
            new MissionStepDefinition(
                CookSeasonStepId,
                "ปรุงรส แล้วใส่ใบกะเพรา",
                new IngredientRequirement(
                    OysterSauceIngredientId,
                    string.Empty,
                    "ซอสหอยนางรม",
                    "ครั้ง",
                    1,
                    IngredientState.AddedToPan),
                new IngredientRequirement(
                    FishSauceIngredientId,
                    string.Empty,
                    "น้ำปลา",
                    "ครั้ง",
                    1,
                    IngredientState.AddedToPan),
                new IngredientRequirement(
                    SugarIngredientId,
                    string.Empty,
                    "น้ำตาล",
                    "ครั้ง",
                    1,
                    IngredientState.AddedToPan),
                new IngredientRequirement(
                    KrapowLeafIngredientId,
                    LegacyKrapowName,
                    "ใบกะเพรา",
                    "กำ",
                    1,
                    IngredientState.AddedToPan),
                new IngredientRequirement(
                    StirFinalId,
                    string.Empty,
                    "คนให้เข้ากัน",
                    "ครั้ง",
                    1,
                    IngredientState.Cooking))
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

    // เปลี่ยนสถานะวัตถุดิบตามขั้นภารกิจปัจจุบัน ใช้กับอ่างล้าง (Washed)
    // เขียง (Cut) และกระทะ (Cooked) แต่ละขั้นนับจากตาราง Mission Steps
    public IngredientCollectionResult TryAdvanceIngredient(
        IngredientIdentity ingredient,
        IngredientState targetState)
    {
        string action = ActionFor(targetState);
        string stepId = currentStepId;

        if (ingredient == null)
        {
            ActionLog.Record(action, stepId, isCorrect: false,
                value: IngredientCollectionResult.InvalidIngredient.ToString());
            return IngredientCollectionResult.InvalidIngredient;
        }

        MissionStepDefinition step = CurrentStep;
        IngredientState before = ingredient.CurrentState;

        IngredientCollectionResult Reject(IngredientCollectionResult result, string reason)
        {
            Debug.LogWarning($"[เป้าหมาย] {ingredient.DisplayNameThai}: {reason}", this);
            ActionLog.Record(action, stepId, ingredient, before, before,
                isCorrect: false, value: result.ToString());
            return result;
        }

        if (!missionAccepted || step == null || !step.HasRequirementsFor(targetState))
        {
            return Reject(IngredientCollectionResult.WrongObjective,
                $"ขั้นปัจจุบันคือ {step?.titleThai ?? currentStepId}");
        }

        IngredientRequirement requirement =
            step.FindRequirement(ingredient.IngredientId, targetState);

        if (requirement == null)
        {
            return Reject(IngredientCollectionResult.WrongIngredient,
                $"ขั้นนี้ไม่ต้องใช้วัตถุดิบนี้ ({step.DescribeRequirements(targetState)})");
        }

        if (before == targetState || requirement.IsComplete)
        {
            return Reject(IngredientCollectionResult.Duplicate, "ทำขั้นนี้กับชิ้นนี้ไปแล้ว");
        }

        if (!CanMoveTo(before, targetState))
        {
            return Reject(IngredientCollectionResult.WrongIngredient,
                $"ยังทำไม่ได้จากสถานะ {before}");
        }

        ingredient.TrySetState(targetState);
        requirement.currentCount++;

        Debug.Log(
            $"[เป้าหมาย] {step.titleThai}: {requirement.displayNameThai} " +
            $"{requirement.currentCount}/{requirement.requiredCount} {requirement.unitThai}",
            this);

        ActionLog.Record(action, stepId, ingredient, before, targetState,
            isCorrect: true,
            value: $"{requirement.currentCount}/{requirement.requiredCount}");

        if (step.IsComplete)
        {
            AdvanceFrom(step);
        }

        NotifyObjectiveChanged();
        return IngredientCollectionResult.Correct;
    }

    public static bool IsCookingStep(string stepId)
    {
        return stepId == CookAromaticsStepId ||
               stepId == CookChickenStepId ||
               stepId == CookSeasonStepId;
    }

    // ใส่ของลงกระทะ/เทเครื่องปรุง/คน ได้ตั้งแต่ถึงขั้นปรุงขั้นแรก
    public bool CanCookNow(out string reason)
    {
        if (!missionAccepted || !IsCookingStep(currentStepId))
        {
            reason = "ยังไม่ถึงขั้นปรุง เตรียมวัตถุดิบให้ครบก่อน";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    // นับความคืบหน้าขั้นปรุง ไม่บังคับลำดับ: หา requirement ในขั้นปรุงปัจจุบันและขั้นปรุงถัดไป
    // เช่น ใส่ซอสก่อนไก่สุก ยังนับให้ขั้นปรุงรส (ข้อผิดพลาดลำดับบันทึกโดย CookingOrder)
    // ไม่บันทึก ActionLog ที่นี่ ผู้เรียก (PanContents) บันทึกเหตุการณ์เอง
    public bool RecordCookingProgress(string ingredientId, IngredientState state)
    {
        if (!missionAccepted || !IsCookingStep(currentStepId) || steps == null)
        {
            return false;
        }

        int start = steps.FindIndex(s => s.stepId == currentStepId);

        for (int i = Mathf.Max(0, start); i < steps.Count; i++)
        {
            MissionStepDefinition step = steps[i];

            if (!IsCookingStep(step.stepId))
            {
                break;
            }

            IngredientRequirement requirement = step.FindRequirement(ingredientId, state);

            if (requirement == null || requirement.IsComplete)
            {
                continue;
            }

            requirement.currentCount++;
            Debug.Log(
                $"[เป้าหมาย] {step.titleThai}: {requirement.displayNameThai} " +
                $"{requirement.currentCount}/{requirement.requiredCount} {requirement.unitThai}",
                this);

            AdvanceWhileComplete();
            NotifyObjectiveChanged();
            return true;
        }

        return false;
    }

    private void AdvanceWhileComplete()
    {
        MissionStepDefinition step = CurrentStep;

        while (step != null && step.IsComplete)
        {
            MissionStepDefinition next = GetNextStep(step.stepId);
            AdvanceFrom(step);

            if (next == null)
            {
                break;
            }

            step = next;
        }
    }

    // ใส่ลงกระทะได้เฉพาะตอนถึงขั้นปรุง และวัตถุดิบต้องเตรียมแล้ว (ล้างหรือหั่น)
    public bool CanAddToPan(IngredientIdentity ingredient, out string reason)
    {
        if (!CanCookNow(out reason))
        {
            return false;
        }

        if (ingredient.CurrentState != IngredientState.Washed &&
            ingredient.CurrentState != IngredientState.Cut)
        {
            reason = "วัตถุดิบนี้ยังไม่ได้ล้างหรือหั่น";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool CanMoveTo(IngredientState from, IngredientState to)
    {
        return to switch
        {
            IngredientState.Washed => from == IngredientState.Collected,
            IngredientState.Cut => from == IngredientState.Collected ||
                                   from == IngredientState.Washed,
            IngredientState.Cooked => from == IngredientState.AddedToPan ||
                                      from == IngredientState.Cooking,
            _ => false
        };
    }

    private static string ActionFor(IngredientState state)
    {
        return state switch
        {
            IngredientState.Washed => ActionLog.Actions.IngredientWashed,
            IngredientState.Cut => ActionLog.Actions.IngredientCut,
            IngredientState.Collected => ActionLog.Actions.IngredientCollected,
            _ => ActionLog.Actions.FoodStateChanged
        };
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

        // อ่างล้าง เขียง เตา กระทะ ตู้เย็น กลับสู่สภาพเริ่มต้น
        foreach (IMissionResettable resettable in
                 FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include)
                     .OfType<IMissionResettable>())
        {
            resettable.ResetForMission();
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

    // ใช้ตารางขั้นภารกิจล่าสุดจากโค้ด (เรียกจากเครื่องมือ Editor หรือเมนูทดสอบ)
    [ContextMenu("Test/Restore Default Steps")]
    public void ApplyDefaultSteps()
    {
        steps = CreateDefaultSteps();
        ClearProgress();
        Debug.Log("[เป้าหมาย] ใช้ตารางขั้นภารกิจค่าเริ่มต้นแล้ว", this);
    }

    private void NotifyObjectiveChanged()
    {
        onObjectiveChanged?.Invoke();
        ObjectiveChanged?.Invoke();
    }
}
