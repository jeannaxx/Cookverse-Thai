using Unity.VRTemplate;
using UnityEngine;

// เตาแก๊สแบบปุ่มบิด 4 ตำแหน่ง: ปิด / ไฟเบา / ไฟกลาง / ไฟแรง
// ปุ่มใช้ XRKnob จาก VR Template ตั้ง Angle Increment ให้ล็อกทุก 60 องศา (0-180)
// ค่าความร้อนต่อวินาทีอ่านโดย PanContents เพื่อคำนวณความสุก
public class GasStove : MonoBehaviour, IMissionResettable
{
    public static readonly string[] LevelNamesThai = { "ปิด", "ไฟเบา", "ไฟกลาง", "ไฟแรง" };

    [Header("Required References")]
    [SerializeField] private XRKnob knob;
    [SerializeField] private Transform flame;
    [SerializeField] private CollectionFeedbackUI feedbackUI;
    [SerializeField] private ObjectiveManager objectiveManager;

    [Header("Heat (หน่วยต่อวินาที) ตาม GAMEPLAY_CONTENT_V1")]
    [SerializeField] private float[] heatPerSecond = { 0f, 1f, 2.5f, 5f };

    private Vector3 flameFullScale = Vector3.one;
    private bool flameScaleCaptured;

    public int Level { get; private set; }
    public float HeatRate => heatPerSecond[Mathf.Clamp(Level, 0, heatPerSecond.Length - 1)];
    public string LevelNameThai => LevelNamesThai[Level];

    public void Configure(
        XRKnob stoveKnob,
        Transform stoveFlame,
        CollectionFeedbackUI feedback,
        ObjectiveManager objective)
    {
        knob = stoveKnob;
        flame = stoveFlame;
        feedbackUI = feedback;
        objectiveManager = objective;
    }

    private void Awake()
    {
        CaptureFlameScale();
        ApplyFlame();
    }

    private void OnEnable()
    {
        if (knob != null)
        {
            knob.onValueChange.AddListener(OnKnobChanged);
        }
    }

    private void OnDisable()
    {
        if (knob != null)
        {
            knob.onValueChange.RemoveListener(OnKnobChanged);
        }
    }

    private void OnKnobChanged(float value)
    {
        SetLevel(Mathf.Clamp(Mathf.RoundToInt(value * 3f), 0, 3));
    }

    public void SetLevel(int level)
    {
        level = Mathf.Clamp(level, 0, 3);

        if (level == Level)
        {
            return;
        }

        Level = level;
        ApplyFlame();

        ActionLog.Record(
            ActionLog.Actions.HeatChanged,
            objectiveManager != null ? objectiveManager.CurrentStepId : string.Empty,
            value: $"{Level}:{LevelNameThai}");

        string message = Level == 0 ? "ปิดเตาแล้ว" : $"เปิดเตา: {LevelNameThai}";
        Debug.Log($"[เตา] {message}", this);
        feedbackUI?.ShowStationMessage(message, true);
    }

    private void CaptureFlameScale()
    {
        if (flameScaleCaptured || flame == null)
        {
            return;
        }

        flameFullScale = flame.localScale;
        flameScaleCaptured = true;
    }

    private void ApplyFlame()
    {
        if (flame == null)
        {
            return;
        }

        CaptureFlameScale();
        flame.gameObject.SetActive(Level > 0);

        // ไฟเบา 40% · ไฟกลาง 70% · ไฟแรง 100% ของความสูงเปลวไฟ
        float height = Level switch { 1 => 0.4f, 2 => 0.7f, _ => 1f };
        flame.localScale = new Vector3(
            flameFullScale.x * (0.6f + 0.4f * height),
            flameFullScale.y * height,
            flameFullScale.z * (0.6f + 0.4f * height));
    }

    public void ResetForMission()
    {
        if (knob != null)
        {
            knob.value = 0f;
        }

        Level = 0;
        ApplyFlame();
    }

    [ContextMenu("Test/Heat Off")]
    private void TestOff() => SetFromMenu(0);

    [ContextMenu("Test/Heat Low")]
    private void TestLow() => SetFromMenu(1);

    [ContextMenu("Test/Heat Medium")]
    private void TestMedium() => SetFromMenu(2);

    [ContextMenu("Test/Heat High")]
    private void TestHigh() => SetFromMenu(3);

    // ทดสอบบนคอมโดยไม่ต้องบิดปุ่ม ปุ่มจะหมุนตามด้วย
    private void SetFromMenu(int level)
    {
        if (knob != null)
        {
            knob.value = level / 3f;
        }

        SetLevel(level);
    }
}
