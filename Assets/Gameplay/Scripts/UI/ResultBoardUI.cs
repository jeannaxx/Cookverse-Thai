using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// หน้าผลหลังเสิร์ฟ (G5): คะแนนรวม · ระดับผล · คะแนน 6 หมวดตาม Rubric v1 · ข้อแนะนำ · ปุ่ม "เล่นใหม่"
// - ฟัง ActionLog: เห็น DishServed → รอ showDelay วินาที (ให้ป้าแสงพูดก่อน) → คิดคะแนนด้วย RubricScorer แล้วแสดง
// - กระดานลอยตรงหน้าผู้เล่น (ห่าง distance ม.) หันหาผู้เล่น แล้วอยู่กับที่ (ไม่ตามหัว)
// - ปุ่มเล่นใหม่ = XRSimpleInteractable (ชี้เลเซอร์แล้วกดไก / เอามือแตะแล้วกำ) → SessionManager.StartNewSession()
//   ซึ่งรีเซ็ตทุกสถานี วัตถุดิบ ภารกิจ และเริ่ม ActionLog รอบใหม่
// - สร้าง Canvas เองตอนเริ่ม (ฟอนต์ Sarabun) · ไม่ต้องมี EventSystem/XR UI Input Module
public class ResultBoardUI : MonoBehaviour, IMissionResettable
{
    [SerializeField] private SessionManager sessionManager;
    [SerializeField, Min(0f)] private float showDelay = 2.5f;
    [SerializeField, Min(0.3f)] private float distance = 1.1f;
    [SerializeField] private float heightOffset = -0.15f;   // ต่ำกว่าระดับตาเล็กน้อย
    [SerializeField] private Color backgroundColor = new(0.08f, 0.07f, 0.06f, 0.88f);

    public static RubricResult LastResult { get; private set; }
    public bool IsShowing => board != null && board.activeSelf;
    public XRSimpleInteractable RestartButton { get; private set; }

    private const float PixelScale = 0.0012f;   // 1 หน่วย Canvas = 1.2 มม. → กระดาน 0.72 × 0.9 ม.
    private const float Width = 600f, Height = 750f;

    private GameObject board;
    private Text title, score, grade, categories, scores, tips;
    private Coroutine pending;

    private void OnEnable() => ActionLog.EntryAdded += OnEntry;
    private void OnDisable() => ActionLog.EntryAdded -= OnEntry;

    private void Awake()
    {
        if (sessionManager == null) sessionManager = FindAnyObjectByType<SessionManager>();
        Build();
        board.SetActive(false);
    }

    private void OnEntry(ActionLogEntry e)
    {
        if (e.action == ActionLog.Actions.DishServed)
        {
            if (pending != null) StopCoroutine(pending);
            pending = StartCoroutine(ShowAfterDelay());
        }
        else if (e.action == ActionLog.Actions.MissionReset)
        {
            Hide();
        }
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(showDelay);
        pending = null;
        ShowResult(RubricScorer.Score(ActionLog.Entries, ActionLog.SessionId, ActionLog.Mode));
    }

    public void ShowResult(RubricResult result)
    {
        LastResult = result;
        Debug.Log($"[ผลการประเมิน] {result.totalScore}/{result.maxScore} {result.gradeThai}\n{result.ToJson()}", this);

        title.text = "ป้าแสงประเมินผัดกะเพราไก่";
        score.text = $"{result.totalScore}<size=40>/{result.maxScore}</size>";
        grade.text = result.gradeThai;
        grade.color = result.passed ? new Color(0.55f, 0.9f, 0.45f) : new Color(1f, 0.55f, 0.4f);
        categories.text = string.Join("\n", result.categories.Select(c => c.nameThai));
        scores.text = string.Join("\n", result.categories.Select(c => $"{c.score}/{c.maxScore}"));
        var tipList = result.TopTips(4);
        tips.text = tipList.Count == 0
            ? "ทำได้ครบทุกขั้น เก่งมากจ้ะ"
            : CollectionFeedbackUI.WrapThai("ข้อแนะนำ\n" + string.Join("\n", tipList.Select(t => "• " + t)), 44);

        PlaceInFrontOfPlayer();
        board.SetActive(true);
    }

    public void Hide()
    {
        if (pending != null) { StopCoroutine(pending); pending = null; }
        if (board != null) board.SetActive(false);
    }

    public void Restart()
    {
        Hide();
        if (sessionManager == null) sessionManager = FindAnyObjectByType<SessionManager>();
        if (sessionManager == null)
        {
            Debug.LogError("[ผลการประเมิน] ไม่พบ SessionManager จึงเริ่มใหม่ไม่ได้", this);
            return;
        }
        sessionManager.StartNewSession();
        CollectionFeedbackUI feedback = FindAnyObjectByType<CollectionFeedbackUI>();
        if (feedback != null) feedback.ShowStationMessage("เริ่มรอบใหม่แล้ว กลับไปรับภารกิจจากป้าแสงนะจ๊ะ", true);
    }

    public void ResetForMission() => Hide();

    private void PlaceInFrontOfPlayer()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 pos = cam.transform.position + forward * distance + Vector3.up * heightOffset;
        // Canvas World Space อ่านถูกด้านเมื่อ +Z หันออกจากผู้มอง
        board.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(forward, Vector3.up));
    }

    // ---------- สร้าง UI ----------
    private void Build()
    {
        board = new GameObject("ResultBoard", typeof(RectTransform), typeof(Canvas));
        board.transform.SetParent(transform, false);
        var canvas = board.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        var rt = (RectTransform)board.transform;
        rt.sizeDelta = new Vector2(Width, Height);
        rt.localScale = Vector3.one * PixelScale;

        Panel(rt, "Background", Vector2.zero, new Vector2(Width, Height), backgroundColor);
        title = Label(rt, "Title", new Vector2(0, 320), new Vector2(560, 60), 34, TextAnchor.MiddleCenter, Color.white);
        score = Label(rt, "Score", new Vector2(0, 225), new Vector2(560, 120), 96, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));
        grade = Label(rt, "Grade", new Vector2(0, 135), new Vector2(560, 60), 40, TextAnchor.MiddleCenter, Color.white);
        categories = Label(rt, "Categories", new Vector2(-70, -5), new Vector2(380, 230), 28, TextAnchor.UpperLeft, Color.white);
        scores = Label(rt, "Scores", new Vector2(200, -5), new Vector2(140, 230), 28, TextAnchor.UpperRight, new Color(1f, 0.85f, 0.45f));
        categories.lineSpacing = scores.lineSpacing = 1.15f;
        tips = Label(rt, "Tips", new Vector2(0, -205), new Vector2(560, 170), 22, TextAnchor.UpperLeft, new Color(0.9f, 0.9f, 0.85f));

        // ปุ่มเล่นใหม่
        RectTransform button = Panel(rt, "RestartButton", new Vector2(0, -330), new Vector2(300, 70), new Color(0.85f, 0.45f, 0.15f, 1f));
        Label(button, "Text", Vector2.zero, new Vector2(300, 70), 34, TextAnchor.MiddleCenter, Color.white).text = "เล่นใหม่";
        var box = button.gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(300f, 70f, 40f);   // หน่วย Canvas (สเกลแม่ 0.0012 → 36 × 8 ซม.)
        RestartButton = button.gameObject.AddComponent<XRSimpleInteractable>();
        RestartButton.selectEntered.AddListener(_ => Restart());
    }

    private static RectTransform Panel(RectTransform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return rt;
    }

    private static Text Label(RectTransform parent, string name, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var t = go.GetComponent<Text>();
        t.font = ThaiFont.Get();
        t.fontSize = fontSize;
        t.alignment = align;
        t.color = color;
        t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;   // ตัดบรรทัดเองด้วย WrapThai
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
}
