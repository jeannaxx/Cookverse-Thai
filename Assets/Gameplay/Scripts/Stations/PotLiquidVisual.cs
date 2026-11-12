using UnityEngine;

// น้ำในหม้อ: โผล่เมื่อเติมน้ำ · เดือดแล้วผิวน้ำกระเพื่อม · ใส่นมข้นจืด/น้ำพริกเผาแล้วเป็นสีส้มข้น
public class PotLiquidVisual : MonoBehaviour
{
    [SerializeField] private PanContents pan;
    [SerializeField] private Transform liquid;
    [SerializeField] private Color waterColor = new(0.55f, 0.75f, 0.85f, 1f);
    [SerializeField] private Color soupColor = new(0.93f, 0.48f, 0.18f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private Renderer liquidRenderer;
    private MaterialPropertyBlock block;
    private Vector3 baseScale;
    private Vector3 basePosition;

    public void Configure(PanContents targetPan, Transform liquidTransform)
    {
        pan = targetPan;
        liquid = liquidTransform;
    }

    private void Awake()
    {
        if (liquid == null) return;
        liquidRenderer = liquid.GetComponent<Renderer>();
        baseScale = liquid.localScale;
        basePosition = liquid.localPosition;
        block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        if (pan == null || liquid == null) return;

        bool hasWater = pan.HasWater && !pan.Plated;
        if (liquid.gameObject.activeSelf != hasWater) liquid.gameObject.SetActive(hasWater);
        if (!hasWater) return;

        bool creamy = pan.Order.Has(MenuCatalog.EvaporatedMilkId) || pan.Order.Has(MenuCatalog.NamPrikPaoId);
        Color color = creamy ? soupColor : Color.Lerp(waterColor, waterColor * 1.1f, pan.BoilProgress);

        if (liquidRenderer != null)
        {
            liquidRenderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            liquidRenderer.SetPropertyBlock(block);
        }

        float wobble = pan.Boiling && pan.StoveLevel > 0 ? Mathf.Sin(Time.time * 18f) * 0.004f : 0f;
        liquid.localPosition = basePosition + Vector3.up * wobble / Mathf.Max(0.0001f, liquid.parent != null ? liquid.parent.lossyScale.y : 1f);
        liquid.localScale = baseScale;
    }
}
