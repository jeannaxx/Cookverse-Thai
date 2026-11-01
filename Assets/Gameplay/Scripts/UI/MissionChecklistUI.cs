using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

public class MissionChecklistUI : MonoBehaviour
{
    public enum PanelMode
    {
        Wrist,  // ติดข้อมือซ้าย ขึ้นเมื่อหันหลังมือมาดู (ค่าเริ่มต้น ตั้งแต่ 2 พ.ย.)
        Follow  // ลอยหน้าแบบหน่วง (แบบเดิม)
    }

    // M5 (2 พ.ย.): ป้ายติดข้อมือซ้ายแบบนาฬิกา ไม่บังสายตา ไม่จมพื้น/ผนัง
    // ถ้าหา Controller ซ้ายไม่เจอ (เช่น โหมด Hand Tracking) จะกลับไปใช้แบบ Follow เอง
    [Header("Panel Mode")]
    [SerializeField] private PanelMode panelMode = PanelMode.Wrist;

    [Header("Wrist Panel (Left Controller)")]
    [SerializeField] private float wristScale = 0.0003f;                         // 560 px → กว้าง ~17 ซม.
    [SerializeField] private Vector3 wristLocalOffset = new Vector3(-0.05f, 0.02f, -0.09f); // จาก Controller ซ้าย: ด้านหลังมือ ค่อนไปทางข้อมือ (ม.)
    [SerializeField] private Vector3 wristFaceLocal = new Vector3(-1f, 0.3f, 0f);           // ด้านหลังมือซ้าย (ทิศที่หันหาหน้าตอนดูนาฬิกา)
    [SerializeField] private float showDot = 0.45f;          // หันหลังมือเข้าหาหน้ามากกว่านี้ → ป้ายขึ้น
    [SerializeField] private float hideDot = 0.3f;           // น้อยกว่านี้ → ป้ายหาย (กันกะพริบ)
    [SerializeField] private float showOnChangeSeconds = 4f; // ภารกิจเปลี่ยนขั้น → ป้ายขึ้นเองชั่วคราว

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

    // M5 (2 พ.ย.): กันป้ายจมพื้น/จมเข้าผนัง ที่เจอจากการทดสอบ WebXR
    [Header("VR Panel (Visibility)")]
    [SerializeField] private float minHeightAboveFloor = 0.15f; // ขอบล่างของป้ายต้องสูงจากพื้นที่ยืนอย่างน้อยเท่านี้ (ม.)
    [SerializeField] private bool avoidWalls = true;           // มีผนัง/เคาน์เตอร์คั่นระหว่างหัวกับป้าย → ดึงป้ายมาไว้หน้าวัตถุนั้น
    [SerializeField] private float minPanelDistance = 0.45f;   // ดึงเข้ามาใกล้สุดเท่านี้ (ม.)
    [SerializeField] private bool drawOnTop = false;           // วาดทับทุกอย่าง (ปิดไว้: ทับมือ/Controller ด้วย)

    private XROrigin xrOrigin;
    private bool searchedOrigin;
    private Material onTopMaterial;
    private readonly RaycastHit[] rayHits = new RaycastHit[16];

    private XRInputModalityManager modality;
    private bool searchedModality;
    private Transform fallbackLeftController;
    private CanvasGroup canvasGroup;
    private Canvas panelCanvas;
    private bool wristVisible;
    private float forceShowUntil;

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

        if (drawOnTop)
        {
            ApplyOnTopMaterial();
        }

        panelCanvas = canvas;
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (panelMode == PanelMode.Wrist && scaler != null)
        {
            scaler.dynamicPixelsPerUnit = 8f; // ป้ายเล็กอยู่ใกล้ตา ต้องคมกว่าเดิม
        }

        // ตอนเริ่มเกมโชว์บนข้อมือสักพัก ให้ผู้เล่นรู้ว่าป้ายอยู่ตรงนี้
        forceShowUntil = Time.time + 6f;
        RefreshChecklist();

        lazyFollow = true;
        Transform head = Head();
        if (head != null)
        {
            panelTarget = TargetFor(head);
            transform.position = panelTarget;
            FaceCamera(head);
        }
    }

    private void OnDestroy()
    {
        if (onTopMaterial != null)
        {
            Destroy(onTopMaterial);
        }
    }

    private void LateUpdate()
    {
        if (lazyFollow)
        {
            Transform wrist = panelMode == PanelMode.Wrist ? LeftController() : null;
            if (wrist != null)
            {
                WristUpdate(wrist);
                return;
            }

            transform.localScale = Vector3.one * panelScale;
            SetPanelAlpha(1f, true);
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
        Transform head = Head();
        if (head == null)
        {
            return;
        }

        Vector3 toPanel = transform.position - head.position;
        Vector3 flatForward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        Vector3 flatToPanel = Vector3.ProjectOnPlane(toPanel, Vector3.up);
        float angle = Vector3.Angle(flatForward, flatToPanel);
        float distanceError = Mathf.Abs(flatToPanel.magnitude - panelDistance);

        // ป้ายอยู่นิ่ง ๆ จนกว่าจะหันไปไกลหรือเดินห่างเกิน แล้วค่อยลอยตามมาเบา ๆ
        if (!following && (angle > recenterAngle || distanceError > 0.6f || IsBlocked(head.position, transform.position)))
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
            p.y = Mathf.Lerp(p.y, ClampToFloor(head.position.y + panelHeight), Time.deltaTime * followSpeed);
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
        Vector3 target = head.position + forward * panelDistance + right * panelSide + Vector3.up * panelHeight;
        target.y = ClampToFloor(target.y);
        return PullInFrontOfWalls(head.position, target);
    }

    // ถ้ามีผนัง/เคาน์เตอร์/ตู้ (วัตถุที่ไม่มี Rigidbody) คั่นอยู่ ให้ป้ายมาอยู่หน้าวัตถุนั้นแทนการจมเข้าไป
    private Vector3 PullInFrontOfWalls(Vector3 headPos, Vector3 target)
    {
        if (!avoidWalls)
        {
            return target;
        }

        Vector3 toTarget = target - headPos;
        float distance = toTarget.magnitude;
        if (distance < 0.001f || !TryGetBlockingHit(headPos, toTarget / distance, distance + 0.05f, out float hitDistance))
        {
            return target;
        }

        float pulled = Mathf.Max(hitDistance - 0.08f, minPanelDistance);
        Vector3 result = headPos + toTarget / distance * pulled;
        result.y = ClampToFloor(result.y);
        return result;
    }

    private bool IsBlocked(Vector3 headPos, Vector3 panelPos)
    {
        if (!avoidWalls)
        {
            return false;
        }

        Vector3 toPanel = panelPos - headPos;
        float distance = toPanel.magnitude;
        return distance > minPanelDistance + 0.1f &&
               TryGetBlockingHit(headPos, toPanel / distance, distance, out _);
    }

    private bool TryGetBlockingHit(Vector3 origin, Vector3 direction, float maxDistance, out float hitDistance)
    {
        hitDistance = float.MaxValue;
        int count = Physics.RaycastNonAlloc(origin, direction, rayHits, maxDistance, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = rayHits[i];
            // ไม่นับของที่หยิบได้/ขยับได้ (มี Rigidbody) และตัวผู้เล่นเอง
            if (hit.collider.attachedRigidbody != null)
            {
                continue;
            }

            if (xrOrigin != null && hit.collider.transform.IsChildOf(xrOrigin.transform))
            {
                continue;
            }

            hitDistance = Mathf.Min(hitDistance, hit.distance);
        }

        return hitDistance < float.MaxValue;
    }

    // Controller ซ้ายที่กำลังใช้งาน (จาก XRInputModalityManager ของ XR Origin)
    // คืน null ถ้าไม่ได้ถือ Controller (เช่น Hand Tracking) → ใช้แบบ Follow แทน
    private Transform LeftController()
    {
        if (!searchedModality)
        {
            searchedModality = true;
            modality = FindAnyObjectByType<XRInputModalityManager>();

            if (modality == null)
            {
                Head();
                if (xrOrigin != null)
                {
                    foreach (Transform t in xrOrigin.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Left Controller")
                        {
                            fallbackLeftController = t;
                            break;
                        }
                    }
                }
            }
        }

        GameObject left = modality != null ? modality.leftController : null;
        if (left != null)
        {
            return left.activeInHierarchy ? left.transform : null;
        }

        return fallbackLeftController != null && fallbackLeftController.gameObject.activeInHierarchy
            ? fallbackLeftController
            : null;
    }

    private void WristUpdate(Transform wrist)
    {
        transform.localScale = Vector3.one * wristScale;

        Transform head = Head();
        Vector3 position = wrist.TransformPoint(wristLocalOffset);
        transform.position = position;

        bool show = true;
        if (head != null)
        {
            Vector3 away = position - head.position;
            if (away.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(away, head.up);
            }

            if (Time.time >= forceShowUntil)
            {
                Vector3 face = wrist.TransformDirection(wristFaceLocal.normalized);
                float facing = Vector3.Dot(face, (head.position - position).normalized);
                show = wristVisible ? facing > hideDot : facing > showDot;
            }
        }

        wristVisible = show;
        SetPanelAlpha(show ? 1f : 0f, false);
    }

    private void SetPanelAlpha(float target, bool instant)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = instant ? target : Mathf.MoveTowards(canvasGroup.alpha, target, Time.deltaTime * 6f);

        if (panelCanvas != null)
        {
            panelCanvas.enabled = canvasGroup.alpha > 0.01f || target > 0f;
        }
    }

    // กล้องของผู้เล่นจาก XR Origin (กล้องที่ถูก Track จริง) ถ้าไม่มีค่อยใช้ Camera.main
    private Transform Head()
    {
        if (xrOrigin == null && !searchedOrigin)
        {
            searchedOrigin = true;
            xrOrigin = FindAnyObjectByType<XROrigin>();
        }

        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            return xrOrigin.Camera.transform;
        }

        Camera cam = Camera.main;
        return cam != null ? cam.transform : null;
    }

    // ไม่ให้ขอบล่างของป้ายต่ำกว่าพื้นที่ผู้เล่นยืน (ตำแหน่ง XR Origin = เท้า)
    private float ClampToFloor(float centerY)
    {
        if (xrOrigin == null)
        {
            return centerY;
        }

        var rect = (RectTransform)transform;
        float halfHeight = rect.rect.height * 0.5f * transform.lossyScale.y;
        float minY = xrOrigin.transform.position.y + minHeightAboveFloor + halfHeight;
        return Mathf.Max(centerY, minY);
    }

    // ใช้วัสดุ UI ที่ปิดการเทียบความลึก (ZTest Always) ป้ายจึงไม่จมหายเข้าไปในพื้นหรือผนัง
    private void ApplyOnTopMaterial()
    {
        onTopMaterial = new Material(Canvas.GetDefaultCanvasMaterial());
        onTopMaterial.name = "MissionChecklist_OnTop";
        onTopMaterial.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);

        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
        {
            graphic.material = onTopMaterial;
        }
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

        // ภารกิจเปลี่ยน → ป้ายบนข้อมือขึ้นเองชั่วคราว
        if (panelMode == PanelMode.Wrist && lazyFollow)
        {
            forceShowUntil = Mathf.Max(forceShowUntil, Time.time + showOnChangeSeconds);
        }

        if (objectiveManager == null || !objectiveManager.MissionAccepted)
        {
            checklistText.text = panelMode == PanelMode.Wrist
                ? "ภารกิจ: ผัดกะเพรา\n\nเดินไปหาป้าแสงเพื่อรับภารกิจ\n(หันหลังมือซ้ายมาดูภารกิจได้ทุกเมื่อ)"
                : "ภารกิจ: ผัดกะเพรา\n\n" +
                  "เดินไปหาป้าแสงเพื่อรับภารกิจ";
            return;
        }

        // ป้ายข้อมือเล็ก แสดงเฉพาะขั้นปัจจุบัน
        if (panelMode == PanelMode.Wrist && objectiveManager.CurrentStep != null)
        {
            MissionStepDefinition current = objectiveManager.CurrentStep;
            var compact = new System.Text.StringBuilder();
            compact.Append(current.titleThai).Append('\n');

            if (current.HasRequirements)
            {
                foreach (IngredientRequirement requirement in current.requirements)
                {
                    compact
                        .Append('\n')
                        .Append(requirement.IsComplete ? "■ " : "• ")
                        .Append(VerbFor(requirement.targetState))
                        .Append(requirement.displayNameThai).Append(' ')
                        .Append(requirement.currentCount).Append('/')
                        .Append(requirement.requiredCount);
                }
            }

            checklistText.text = compact.ToString().TrimEnd();
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
