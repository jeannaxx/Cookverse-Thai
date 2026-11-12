using UnityEngine;

// จุดเสิร์ฟบนโต๊ะทานข้าวข้างที่นั่งป้าแสง
// วางจานที่ตักอาหารแล้ว (ปล่อยจากมือ) ในพื้นที่นี้ → เสิร์ฟ: บันทึก DishServed · จบภารกิจ · ป้าแสงพูดตามผลจริง
// เสิร์ฟได้ครั้งเดียวต่อรอบ · คะแนนจริงคิดจาก ActionLog ตาม Rubric (G5)
[RequireComponent(typeof(Collider))]
public class ServeZone : MonoBehaviour, IMissionResettable
{
    [SerializeField] private ObjectiveManager objectiveManager;
    [Tooltip("ว่างไว้ = ใช้ป้าย ServeUI เหนือโต๊ะ")]
    [SerializeField] private CollectionFeedbackUI feedback;
    [SerializeField] private string npcName = "ป้าแสง";

    private DishPlate plateInside;
    private bool warnedEmpty;

    public bool HasServed { get; private set; }

    public void Configure(ObjectiveManager objective)
    {
        objectiveManager = objective;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        if (objectiveManager == null)
        {
            objectiveManager = FindAnyObjectByType<ObjectiveManager>();
        }

        if (feedback == null)
        {
            GameObject label = GameObject.Find("ServeUI");
            feedback = label != null ? label.GetComponent<CollectionFeedbackUI>() : null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DishPlate plate = other.GetComponentInParent<DishPlate>();

        if (plate != null)
        {
            plateInside = plate;
            warnedEmpty = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (plateInside != null && other.GetComponentInParent<DishPlate>() == plateInside)
        {
            plateInside = null;
        }
    }

    private void Update()
    {
        if (plateInside != null && !plateInside.IsHeld && !HasServed)
        {
            TryServe(plateInside);
        }
    }

    public bool TryServe(DishPlate plate)
    {
        if (HasServed || plate == null || plate.Served)
        {
            return false;
        }

        if (!plate.Filled)
        {
            if (!warnedEmpty)
            {
                warnedEmpty = true;
                Show(objectiveManager != null && objectiveManager.Menu.vessel == CookVessel.Pot
                    ? $"ชามยังว่าง ตัก{objectiveManager.Menu.nameThai}ใส่ชามก่อนนะ"
                    : "จานยังว่าง ตักผัดกะเพราใส่จานก่อนนะ", false);
            }

            return false;
        }

        if (objectiveManager == null || !objectiveManager.MissionAccepted)
        {
            Show("ยังไม่ได้รับภารกิจจากป้าแสง", false);
            return false;
        }

        HasServed = true;
        DishSummary summary = plate.Summary;
        plate.MarkServed();

        ActionLog.Record(ActionLog.Actions.DishServed, objectiveManager.CurrentStepId,
            isCorrect: summary.IsCorrect, value: summary.ToLogValue());
        objectiveManager.RecordServeProgress(ObjectiveManager.DishServedId);

        string issues = summary.IssuesThai();
        Show(string.IsNullOrEmpty(issues)
            ? $"{npcName}: หอมจังเลย ขอบใจมากนะจ๊ะ"
            : $"{npcName}: ขอบใจจ้ะ แต่{issues} ไว้ลองใหม่นะ", string.IsNullOrEmpty(issues));
        return true;
    }

    private void Show(string message, bool success)
    {
        Debug.Log($"[เสิร์ฟ] {message}", this);

        if (feedback != null)
        {
            feedback.ShowStationMessage(message, success);
        }
    }

    public void ResetForMission()
    {
        HasServed = false;
        plateInside = null;
        warnedEmpty = false;
    }
}
