using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// คืนตำแหน่งและการหมุนเริ่มต้นเมื่อเริ่มภารกิจใหม่
// ใช้กับของที่จับได้แต่ไม่ใช่วัตถุดิบ เช่น ขวดเครื่องปรุง ตะหลิว
public class GrabbableResetPose : MonoBehaviour, IMissionResettable
{
    private Transform initialParent;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private bool captured;
    private Rigidbody body;
    private bool initialIsKinematic;
    private XRGrabInteractable grab;
    private bool originalThrowOnDetach;
    private Coroutine finishRoutine;

    private void Awake()
    {
        Capture();
    }

    private void Capture()
    {
        if (captured)
        {
            return;
        }

        initialParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        body = GetComponent<Rigidbody>();
        initialIsKinematic = body != null && body.isKinematic;
        grab = GetComponent<XRGrabInteractable>();
        captured = true;
    }

    public void ResetForMission()
    {
        Capture();

        if (grab != null)
        {
            if (finishRoutine == null)
            {
                originalThrowOnDetach = grab.throwOnDetach;
            }

            // กันแรงเหวี่ยงตอนปล่อยจากมือ (XRI ใส่ความเร็วใน LateUpdate)
            grab.throwOnDetach = false;

            if (grab.isSelected && grab.interactionManager != null)
            {
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            }
        }

        Restore();

        if (Application.isPlaying && isActiveAndEnabled)
        {
            if (finishRoutine != null)
            {
                StopCoroutine(finishRoutine);
            }

            finishRoutine = StartCoroutine(FinishAtEndOfFrame());
        }
        else if (grab != null)
        {
            grab.throwOnDetach = originalThrowOnDetach;
        }

        if (TryGetComponent(out SpatulaStirrer stirrer))
        {
            stirrer.ResetStroke();
        }
    }

    private IEnumerator FinishAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        Restore();

        if (grab != null)
        {
            grab.throwOnDetach = originalThrowOnDetach;
        }

        finishRoutine = null;
    }

    private void Restore()
    {
        transform.SetParent(initialParent, false);
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;

        if (body == null)
        {
            return;
        }

        body.isKinematic = initialIsKinematic;

        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        body.position = transform.position;
        body.rotation = transform.rotation;
        body.Sleep();
    }
}
