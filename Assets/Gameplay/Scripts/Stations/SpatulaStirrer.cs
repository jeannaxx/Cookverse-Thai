using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ตะหลิว: ถือแล้วขยับปลายตะหลิวในกระทะ ระยะแนวนอนสะสมครบ stirDistance = คน 1 ครั้ง
// ปลายตะหลิวออกนอกกระทะไม่นับระยะ แต่ไม่ล้างระยะที่สะสมไว้ (คนเป็นจังหวะได้)
[RequireComponent(typeof(XRGrabInteractable))]
public class SpatulaStirrer : MonoBehaviour
{
    [SerializeField] private PanContents pan;
    [SerializeField] private Transform tip;
    [Tooltip("ระยะแนวนอนรวมของปลายตะหลิวในกระทะต่อการคน 1 ครั้ง (เมตร)")]
    [SerializeField] private float stirDistance = 0.5f;
    [Tooltip("ระยะต่อเฟรมที่มากกว่านี้ไม่นับ (กันการวาร์ป/Teleport)")]
    [SerializeField] private float maxStepPerFrame = 0.2f;

    private XRGrabInteractable grab;
    private Vector3 lastTip;
    private bool hasLastTip;

    public float Travelled { get; private set; }
    public float StirDistance => stirDistance;

    public void Configure(PanContents targetPan, Transform tipPoint)
    {
        pan = targetPan;
        tip = tipPoint;
    }

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
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

        if (hasLastTip)
        {
            ProcessTipMovement(lastTip, current);
        }

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

    public void ResetStroke()
    {
        Travelled = 0f;
        hasLastTip = false;
    }
}
