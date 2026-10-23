using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ประตูตู้เย็นแบบต้นแบบ: จับหรือกดที่ประตูหนึ่งครั้งเพื่อเปิด/ปิด
// ติดไว้ที่จุดบานพับ (Hinge) ประตูต้องเป็นลูกของจุดนี้และมี XRSimpleInteractable
public class FridgeDoor : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private XRSimpleInteractable doorInteractable;

    [Header("Door Motion")]
    [Tooltip("มุมเปิดรอบแกน Y ของบานพับ ค่าลบเปิดออกทางด้านหน้า")]
    [SerializeField] private float openAngle = -100f;
    [SerializeField] private float swingSeconds = 0.4f;
    [SerializeField] private bool startOpen;

    [Header("Events")]
    [SerializeField] private UnityEvent onOpened;
    [SerializeField] private UnityEvent onClosed;

    private Quaternion closedRotation;
    private Coroutine swingRoutine;

    public bool IsOpen { get; private set; }

    public void Configure(XRSimpleInteractable interactable)
    {
        doorInteractable = interactable;
    }

    private void Awake()
    {
        closedRotation = transform.localRotation;
        IsOpen = startOpen;
        transform.localRotation = TargetRotation(IsOpen);
    }

    private void OnEnable()
    {
        if (doorInteractable != null)
        {
            doorInteractable.selectEntered.AddListener(OnDoorSelected);
        }
    }

    private void OnDisable()
    {
        if (doorInteractable != null)
        {
            doorInteractable.selectEntered.RemoveListener(OnDoorSelected);
        }
    }

    private void OnDoorSelected(SelectEnterEventArgs args)
    {
        Toggle();
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

    [ContextMenu("Test/Toggle Door")]
    private void TestToggle()
    {
        Toggle();
    }
}
