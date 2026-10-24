using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ระบุว่าวัตถุนี้คือมีด จุดหั่น (StationZone แบบ Cut) นับการสับเมื่อมีดที่ถืออยู่ลงไปในโซน
[RequireComponent(typeof(XRGrabInteractable))]
public class KitchenKnife : MonoBehaviour, IMissionResettable
{
    private XRGrabInteractable grab;
    private Vector3 startPosition;
    private Quaternion startRotation;

    public bool IsHeld => grab != null && grab.isSelected;

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    // รีเซ็ตภารกิจแล้วมีดกลับมาวางข้างเขียง
    public void ResetForMission()
    {
        if (IsHeld && grab.interactionManager != null)
        {
            grab.interactionManager.CancelInteractableSelection(
                (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
        }

        transform.SetPositionAndRotation(startPosition, startRotation);

        if (TryGetComponent(out Rigidbody body))
        {
            body.position = startPosition;
            body.rotation = startRotation;
        }
    }
}
