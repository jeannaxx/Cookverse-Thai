using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ขวดเครื่องปรุง 1 ขวด = 1 ชนิด (น้ำมัน ซอสหอยนางรม น้ำปลา น้ำตาล)
// ซอส 3 อย่างเทลงถ้วยผสม (SauceBowl) แล้วเทถ้วยลงกระทะทีเดียว · น้ำมันเทลงกระทะตรง
// เทได้ 2 วิธี เมื่อถือปากขวดอยู่เหนือถ้วยหรือกระทะ:
//   (1) เอียงขวดเกิน pourTiltAngle (75°) ค้าง tiltHoldSeconds (0.25 วินาที)
//   (2) กดไก (Activate) ขณะถือขวด
// เท 1 ครั้ง = 1 Portion · ต้องตั้งขวดกลับก่อนจึงเทแบบเอียงได้อีก (กันนับรัวตอนเอียงค้าง)
[RequireComponent(typeof(XRGrabInteractable))]
public class SeasoningBottle : MonoBehaviour
{
    [Header("Seasoning")]
    [SerializeField] private string ingredientId = ObjectiveManager.FishSauceIngredientId;
    [SerializeField] private string displayNameThai = "น้ำปลา";

    [Header("References")]
    [SerializeField] private PanContents pan;
    [SerializeField] private Transform spout;
    [Tooltip("แท่งสายเครื่องปรุงที่โผล่ใต้ปากขวดตอนเท (ไม่บังคับ)")]
    [SerializeField] private Transform pourStream;
    [Tooltip("ป้ายที่ขึ้นชื่อขวดตอนหยิบ ว่างไว้ = ใช้ป้าย SeasoningShelfUI เหนือชั้นเครื่องปรุง")]
    [SerializeField] private CollectionFeedbackUI grabFeedback;
    [Tooltip("ถ้วยผสมซอส ว่างไว้ = หาในฉากเอง · น้ำมันไม่เทลงถ้วย")]
    [SerializeField] private SauceBowl bowl;

    [Header("Pouring")]
    // ผลทดสอบแว่น v9: เทได้แค่ขวดเดียว · คนเทจริงเอียงราว 70–90° จึงลดจาก 100°/0.4 วิ
    [SerializeField] private float pourTiltAngle = 75f;
    [SerializeField] private float uprightResetAngle = 40f;
    [SerializeField] private float tiltHoldSeconds = 0.25f;
    [SerializeField] private float streamSeconds = 0.6f;

    private XRGrabInteractable grab;
    private float tiltTimer;
    private bool pouredThisTilt;
    private float streamHideTime;

    public string IngredientId => ingredientId;
    public string DisplayNameThai => displayNameThai;
    public string InstanceId => $"{ingredientId}-bottle";
    public Vector3 SpoutPosition =>
        spout != null ? spout.position : transform.position + transform.up * 0.12f;

    // น้ำมันลงกระทะเท่านั้น · ซอสอื่นลงถ้วยถ้าปากขวดอยู่เหนือถ้วย
    // เมนูกำหนดว่าเครื่องปรุงไหนเทลงถ้วยผสม (กะเพรา: ซอส 3 อย่าง · ต้มยำ: ไม่มี เทลงหม้อตรง)
    private bool UsesBowl =>
        bowl != null && bowl.isActiveAndEnabled && pan != null && pan.Menu.UsesBowl(ingredientId);

    private bool IsAboveBowl => UsesBowl && bowl.IsAbove(SpoutPosition);

    public void Configure(
        string id,
        string nameThai,
        PanContents targetPan,
        Transform spoutPoint,
        Transform stream)
    {
        ingredientId = id;
        displayNameThai = nameThai;
        pan = targetPan;
        spout = spoutPoint;
        pourStream = stream;
    }

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (grabFeedback == null)
        {
            GameObject shelfLabel = GameObject.Find("SeasoningShelfUI");
            grabFeedback = shelfLabel != null ? shelfLabel.GetComponent<CollectionFeedbackUI>() : null;
        }

        if (bowl == null)
        {
            bowl = FindAnyObjectByType<SauceBowl>();
        }

        if (pourStream != null)
        {
            pourStream.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (grab == null)
        {
            grab = GetComponent<XRGrabInteractable>();
        }
        grab.activated.AddListener(OnActivated);
        grab.selectEntered.AddListener(OnGrabbed);
        grab.selectExited.AddListener(OnReleased);
    }

    private void OnDisable()
    {
        grab.activated.RemoveListener(OnActivated);
        grab.selectEntered.RemoveListener(OnGrabbed);
        grab.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (grabFeedback != null)
        {
            string target = UsesBowl ? "ถ้วยผสมซอสบนโต๊ะเตรียม" : (pan != null ? pan.Menu.VesselThai : "กระทะ");
            grabFeedback.ShowStationMessage(
                $"ถือ{displayNameThai}แล้ว เอียงหรือกดไกเหนือ{target}เพื่อเท",
                true);
        }
    }

    private void OnActivated(ActivateEventArgs args)
    {
        TryPour(true);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        tiltTimer = 0f;
        pouredThisTilt = false;
    }

    private void Update()
    {
        UpdateStream();

        if (!grab.isSelected)
        {
            tiltTimer = 0f;
            pouredThisTilt = false;
            return;
        }

        float tilt = Vector3.Angle(transform.up, Vector3.up);
        bool abovePan = IsAboveBowl || (pan != null && pan.IsAbovePan(SpoutPosition));
        UpdateTilt(tilt, abovePan, Time.deltaTime);
    }

    // แยกจาก Update เพื่อให้ชุดทดสอบเรียกได้โดยไม่ต้องจับขวดจริง
    public bool UpdateTilt(float tiltDegrees, bool abovePan, float deltaTime)
    {
        if (tiltDegrees <= uprightResetAngle)
        {
            pouredThisTilt = false;
            tiltTimer = 0f;
            return false;
        }

        if (pouredThisTilt || tiltDegrees < pourTiltAngle || !abovePan)
        {
            tiltTimer = 0f;
            return false;
        }

        tiltTimer += deltaTime;

        if (tiltTimer < tiltHoldSeconds)
        {
            return false;
        }

        pouredThisTilt = true;
        tiltTimer = 0f;
        return TryPour(false);
    }

    public bool TryPour(bool fromButton)
    {
        if (pan == null)
        {
            Debug.LogWarning($"[เครื่องปรุง] ขวด{displayNameThai} ยังไม่ได้ผูกกับกระทะ", this);
            return false;
        }

        if (IsAboveBowl)
        {
            ShowStream();
            return bowl.AddSeasoning(ingredientId, displayNameThai);
        }

        if (!pan.IsAbovePan(SpoutPosition))
        {
            if (fromButton)
            {
                pan.ShowHint(UsesBowl
                    ? $"ถือขวด{displayNameThai}ให้อยู่เหนือถ้วยผสมซอสก่อนเท"
                    : $"ถือขวด{displayNameThai}ให้อยู่เหนือ{pan.Menu.VesselThai}ก่อนเท");
            }

            return false;
        }

        ShowStream();
        return pan.AddSeasoning(ingredientId, displayNameThai, InstanceId);
    }

    private void ShowStream()
    {
        if (pourStream == null)
        {
            return;
        }

        pourStream.gameObject.SetActive(true);
        streamHideTime = Time.time + streamSeconds;
        UpdateStream();
    }

    // สายเครื่องปรุงตั้งตรงลงพื้นเสมอ ไม่ว่าขวดเอียงแค่ไหน
    private void UpdateStream()
    {
        if (pourStream == null || !pourStream.gameObject.activeSelf)
        {
            return;
        }

        if (Time.time >= streamHideTime)
        {
            pourStream.gameObject.SetActive(false);
            return;
        }

        float halfLength = pourStream.lossyScale.y;
        pourStream.SetPositionAndRotation(
            SpoutPosition + Vector3.down * halfLength,
            Quaternion.identity);
    }
}
