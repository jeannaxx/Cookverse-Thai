using System;
using System.Collections;
using System.Collections.Generic;
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

    // ภาพตามสถานะ: ล้างแล้วสีสดขึ้น หั่นแล้วแยกเป็น 3 ชิ้น
    // กำลังสุก/สุก/ไหม้ เปลี่ยนสี (ใช้ MaterialPropertyBlock ไม่แก้วัสดุจริง)
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly Color CookedColor = new(0.62f, 0.40f, 0.22f);
    private static readonly Color BurnedColor = new(0.12f, 0.09f, 0.07f);
    private const int CutPieceCount = 3;
    private MaterialPropertyBlock propertyBlock;
    private readonly List<GameObject> cutPieces = new();
    private MeshRenderer hiddenWholeRenderer;
    private bool wasWashed;

    public event Action<IngredientIdentity, IngredientState> StateChanged;

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

        if (newState == IngredientState.Washed)
        {
            wasWashed = true;
        }

        ApplyStateVisual();
        StateChanged?.Invoke(this, newState);

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
        ClearStateVisual();

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

    private void ApplyStateVisual()
    {
        if (currentState >= IngredientState.Cut && cutPieces.Count == 0)
        {
            CreateCutPieces();
        }

        foreach (Renderer target in GetComponentsInChildren<Renderer>(true))
        {
            if (!TryGetStateColor(target, out Color color))
            {
                target.SetPropertyBlock(null);
                continue;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            target.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(BaseColorId, color);
            propertyBlock.SetColor(ColorId, color);
            target.SetPropertyBlock(propertyBlock);
        }
    }

    private bool TryGetStateColor(Renderer target, out Color color)
    {
        Material material = target.sharedMaterial;
        Color baseColor = Color.white;

        if (material != null && material.HasProperty(BaseColorId))
        {
            baseColor = material.GetColor(BaseColorId);
        }
        else if (material != null && material.HasProperty(ColorId))
        {
            baseColor = material.GetColor(ColorId);
        }

        Color fresh = wasWashed ? Color.Lerp(baseColor, Color.white, 0.3f) : baseColor;

        switch (currentState)
        {
            case IngredientState.Washed:
            case IngredientState.Cut:
            case IngredientState.AddedToPan:
                color = fresh;
                return wasWashed;
            case IngredientState.Cooking:
                color = Color.Lerp(fresh, CookedColor, 0.45f);
                return true;
            case IngredientState.Cooked:
            case IngredientState.Served:
                color = CookedColor;
                return true;
            case IngredientState.Burned:
                color = BurnedColor;
                return true;
            default:
                color = baseColor;
                return false;
        }
    }

    // แยกบล็อกเป็น 3 ชิ้นตามแกนที่ยาวที่สุด ใช้กับวัตถุดิบที่เป็นบล็อกเดียว
    // (พริก ไก่ กระเทียม) Collider เดิมยังอยู่ จึงหยิบทั้งกองได้เหมือนเดิม
    private void CreateCutPieces()
    {
        MeshRenderer wholeRenderer = GetComponent<MeshRenderer>();
        MeshFilter wholeFilter = GetComponent<MeshFilter>();

        if (wholeRenderer == null || wholeFilter == null)
        {
            return;
        }

        Vector3 size = transform.lossyScale;
        int axis = size.x >= size.y && size.x >= size.z ? 0 : size.y >= size.z ? 1 : 2;

        for (int i = 0; i < CutPieceCount; i++)
        {
            var piece = new GameObject($"CutPiece_{i + 1}");
            piece.transform.SetParent(transform, false);
            piece.AddComponent<MeshFilter>().sharedMesh = wholeFilter.sharedMesh;
            piece.AddComponent<MeshRenderer>().sharedMaterials = wholeRenderer.sharedMaterials;

            Vector3 position = Vector3.zero;
            Vector3 scale = Vector3.one * 0.95f;
            position[axis] = (i - 1) * 0.36f;
            scale[axis] = 0.28f;
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            cutPieces.Add(piece);
        }

        wholeRenderer.enabled = false;
        hiddenWholeRenderer = wholeRenderer;
    }

    private void ClearStateVisual()
    {
        foreach (GameObject piece in cutPieces)
        {
            if (piece == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(piece);
            }
            else
            {
                DestroyImmediate(piece);
            }
        }

        cutPieces.Clear();

        if (hiddenWholeRenderer != null)
        {
            hiddenWholeRenderer.enabled = true;
            hiddenWholeRenderer = null;
        }

        wasWashed = false;

        foreach (Renderer target in GetComponentsInChildren<Renderer>(true))
        {
            target.SetPropertyBlock(null);
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
