using UnityEngine;
using UnityEngine.UI;

public class MissionChecklistUI : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private Text checklistText;
    [SerializeField] private Transform followTarget;

    [Header("World Space Placement")]
    [SerializeField] private Vector3 localOffset =
        new Vector3(0.38f, 0.08f, 0.9f);

    public void Configure(
        ObjectiveManager objective,
        Text textComponent,
        Transform target)
    {
        Unsubscribe();
        objectiveManager = objective;
        checklistText = textComponent;
        followTarget = target;
        Subscribe();
        RefreshChecklist();
    }

    private void OnEnable()
    {
        EnsureThaiFont();
        Subscribe();
        RefreshChecklist();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void LateUpdate()
    {
        Canvas checklistCanvas = GetComponent<Canvas>();

        if (checklistCanvas != null &&
            checklistCanvas.renderMode != RenderMode.WorldSpace)
        {
            return;
        }

        if (followTarget == null)
        {
            return;
        }

        transform.position = followTarget.TransformPoint(localOffset);
        transform.rotation = followTarget.rotation;
    }

    public void RefreshChecklist()
    {
        if (checklistText == null)
        {
            return;
        }

        if (objectiveManager == null || !objectiveManager.MissionAccepted)
        {
            checklistText.text =
                "ภารกิจ: ผัดกะเพรา\n\n" +
                "เดินไปหาป้าแสงเพื่อรับภารกิจ";
            return;
        }

        // แสดงทุกขั้นที่มีรายการวัตถุดิบ ตั้งแต่ขั้นแรกจนถึงขั้นปัจจุบัน
        // ดึงจากตาราง Mission Steps จึงไม่ต้องแก้ UI เมื่อเพิ่มวัตถุดิบใหม่
        var builder = new System.Text.StringBuilder();
        builder.Append("ป้าแสง: ช่วยทำผัดกะเพราให้หน่อยจ้ะ\n");
        builder.Append("นำวัตถุดิบไปวางบนโต๊ะเตรียม\n");

        var steps = objectiveManager.Steps;
        string currentStepId = objectiveManager.CurrentStepId;

        for (int i = 0; i < steps.Count; i++)
        {
            MissionStepDefinition step = steps[i];

            if (step.HasRequirements)
            {
                builder.Append("\n").Append(step.titleThai).Append('\n');

                foreach (IngredientRequirement requirement in step.requirements)
                {
                    // ใช้เฉพาะสัญลักษณ์ที่ Sarabun มี (■ •) เพราะ ✓ □ ไม่มีในฟอนต์
                    builder
                        .Append(requirement.IsComplete ? "■ " : "• ")
                        .Append(requirement.displayNameThai).Append(' ')
                        .Append(requirement.currentCount).Append('/')
                        .Append(requirement.requiredCount)
                        .Append(requirement.IsComplete ? " ครบ" : string.Empty)
                        .Append('\n');
                }

                if (step.IsComplete)
                {
                    builder.Append("วัตถุดิบครบแล้ว\n");
                }
            }

            if (step.stepId == currentStepId)
            {
                break;
            }
        }

        string message = builder.ToString().TrimEnd();

        checklistText.text = message;
    }

    private void Subscribe()
    {
        if (objectiveManager != null)
        {
            objectiveManager.ObjectiveChanged -= RefreshChecklist;
            objectiveManager.ObjectiveChanged += RefreshChecklist;
        }
    }

    private void Unsubscribe()
    {
        if (objectiveManager != null)
        {
            objectiveManager.ObjectiveChanged -= RefreshChecklist;
        }
    }

    private void EnsureThaiFont()
    {
        if (checklistText == null)
        {
            return;
        }

        // ใช้ Sarabun ที่ฝังในโปรเจกต์เสมอ ฟอนต์เดิม (LegacyRuntime)
        // แสดงไทยได้บน Mac เท่านั้น
        Font thaiFont = ThaiFont.Get();

        if (thaiFont != null)
        {
            checklistText.font = thaiFont;
        }
    }
}
