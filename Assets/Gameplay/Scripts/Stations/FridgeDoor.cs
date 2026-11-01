using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ประตูตู้เย็นแบบต้นแบบ
// - ประตูปิด: จับหรือแตะตรงไหนของบานก็ได้ → เปิดค้าง
// - ประตูเปิด: ตัวบานปิด Collider ไว้ ไม่ขวาง Ray/มือตอนเอื้อมหยิบของในตู้
// - ปิดประตู: จับที่ด้ามจับ (สร้างอัตโนมัติถ้าไม่ได้กำหนด) · ด้ามจับเปิดได้ด้วย
// - กันกดซ้ำ: ไม่รับคำสั่งจากผู้เล่นภายใน toggleCooldown วินาทีหลังเปิด/ปิด
// ติดไว้ที่จุดบานพับ (Hinge) ประตูต้องเป็นลูกของจุดนี้และมี XRSimpleInteractable
public class FridgeDoor : MonoBehaviour, IMissionResettable
{
    [Header("Required References")]
    [SerializeField] private XRSimpleInteractable doorInteractable;

    [Header("Handle (ใช้ปิดประตู)")]
    [Tooltip("ว่างไว้ = สร้างด้ามจับอัตโนมัติที่ขอบนอกด้านหน้าของบานตอนเริ่มเกม")]
    [SerializeField] private XRSimpleInteractable handleInteractable;
    [SerializeField] private Color handleColor = new Color(0.95f, 0.55f, 0.1f);
    [Tooltip("ระยะด้ามจับจากขอบนอกของบาน (เมตร)")]
    [SerializeField] private float handleInset = 0.07f;

    [Header("Door Motion")]
    [Tooltip("มุมเปิดรอบแกน Y ของบานพับ ค่าลบเปิดออกทางด้านหน้า")]
    [SerializeField] private float openAngle = -100f;
    [SerializeField] private float swingSeconds = 0.4f;
    [SerializeField] private bool startOpen;
    [Tooltip("ไม่รับการจับซ้ำจากผู้เล่นภายในเวลานี้หลังเปิด/ปิด (วินาที)")]
    [SerializeField] private float toggleCooldown = 0.5f;

    [Header("Events")]
    [SerializeField] private UnityEvent onOpened;
    [SerializeField] private UnityEvent onClosed;

    private Quaternion closedRotation;
    private Coroutine swingRoutine;
    private Collider[] bodyColliders = new Collider[0];
    private float lastPlayerToggleTime = float.NegativeInfinity;

    public bool IsOpen { get; private set; }
    public XRSimpleInteractable DoorInteractable => doorInteractable;
    public XRSimpleInteractable HandleInteractable => handleInteractable;

    // true เมื่อตัวบานยังมี Collider ที่ขวาง Ray/มืออยู่
    public bool BodyBlocksRay
    {
        get
        {
            foreach (Collider body in bodyColliders)
            {
                if (body != null && body.enabled)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public void Configure(XRSimpleInteractable interactable)
    {
        doorInteractable = interactable;
    }

    private void Awake()
    {
        closedRotation = transform.localRotation;

        if (doorInteractable != null)
        {
            // ด้ามจับเป็นลูกของบานพับ ไม่ใช่ลูกของบาน จึงไม่ถูกนับเป็น Collider ของบาน
            bodyColliders = doorInteractable.GetComponentsInChildren<Collider>(true);

            if (handleInteractable == null)
            {
                handleInteractable = BuildHandle();
            }
        }
        else
        {
            Debug.LogWarning("[ตู้เย็น] ยังไม่ได้กำหนด doorInteractable", this);
        }

        IsOpen = startOpen;
        transform.localRotation = TargetRotation(IsOpen);
        SetBodyBlocking(!IsOpen);
    }

    private void OnEnable()
    {
        if (doorInteractable != null)
        {
            doorInteractable.selectEntered.AddListener(OnBodySelected);
        }

        if (handleInteractable != null)
        {
            handleInteractable.selectEntered.AddListener(OnHandleSelected);
        }
    }

    private void OnDisable()
    {
        if (doorInteractable != null)
        {
            doorInteractable.selectEntered.RemoveListener(OnBodySelected);
        }

        if (handleInteractable != null)
        {
            handleInteractable.selectEntered.RemoveListener(OnHandleSelected);
        }
    }

    // ตัวบานใช้เปิดอย่างเดียว ตอนเปิดอยู่จับโดนบานจะไม่ปิดประตู
    private void OnBodySelected(SelectEnterEventArgs args)
    {
        if (!IsOpen)
        {
            TrySetOpenFromPlayer(true);
        }
    }

    // ด้ามจับสลับเปิด/ปิด
    private void OnHandleSelected(SelectEnterEventArgs args)
    {
        TrySetOpenFromPlayer(!IsOpen);
    }

    private void TrySetOpenFromPlayer(bool open)
    {
        if (Time.time - lastPlayerToggleTime < toggleCooldown)
        {
            return;
        }

        lastPlayerToggleTime = Time.time;
        SetOpen(open);
    }

    public void Toggle()
    {
        SetOpen(!IsOpen);
    }

    public void Open()
    {
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        if (IsOpen == open)
        {
            return;
        }

        IsOpen = open;

        // ปิด/เปิด Collider ของบานทันที ไม่รอให้หมุนจบ เพื่อให้มือผ่านได้ตั้งแต่ประตูเริ่มเปิด
        SetBodyBlocking(!open);

        if (swingRoutine != null)
        {
            StopCoroutine(swingRoutine);
        }

        if (Application.isPlaying && isActiveAndEnabled)
        {
            swingRoutine = StartCoroutine(Swing(TargetRotation(open)));
        }
        else
        {
            transform.localRotation = TargetRotation(open);
        }

        Debug.Log(open ? "[ตู้เย็น] เปิดประตูแล้ว" : "[ตู้เย็น] ปิดประตูแล้ว", this);

        if (open)
        {
            onOpened?.Invoke();
        }
        else
        {
            onClosed?.Invoke();
        }
    }

    private void SetBodyBlocking(bool blocking)
    {
        foreach (Collider body in bodyColliders)
        {
            if (body != null)
            {
                body.enabled = blocking;
            }
        }
    }

    private XRSimpleInteractable BuildHandle()
    {
        Transform door = doorInteractable.transform;

        if (door.parent != transform)
        {
            Debug.LogWarning("[ตู้เย็น] บานไม่ได้เป็นลูกตรงของบานพับ จึงไม่สร้างด้ามจับอัตโนมัติ", this);
            return null;
        }

        // บานเป็น Cube ขนาดตาม localScale · ด้านหน้าอยู่ทาง +Z ของบาน
        Vector3 doorSize = door.localScale;
        float height = Mathf.Min(0.35f, doorSize.y * 0.35f);
        const float thickness = 0.035f;

        GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Fridge_Door_Handle";
        handle.transform.SetParent(transform, false);
        handle.transform.localRotation = door.localRotation;
        handle.transform.localPosition = door.localPosition + door.localRotation * new Vector3(
            doorSize.x / 2f - handleInset,
            0f,
            doorSize.z / 2f + thickness);
        handle.transform.localScale = new Vector3(thickness, height, thickness);

        // พื้นที่จับใหญ่กว่าตัวด้ามที่มองเห็น ให้เล็งง่ายในแว่น
        BoxCollider box = handle.GetComponent<BoxCollider>();
        box.size = new Vector3(2.5f, 1.2f, 3f);

        Renderer doorRenderer = door.GetComponent<Renderer>();
        if (doorRenderer != null && doorRenderer.sharedMaterial != null)
        {
            var material = new Material(doorRenderer.sharedMaterial) { name = "Fridge_Handle (runtime)" };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", handleColor);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", handleColor);
            }

            handle.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        // เพิ่มหลัง Collider มีแล้ว Interactable จะเก็บ Collider ของด้ามจับเอง
        return handle.AddComponent<XRSimpleInteractable>();
    }

    private Quaternion TargetRotation(bool open)
    {
        return open
            ? closedRotation * Quaternion.Euler(0f, openAngle, 0f)
            : closedRotation;
    }

    private IEnumerator Swing(Quaternion target)
    {
        Quaternion start = transform.localRotation;
        float time = 0f;

        while (time < swingSeconds)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / swingSeconds);
            transform.localRotation = Quaternion.Slerp(start, target, t);
            yield return null;
        }

        transform.localRotation = target;
        swingRoutine = null;
    }

    public void ResetForMission()
    {
        lastPlayerToggleTime = float.NegativeInfinity;

        if (startOpen)
        {
            Open();
        }
        else
        {
            Close();
        }
    }

    [ContextMenu("Test/Toggle Door")]
    private void TestToggle()
    {
        Toggle();
    }
}
