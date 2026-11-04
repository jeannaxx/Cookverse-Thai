using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ตะหลิว: ถือแล้วขยับปลายตะหลิวในกระทะ ระยะแนวนอนสะสมครบ stirDistance = คน 1 ครั้ง
// ปลายตะหลิวออกนอกกระทะไม่นับระยะ แต่ไม่ล้างระยะที่สะสมไว้ (คนเป็นจังหวะได้)
// ตัก: จุ่มปลายตะหลิวในกระทะนิ่ง ๆ scoopSeconds (เมื่อ PanContents.CanScoop) → มีอาหารบนตะหลิว
//      แล้วแตะจาน (DishPlate) → อาหารลงจาน
[RequireComponent(typeof(XRGrabInteractable))]
public class SpatulaStirrer : MonoBehaviour
{
    [SerializeField] private PanContents pan;
    [SerializeField] private Transform tip;
    [Tooltip("ระยะแนวนอนรวมของปลายตะหลิวในกระทะต่อการคน 1 ครั้ง (เมตร)")]
    [SerializeField] private float stirDistance = 0.5f;
    [Tooltip("ระยะต่อเฟรมที่มากกว่านี้ไม่นับ (กันการวาร์ป/Teleport)")]
    [SerializeField] private float maxStepPerFrame = 0.2f;

    [Header("ตักใส่จาน")]
    [Tooltip("อาหารบนใบตะหลิว ซ่อนไว้จนกว่าจะตัก")]
    [SerializeField] private GameObject scoopVisual;
    [Tooltip("ว่างไว้ = หาจานในฉากเอง")]
    [SerializeField] private DishPlate plate;
    [SerializeField] private float scoopSeconds = 0.5f;
    [Tooltip("ปลายตะหลิวต้องเกือบนิ่ง (ม./วินาที) จึงนับเป็นการตัก ไม่ใช่การคน")]
    [SerializeField] private float scoopMaxSpeed = 0.15f;

    private XRGrabInteractable grab;
    private Vector3 lastTip;
    private bool hasLastTip;
    private float scoopTimer;

    public float Travelled { get; private set; }
    public float StirDistance => stirDistance;
    public bool Loaded { get; private set; }

    public void ConfigureScoop(GameObject food)
    {
        scoopVisual = food;
    }

    public void Configure(PanContents targetPan, Transform tipPoint)
    {
        pan = targetPan;
        tip = tipPoint;
    }

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (plate == null)
        {
            plate = FindAnyObjectByType<DishPlate>();
        }

        SetLoaded(false);
    }

    private void Update()
    {
        if (!grab.isSelected || pan == null || tip == null)
        {
            hasLastTip = false;
            Travelled = 0f;
            return;
        }

        Vector3 current = tip.position;
        float speed = 0f;

        if (hasLastTip)
        {
            ProcessTipMovement(lastTip, current);
            speed = Time.deltaTime > 0f ? Vector3.Distance(lastTip, current) / Time.deltaTime : 0f;
        }

        ProcessScoop(current, speed, Time.deltaTime);
        lastTip = current;
        hasLastTip = true;
    }

    // แยกจาก Update เพื่อให้ชุดทดสอบเรียกได้โดยไม่ต้องจับตะหลิวจริง
    public bool ProcessTipMovement(Vector3 from, Vector3 to)
    {
        if (pan == null || !pan.ContainsPoint(from) || !pan.ContainsPoint(to))
        {
            return false;
        }

        Vector3 delta = to - from;
        delta.y = 0f;
        float step = delta.magnitude;

        if (step > maxStepPerFrame)
        {
            return false;
        }

        Travelled += step;

        if (Travelled < stirDistance)
        {
            return false;
        }

        Travelled = 0f;
        pan.RegisterStir();
        return true;
    }

    // แยกจาก Update เพื่อให้ชุดทดสอบเรียกได้ · คืน true เมื่อตักสำเร็จหรือใส่จานสำเร็จ
    public bool ProcessScoop(Vector3 tipPosition, float tipSpeed, float deltaTime)
    {
        if (pan == null)
        {
            return false;
        }

        if (Loaded)
        {
            if (plate == null || !plate.ContainsPoint(tipPosition) || plate.Filled)
            {
                return false;
            }

            DishSummary summary = pan.TakeDishForPlating();

            if (summary == null || !plate.Fill(summary))
            {
                return false;
            }

            SetLoaded(false);
            return true;
        }

        if (!pan.ContainsPoint(tipPosition) || tipSpeed > scoopMaxSpeed)
        {
            scoopTimer = 0f;
            return false;
        }

        // ระหว่างปรุง (ไฟยังเปิด) จุ่มนิ่งไม่ใช่การตัก จึงไม่ขึ้นข้อความรบกวน
        if (!pan.CanScoop(out _))
        {
            scoopTimer = 0f;
            return false;
        }

        scoopTimer += deltaTime;

        if (scoopTimer < scoopSeconds)
        {
            return false;
        }

        scoopTimer = 0f;
        SetLoaded(true);
        pan.ShowHint("ตักแล้ว นำตะหลิวไปแตะจานบนโต๊ะจัดจาน");
        return true;
    }

    private void SetLoaded(bool loaded)
    {
        Loaded = loaded;

        if (scoopVisual != null)
        {
            scoopVisual.SetActive(loaded);
        }
    }

    public void ResetStroke()
    {
        Travelled = 0f;
        hasLastTip = false;
        scoopTimer = 0f;
        SetLoaded(false);
    }
}
