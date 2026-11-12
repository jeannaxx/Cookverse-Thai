using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// จานเสิร์ฟ: ตะหลิวที่ตักแล้วแตะจาน → อาหารจากกระทะลงจาน (Fill) · ถือไปวางที่จุดเสิร์ฟ (ServeZone)
// ใส่ได้ครั้งเดียว · เสิร์ฟแล้วล็อก (จับไม่ได้) · Reset คืนจานว่าง (ตำแหน่งคืนโดย GrabbableResetPose)
[RequireComponent(typeof(XRGrabInteractable))]
public class DishPlate : MonoBehaviour, IMissionResettable
{
    [Tooltip("กองผัดกะเพราบนจาน ซ่อนไว้จนกว่าจะตักใส่")]
    [SerializeField] private GameObject foodVisual;
    [Tooltip("ชิ้นที่เปลี่ยนสีตามความสุกของไก่ (ไม่บังคับ)")]
    [SerializeField] private Renderer chickenRenderer;
    [Tooltip("ว่างไว้ = ใช้ป้าย PlatingUI เหนือโต๊ะจัดจาน")]
    [SerializeField] private CollectionFeedbackUI feedback;
    [SerializeField] private float catchRadius = 0.14f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private XRGrabInteractable grab;
    private MaterialPropertyBlock block;

    public bool Filled { get; private set; }
    public bool Served { get; private set; }
    public DishSummary Summary { get; private set; }
    public bool IsHeld => grab != null && grab.isSelected;

    public void Configure(GameObject food, Renderer chicken)
    {
        foodVisual = food;
        chickenRenderer = chicken;
    }

    private void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        if (feedback == null)
        {
            GameObject label = GameObject.Find("PlatingUI");
            feedback = label != null ? label.GetComponent<CollectionFeedbackUI>() : null;
        }

        ShowFood(false);
    }

    // ปลายตะหลิวแตะจาน (แนวนอนในขอบจาน สูงไม่เกิน 15 ซม. เหนือจาน)
    public bool ContainsPoint(Vector3 point)
    {
        Vector3 c = transform.position;
        float horizontal = new Vector2(point.x - c.x, point.z - c.z).magnitude;
        return horizontal <= catchRadius && point.y >= c.y - 0.03f && point.y <= c.y + 0.15f;
    }

    public bool Fill(DishSummary summary)
    {
        if (Filled || Served || summary == null)
        {
            return false;
        }

        Filled = true;
        Summary = summary;
        ShowFood(true);
        TintChicken(summary);
        TintSoup(summary);

        if (feedback != null)
        {
            feedback.ShowStationMessage("ใส่จานแล้ว ถือจานไปเสิร์ฟป้าแสงที่โต๊ะทานข้าว", true);
        }

        return true;
    }

    public void MarkServed()
    {
        Served = true;

        if (grab != null)
        {
            if (grab.isSelected && grab.interactionManager != null)
            {
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
            }

            grab.enabled = false;
        }
    }

    private void ShowFood(bool visible)
    {
        if (foodVisual != null)
        {
            foodVisual.SetActive(visible);
        }
    }

    // ไก่สุก = น้ำตาล · ดิบ = ชมพู · ไหม้ = ดำ
    private void TintChicken(DishSummary summary)
    {
        if (chickenRenderer == null)
        {
            return;
        }

        Color color = summary.ChickenBurned ? new Color(0.12f, 0.09f, 0.07f)
            : summary.ChickenCooked ? new Color(0.62f, 0.40f, 0.22f)
            : new Color(0.95f, 0.70f, 0.68f);
        block ??= new MaterialPropertyBlock();
        chickenRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        chickenRenderer.SetPropertyBlock(block);
    }

    // เมนูหม้อ (ต้มยำ): ย้อมอาหารบนจานทั้งกองเป็นสีน้ำแกงส้ม · เมนูกระทะ: คืนสีเดิม
    private void TintSoup(DishSummary summary)
    {
        if (foodVisual == null)
        {
            return;
        }

        bool soup = summary != null && summary.Menu != null && summary.Menu.vessel == CookVessel.Pot;
        foreach (Renderer r in foodVisual.GetComponentsInChildren<Renderer>(true))
        {
            if (r == chickenRenderer && !soup)
            {
                continue;
            }

            if (!soup)
            {
                r.SetPropertyBlock(null);
                continue;
            }

            var soupBlock = new MaterialPropertyBlock();
            Color color = r == chickenRenderer ? TintFor(summary) : summary.Menu.soupColor;
            soupBlock.SetColor(BaseColorId, color);
            soupBlock.SetColor(ColorId, color);
            r.SetPropertyBlock(soupBlock);
        }
    }

    // กุ้งสุก = ส้มอ่อน · ดิบ = เทาใส · สุกเกิน = ส้มเข้ม
    private static Color TintFor(DishSummary summary) =>
        summary.ChickenBurned ? new Color(0.75f, 0.30f, 0.15f)
        : summary.ChickenCooked ? new Color(1f, 0.62f, 0.45f)
        : new Color(0.75f, 0.75f, 0.78f);

    public void ResetForMission()
    {
        Filled = false;
        Served = false;
        Summary = null;
        ShowFood(false);

        if (grab != null)
        {
            grab.enabled = true;
        }
    }
}
