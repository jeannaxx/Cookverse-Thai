using UnityEngine;
using UnityEngine.UI;

// M5: เตือนเมื่อผู้เล่นเดินใกล้ขอบพื้นที่เล่น (แทนรั้ว) — ป้ายลอยหน้าผู้เล่น ค่อย ๆ จางเข้า/ออก
// ใช้คู่กับกำแพงล่องหนใน Countryside_M5/Boundary (สร้างโดย M5CountrysideEditor)
public sealed class BoundaryHint : MonoBehaviour
{
    [SerializeField] private Rect area;                 // ขอบเขตเดิน (x,z ในโลก)
    [SerializeField] private float warnDistance = 1.2f; // ใกล้ขอบน้อยกว่านี้ → เตือน (ม.)
    [SerializeField] private string message = "ห้ามออกไปไกลกว่านี้นะ\nกลับไปทำกับข้าวกับป้าแสงกันเถอะ";

    private Transform head;
    private Transform panel;
    private CanvasGroup group;
    private float nextFind;

    public void Configure(Rect walkArea, float warn)
    {
        area = walkArea;
        warnDistance = warn;
    }

    private void Start()
    {
        BuildPanel();
    }

    private void Update()
    {
        if (head == null)
        {
            if (Time.time < nextFind)
            {
                return;
            }

            nextFind = Time.time + 1f;
            Camera cam = Camera.main;
            head = cam != null ? cam.transform : null;
            if (head == null)
            {
                return;
            }
        }

        Vector3 p = head.position;
        float d = Mathf.Min(Mathf.Min(p.x - area.xMin, area.xMax - p.x), Mathf.Min(p.z - area.yMin, area.yMax - p.z));
        bool show = d < warnDistance;

        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, Time.deltaTime * 3f);
        bool visible = group.alpha > 0.01f;
        if (panel.gameObject.activeSelf != visible)
        {
            panel.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        // ลอยหน้าผู้เล่น 1.8 ม. สูงกว่าสายตาเล็กน้อย (พ้นมือ/ป้ายปุ่มคอนโทรลเลอร์) ตามหันแบบนุ่ม ๆ
        Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (fwd.sqrMagnitude < 0.001f)
        {
            fwd = Vector3.forward;
        }

        Vector3 target = p + fwd.normalized * 1.8f + Vector3.up * 0.25f;
        panel.position = Vector3.Lerp(panel.position, target, show ? Time.deltaTime * 6f : 0f);
        panel.rotation = Quaternion.LookRotation(panel.position - p, Vector3.up);
    }

    private void BuildPanel()
    {
        var go = new GameObject("BoundaryHintPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        panel = go.transform;
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(760f, 200f);
        rt.localScale = Vector3.one * 0.0011f;
        group = go.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        var bgRt = (RectTransform)bg.transform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.GetComponent<Image>().color = new Color(0.23f, 0.11f, 0.05f, 0.85f);

        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGo.transform.SetParent(go.transform, false);
        var tRt = (RectTransform)txtGo.transform;
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(28f, 18f);
        tRt.offsetMax = new Vector2(-28f, -18f);
        var txt = txtGo.GetComponent<Text>();
        txt.font = ThaiFont.Get();
        txt.fontSize = 40;
        txt.lineSpacing = 1.2f;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow; // กันสระ/วรรณยุกต์ไทยโดนตัด
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(1f, 0.95f, 0.88f);
        txt.text = message;

        go.SetActive(false);
    }
}
