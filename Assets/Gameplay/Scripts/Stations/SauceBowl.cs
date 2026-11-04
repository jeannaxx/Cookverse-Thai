using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ถ้วยผสมซอส (วางบนโต๊ะเตรียม SauceMixPoint): เทซอสหอยนางรม น้ำปลา น้ำตาล ลงถ้วยก่อน แล้วถือถ้วยไปเทลงกระทะทีเดียว
// - เทจากขวดลงถ้วยได้ทุกเมื่อ (เช่นระหว่างรอไก่สุก) · ชนิดเดิมซ้ำไม่นับ บันทึก SauceMixed isCorrect=false
// - เอียงถ้วยเหนือกระทะ ≥ pourTiltAngle ค้าง tiltHoldSeconds → ทุกอย่างในถ้วยลงกระทะพร้อมกัน
//   ผ่าน PanContents.AddSeasoning ทีละชนิด (ตรวจลำดับ/ซ้ำ/นับขั้น cook_season เหมือนเทจากขวด)
// - น้ำมันไม่ผ่านถ้วย (SeasoningBottle เทลงกระทะตรง)
[RequireComponent(typeof(XRGrabInteractable))]
public class SauceBowl : MonoBehaviour, IMissionResettable
{
    public const string InstanceId = "sauce-bowl";

    [SerializeField] private PanContents pan;
    [Tooltip("ทรงกระบอกแบนสีซอสในถ้วย สูงขึ้นตามจำนวนที่เท")]
    [SerializeField] private Transform sauceFill;
    [Tooltip("ว่างไว้ = ใช้ป้าย SauceBowlUI เหนือถ้วยบนโต๊ะเตรียม")]
    [SerializeField] private CollectionFeedbackUI feedback;
    [SerializeField] private int expectedPortions = 3;

    [Header("รับเครื่องปรุงจากขวด")]
    [SerializeField] private float catchRadius = 0.12f;
    [SerializeField] private float catchHeight = 0.4f;

    [Header("เทลงกระทะ")]
    [SerializeField] private float pourTiltAngle = 75f;
    [SerializeField] private float uprightResetAngle = 40f;
    [SerializeField] private float tiltHoldSeconds = 0.25f;

    [Header("ระดับซอส")]
    [SerializeField] private float fillHeightPerPortion = 0.012f;

    private readonly List<(string id, string nameThai)> contents = new();
    private XRGrabInteractable grab;
    private float tiltTimer;
    private bool pouredThisTilt;
    private Vector3 fillBaseLocalPosition;
    private Vector3 fillBaseLocalScale;

    public int Count => contents.Count;
    public bool Contains(string id) => contents.Exists(c => c.id == id);

    public void Configure(PanContents targetPan, Transform fill, CollectionFeedbackUI label)
    {
        pan = targetPan;
        sauceFill = fill;
        feedback = label;
    }

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (feedback == null)
        {
            GameObject label = GameObject.Find("SauceBowlUI") ?? GameObject.Find("SeasoningShelfUI");
            feedback = label != null ? label.GetComponent<CollectionFeedbackUI>() : null;
        }

        if (sauceFill != null)
        {
            fillBaseLocalPosition = sauceFill.localPosition;
            fillBaseLocalScale = sauceFill.localScale;
        }

        UpdateFill();
    }

    // ปากขวดอยู่เหนือถ้วย
    public bool IsAbove(Vector3 point)
    {
        Vector3 c = transform.position;
        float horizontal = new Vector2(point.x - c.x, point.z - c.z).magnitude;
        return horizontal <= catchRadius && point.y >= c.y - 0.02f && point.y <= c.y + catchHeight;
    }

    // เรียกจากขวดเครื่องปรุง
    public bool AddSeasoning(string id, string nameThai)
    {
        string stepId = pan != null ? pan.CurrentStepId : string.Empty;

        if (Contains(id))
        {
            ActionLog.Record(ActionLog.Actions.SauceMixed, stepId, id, InstanceId,
                isCorrect: false, value: CookingOrder.Duplicate);
            Show($"ใส่{nameThai}ในถ้วยแล้ว ไม่ต้องใส่ซ้ำ", false);
            return false;
        }

        contents.Add((id, nameThai));
        ActionLog.Record(ActionLog.Actions.SauceMixed, stepId, id, InstanceId,
            isCorrect: true, value: $"count={Count}");
        UpdateFill();
        Show(Count >= expectedPortions
            ? $"ใส่{nameThai}แล้ว ซอสครบ {Count}/{expectedPortions} รอไก่สุกแล้วเทถ้วยลงกระทะ"
            : $"ใส่{nameThai}ลงถ้วยแล้ว ซอสในถ้วย {Count}/{expectedPortions}", true);
        return true;
    }

    private void Update()
    {
        if (!grab.isSelected || Count == 0 || pan == null)
        {
            tiltTimer = 0f;
            pouredThisTilt = false;
            return;
        }

        UpdateTilt(Vector3.Angle(transform.up, Vector3.up), pan.IsAbovePan(transform.position), Time.deltaTime);
    }

    // แยกจาก Update เพื่อให้ชุดทดสอบเรียกได้โดยไม่ต้องจับถ้วยจริง
    public bool UpdateTilt(float tiltDegrees, bool abovePan, float deltaTime)
    {
        if (tiltDegrees <= uprightResetAngle)
        {
            pouredThisTilt = false;
            tiltTimer = 0f;
            return false;
        }

        if (Count == 0 || pouredThisTilt || tiltDegrees < pourTiltAngle || !abovePan)
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
        return PourIntoPan();
    }

    // เททุกอย่างในถ้วยลงกระทะทีเดียว · คืน true ถ้ามีของในถ้วย
    public bool PourIntoPan()
    {
        if (pan == null || Count == 0)
        {
            return false;
        }

        foreach (var item in contents)
        {
            pan.AddSeasoning(item.id, item.nameThai, InstanceId);
        }

        contents.Clear();
        UpdateFill();
        return true;
    }

    private void UpdateFill()
    {
        if (sauceFill == null)
        {
            return;
        }

        sauceFill.gameObject.SetActive(Count > 0);
        float height = fillHeightPerPortion * Count;
        sauceFill.localScale = new Vector3(fillBaseLocalScale.x, height / 2f, fillBaseLocalScale.z);
        sauceFill.localPosition = fillBaseLocalPosition + Vector3.up * (height / 2f);
    }

    private void Show(string message, bool success)
    {
        Debug.Log($"[ถ้วยซอส] {message}", this);

        if (feedback != null)
        {
            feedback.ShowStationMessage(message, success);
        }
    }

    public void ResetForMission()
    {
        contents.Clear();
        tiltTimer = 0f;
        pouredThisTilt = false;
        UpdateFill();
    }
}
