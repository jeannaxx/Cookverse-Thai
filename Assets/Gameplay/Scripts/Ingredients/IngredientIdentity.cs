using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class IngredientIdentity : MonoBehaviour
{
    public const string KrapowLeafId = "krapow_leaf";
    public const string ChiliId = "chili";

    [Header("Ingredient Information")]
    [SerializeField] private string ingredientId = KrapowLeafId;
    [SerializeField] private string legacyPrefabName = "Krapow";
    [SerializeField] private string displayNameThai = "ใบกะเพรา";

    [Header("Runtime State")]
    [SerializeField] private string instanceId;
    [SerializeField] private IngredientState currentState =
        IngredientState.Raw;

    private Transform initialParent;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;
    private Rigidbody cachedRigidbody;
    private bool initialUseGravity;
    private bool initialIsKinematic;
    private bool initialPoseCaptured;
    private Coroutine resetRoutine;
    private XRGrabInteractable resettingGrab;
    private bool originalThrowOnDetach;
    public bool IsResetting { get; private set; }

    public string IngredientId => ingredientId;
    public string InstanceId
    {
        get
        {
            CreateInstanceIdIfNeeded();
            return instanceId;
        }
    }
    public IngredientState CurrentState => currentState;
    public string DisplayNameThai => displayNameThai;

    private void Awake()
    {
        CaptureInitialPoseIfNeeded();
        CreateInstanceIdIfNeeded();
        ApplyLegacyCompatibility();
    }

    private void CaptureInitialPoseIfNeeded()
    {
        if (initialPoseCaptured)
        {
            return;
        }

        initialParent = transform.parent;
        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialLocalScale = transform.localScale;
        cachedRigidbody = GetComponent<Rigidbody>();

        if (cachedRigidbody != null)
        {
            initialUseGravity = cachedRigidbody.useGravity;
            initialIsKinematic = cachedRigidbody.isKinematic;
        }

        initialPoseCaptured = true;
    }

    private void CreateInstanceIdIfNeeded()
    {
        if (!string.IsNullOrEmpty(instanceId))
        {
            return;
        }

        instanceId = Guid.NewGuid().ToString("N");
    }

    private void ApplyLegacyCompatibility()
    {
        if (string.Equals(
                ingredientId,
                "Krapow",
                StringComparison.OrdinalIgnoreCase))
        {
            ingredientId = KrapowLeafId;
        }
    }

    public bool MatchesIngredientId(string requestedId)
    {
        if (string.IsNullOrWhiteSpace(requestedId))
        {
            return false;
        }

        if (string.Equals(
                requestedId,
                ingredientId,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return requestedId.StartsWith(
            legacyPrefabName,
            StringComparison.OrdinalIgnoreCase);
    }

    public bool TryCollect()
    {
        if (currentState != IngredientState.Raw)
        {
            Debug.LogWarning(
                $"[วัตถุดิบ] {displayNameThai} ชิ้นนี้ถูกเก็บแล้ว " +
                "จึงไม่นับซ้ำ",
                this);

            return false;
        }

        return TrySetState(IngredientState.Collected);
    }

    public bool TrySetState(IngredientState newState)
    {
        if (currentState == newState)
        {
            Debug.LogWarning(
                $"[วัตถุดิบ] {displayNameThai} อยู่ในสถานะ " +
                $"{GetStateNameThai(currentState)} แล้ว",
                this);

            return false;
        }

        currentState = newState;

        Debug.Log(
            $"[วัตถุดิบ] {displayNameThai} ({ingredientId}): " +
            $"{GetStateNameThai(currentState)}",
            this);

        return true;
    }

    public void ResetIngredient()
    {
        CaptureInitialPoseIfNeeded();
        if (resetRoutine != null) StopCoroutine(resetRoutine);
        if (!IsResetting)
        {
            resettingGrab = GetComponent<XRGrabInteractable>();
            if (resettingGrab != null) originalThrowOnDetach = resettingGrab.throwOnDetach;
        }
        IsResetting = true;
        if (resettingGrab != null)
        {
            // XRI applies throw velocity in LateUpdate. Suppress it through the
            // end of this frame so a reset cannot launch the returned ingredient.
            resettingGrab.throwOnDetach = false;
            if (resettingGrab.isSelected && resettingGrab.interactionManager != null)
                resettingGrab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)resettingGrab);
        }
        currentState = IngredientState.Raw;

        RestoreInitialPose();
        if (Application.isPlaying && isActiveAndEnabled)
            resetRoutine = StartCoroutine(FinishReset());
        else FinishResetImmediately();

        Debug.Log(
            $"[วัตถุดิบ] {displayNameThai}: ยังไม่เก็บ " +
            "และกลับตำแหน่งเริ่มต้นแล้ว",
            this);
    }

    private IEnumerator FinishReset()
    {
        yield return new WaitForEndOfFrame();
        RestoreInitialPose();
        FinishResetImmediately();
    }

    private void FinishResetImmediately()
    {
        if (resettingGrab != null) resettingGrab.throwOnDetach = originalThrowOnDetach;
        resetRoutine = null;
        IsResetting = false;
    }

    private void OnDisable()
    {
        if (resetRoutine != null) StopCoroutine(resetRoutine);
        if (IsResetting) FinishResetImmediately();
    }

    private void RestoreInitialPose()
    {

        transform.SetParent(initialParent, false);
        transform.localPosition = initialLocalPosition;
        transform.localRotation = initialLocalRotation;
        transform.localScale = initialLocalScale;

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = initialIsKinematic;
            cachedRigidbody.useGravity = initialUseGravity;
            if (!cachedRigidbody.isKinematic)
            {
                cachedRigidbody.linearVelocity = Vector3.zero;
                cachedRigidbody.angularVelocity = Vector3.zero;
            }
            cachedRigidbody.position = transform.position;
            cachedRigidbody.rotation = transform.rotation;
            cachedRigidbody.Sleep();
        }

    }

    private string GetStateNameThai(IngredientState state)
    {
        return state switch
        {
            IngredientState.Raw => "ยังไม่เก็บ",
            IngredientState.Collected => "เก็บแล้ว",
            IngredientState.Washed => "ล้างแล้ว",
            IngredientState.Cut => "หั่นแล้ว",
            IngredientState.AddedToPan => "ใส่กระทะแล้ว",
            IngredientState.Cooking => "กำลังปรุง",
            IngredientState.Cooked => "ปรุงสุกแล้ว",
            IngredientState.Burned => "ไหม้",
            IngredientState.Served => "เสิร์ฟแล้ว",
            _ => "ไม่ทราบสถานะ"
        };
    }

    [ContextMenu("Test/Collect Ingredient")]
    private void TestCollectIngredient()
    {
        TryCollect();
    }

    [ContextMenu("Test/Reset Ingredient")]
    private void TestResetIngredient()
    {
        ResetIngredient();
    }
}
