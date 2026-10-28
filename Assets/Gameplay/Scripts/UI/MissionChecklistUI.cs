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

    // ในแว่น Canvas แบบ Screen Space Overlay จะไปติดขอบเลนส์ (WebXR) หรือไม่แสดงเลย (Quest APK)
    // ตอนเริ่มเกมจึงแปลงเป็น World Space แล้วลอยตามสายตาแบบหน่วง (ไม่ล็อกติดหน้า)
    [Header("VR Panel (Lazy Follow)")]
    [SerializeField] private bool convertToWorldSpace = true;
    [SerializeField] private float panelDistance = 1.1f;   // ระยะจากตา (ม.)
    [SerializeField] private float panelHeight = -0.2f;    // ต่ำกว่าระดับสายตา (ม.)
    [SerializeField] private float panelSide = -0.3f;      // ลบ = ไปทางซ้าย (ม.)
    [SerializeField] private float panelScale = 0.001f;    // 1 px = 1 มม.
    [SerializeField] private float recenterAngle = 40f;    // หันเกินกี่องศาแล้วป้ายค่อยตามมา
    [SerializeField] private float followSpeed = 3f;

    private bool lazyFollow;
    private bool following;
    private Vector3 panelTarget;

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

    private void Start()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (!convertToWorldSpace || canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            return;
        }

        Camera cam = Camera.main;
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.dynamicPixelsPerUnit = 3f; // ตัวหนังสือคมขึ้นใน World Space
        }

        var rect = (RectTransform)transform;
        rect.sizeDelta = new Vector2(560f, 360f);
        rect.localScale = Vector3.one * panelScale;

        // ย้ายแผงเช็กลิสต์ (เดิมชิดมุมขวาบนของจอ) มาไว้กลาง Canvas
        foreach (RectTransform child in rect)
        {
            child.anchorMin = child.anchorMax = child.pivot = new Vector2(0.5f, 0.5f);
            child.anchoredPosition = Vector2.zero;
        }

        lazyFollow = true;
        if (cam != null)
        {
            panelTarget = TargetFor(cam.transform);
            transform.position = panelTarget;
            FaceCamera(cam.transform);
        }
    }

    private void LateUpdate()
    {
        if (lazyFollow)
        {
            LazyFollow();
            return;
        }

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

    private void LazyFollow()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Transform head = cam.transform;
        Vector3 toPanel = transform.position - head.position;
        Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        Vector3 flatToPanel = Vector3.ProjectOnPlane(toPanel, Vector3.up);
        float angle = Vector3.Angle(flatForward, flatToPanel);
        float distanceError = Mathf.Abs(flatToPanel.magnitude - panelDistance);

        // ป้ายอยู่นิ่ง ๆ จนกว่าจะหันไปไกลหรือเดินห่างเกิน แล้วค่อยลอยตามมาเบา ๆ
        if (!following && (angle > recenterAngle || distanceError > 0.6f))
        {
            following = true;
        }

        if (following)
        {
            panelTarget = TargetFor(head);
            transform.position = Vector3.Lerp(transform.position, panelTarget, Time.deltaTime * followSpeed);
            if ((transform.position - panelTarget).sqrMagnitude < 0.0004f)
            {
                following = false;
            }
        }
        else
        {
            // ความสูงตามหัวเสมอ (เช่น ตอนขึ้นบันไดหรือก้ม)
            Vector3 p = transform.position;
            p.y = Mathf.Lerp(p.y, head.position.y + panelHeight, Time.deltaTime * followSpeed);
            transform.position = p;
        }

        FaceCamera(head);
    }

    private Vector3 TargetFor(Transform head)
    {
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.ProjectOnPlane(head.up, Vector3.up);
        }
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        return head.position + forward * panelDistance + right * panelSide + Vector3.up * panelHeight;
    }

    private void FaceCamera(Transform head)
    {
        Vector3 away = transform.position - head.position;
        if (away.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(away, Vector3.up);
        }
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
                        .Append(VerbFor(requirement.targetState))
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

    private static string VerbFor(IngredientState state)
    {
        return state switch
        {
            IngredientState.Washed => "ล้าง",
            IngredientState.Cut => "หั่น",
            IngredientState.Cooked => "ผัดจนสุก: ",
            _ => string.Empty
        };
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
