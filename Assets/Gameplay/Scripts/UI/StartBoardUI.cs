using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// กระดานเริ่มเกม (ลอยตรงหน้าผู้เล่นตอนเริ่ม)
//   หน้า 1 ใส่ชื่อ: แป้นอังกฤษ + ตัวเลข (เช่น jim432 / รหัสนักศึกษา) · 2–12 ตัว · ไม่ต้องมีบัญชี
//   หน้า 2 เลือกเมนู: การ์ดเมนูจาก menus (ว่าง = MenuCatalog) → ObjectiveManager.SetMenu + เริ่มรอบใหม่ → ไปรับภารกิจป้าแสง
// ปุ่มทุกปุ่ม = XRSimpleInteractable (ชี้เลเซอร์แล้วกดไก หรือเอานิ้วจิ้ม) · สร้าง Canvas เองตอนเริ่ม (ฟอนต์ Sarabun)
// ใน Editor พิมพ์จากคีย์บอร์ดจริงได้ด้วย (Enter = ตกลง · Backspace = ลบ)
public class StartBoardUI : MonoBehaviour
{
    public const int MinNameLength = 2;
    public const int MaxNameLength = 12;

    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private SessionManager sessionManager;
    [SerializeField] private MenuDefinition[] menus;
    [SerializeField] private bool showOnStart = true;
    [SerializeField, Min(0.3f)] private float distance = 1.0f;
    [SerializeField] private float heightOffset = -0.1f;
    [SerializeField] private Color backgroundColor = new(0.08f, 0.07f, 0.06f, 0.9f);

    private const float PixelScale = 0.0012f;
    private const float Width = 820f, Height = 640f;
    private static readonly Color KeyColor = new(0.25f, 0.22f, 0.19f, 1f);
    private static readonly Color KeyHover = new(0.45f, 0.38f, 0.30f, 1f);
    private static readonly Color AccentColor = new(0.85f, 0.45f, 0.15f, 1f);
    private static readonly Color AccentHover = new(1f, 0.6f, 0.25f, 1f);
    private static readonly string[] KeyRows = { "1234567890", "QWERTYUIOP", "ASDFGHJKL", "ZXCVBNM" };

    public static bool IsOpen { get; private set; }
    public string PlayerName => playerName;
    public bool OnMenuPage => menuPage != null && menuPage.activeSelf;

    private GameObject board, namePage, menuPage;
    private Text nameField, nameHint, menuTitle;
    private string playerName = string.Empty;
    private readonly List<(MenuDefinition menu, XRSimpleInteractable button)> menuButtons = new();

    public void Configure(ObjectiveManager objective, SessionManager session, MenuDefinition[] menuList)
    {
        objectiveManager = objective;
        sessionManager = session;
        menus = menuList;
    }

    private void Awake()
    {
        if (objectiveManager == null) objectiveManager = FindAnyObjectByType<ObjectiveManager>();
        if (sessionManager == null) sessionManager = FindAnyObjectByType<SessionManager>();
        Build();
        board.SetActive(false);
    }

    private IEnumerator Start()
    {
        if (!showOnStart) yield break;
        // รอกล้อง/ตำแหน่งหัวในแว่นพร้อมก่อนวางกระดาน
        yield return new WaitForSeconds(0.6f);
        Open(!string.IsNullOrEmpty(ActionLog.PlayerName));
    }

    private void OnDestroy() => IsOpen = false;

    // เปิดกระดาน · toMenuPage = ข้ามหน้าชื่อ (มีชื่อแล้ว เช่น กดเล่นใหม่)
    public void Open(bool toMenuPage)
    {
        if (!string.IsNullOrEmpty(ActionLog.PlayerName)) playerName = ActionLog.PlayerName;
        ShowPage(toMenuPage && IsValidName(playerName));
        PlaceInFrontOfPlayer();
        board.SetActive(true);
        IsOpen = true;
    }

    public void Close()
    {
        board.SetActive(false);
        IsOpen = false;
    }

    // ---------- หน้าใส่ชื่อ ----------

    public static bool IsValidName(string name) =>
        !string.IsNullOrEmpty(name) && name.Length >= MinNameLength && name.Length <= MaxNameLength &&
        name.All(c => (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'));

    public void TypeChar(char c)
    {
        c = char.ToLowerInvariant(c);
        if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))) return;
        if (playerName.Length >= MaxNameLength)
        {
            SetHint($"ชื่อยาวได้ไม่เกิน {MaxNameLength} ตัว", false);
            return;
        }
        playerName += c;
        RefreshName();
    }

    public void Backspace()
    {
        if (playerName.Length == 0) return;
        playerName = playerName.Substring(0, playerName.Length - 1);
        RefreshName();
    }

    public void ConfirmName()
    {
        if (!IsValidName(playerName))
        {
            SetHint($"ใส่ชื่ออย่างน้อย {MinNameLength} ตัว (อังกฤษหรือตัวเลข)", false);
            return;
        }

        ActionLog.PlayerName = playerName;
        ActionLog.Record(ActionLog.Actions.PlayerNamed, objectiveManager != null ? objectiveManager.CurrentStepId : "",
            value: playerName);
        Debug.Log($"[กระดานเริ่ม] ผู้เล่น: {playerName}", this);
        ShowPage(true);
    }

    // ---------- หน้าเลือกเมนู ----------

    public IReadOnlyList<MenuDefinition> Menus =>
        menus != null && menus.Length > 0 && menus.All(m => m != null) ? menus : MenuCatalog.All();

    public void SelectMenu(MenuDefinition menu)
    {
        if (menu == null || objectiveManager == null) return;

        ActionLog.MenuId = menu.menuId;
        objectiveManager.SetMenu(menu);
        if (sessionManager != null) sessionManager.StartNewSession();   // รีเซ็ตครัว + เริ่ม Log รอบใหม่ (ชื่อ/เมนูคงอยู่)
        ActionLog.Record(ActionLog.Actions.PlayerNamed, objectiveManager.CurrentStepId, value: playerName);
        ActionLog.Record(ActionLog.Actions.MenuSelected, objectiveManager.CurrentStepId, value: menu.menuId);
        Debug.Log($"[กระดานเริ่ม] {playerName} เลือก {menu.nameThai}", this);
        Close();

        CollectionFeedbackUI feedback = FindAnyObjectByType<CollectionFeedbackUI>();
        if (feedback != null)
            feedback.ShowStationMessage($"เมนู {menu.nameThai} · ไปรับภารกิจจากป้าแสงได้เลยจ้ะ", true);
    }

    public void SelectMenu(string menuId) => SelectMenu(Menus.FirstOrDefault(m => m.menuId == menuId));

    // ---------- แสดงผล ----------

    private void ShowPage(bool menu)
    {
        namePage.SetActive(!menu);
        menuPage.SetActive(menu);
        if (menu) menuTitle.text = $"สวัสดี {playerName}\nเลือกเมนูที่อยากทำ";
        else RefreshName();
    }

    private void RefreshName()
    {
        nameField.text = playerName.Length > 0 ? playerName + "<color=#f0a050>|</color>" : "<color=#777777>เช่น jim432</color>";
        SetHint($"อังกฤษหรือตัวเลข {MinNameLength}–{MaxNameLength} ตัว แล้วกด ตกลง", true);
    }

    private void SetHint(string text, bool ok)
    {
        nameHint.text = text;
        nameHint.color = ok ? new Color(0.85f, 0.85f, 0.8f) : new Color(1f, 0.55f, 0.4f);
    }

    private void PlaceInFrontOfPlayer()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 forward = cam.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();
        Vector3 pos = cam.transform.position + forward * distance + Vector3.up * heightOffset;
        board.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(forward, Vector3.up));
    }

#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
    // ทดสอบใน Editor: พิมพ์ชื่อจากคีย์บอร์ดจริง
    private void OnEnable()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput += OnTextInput;
    }

    private void OnDisable()
    {
        if (Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
    }

    private void OnTextInput(char c)
    {
        if (!IsOpen || namePage == null || !namePage.activeSelf) return;
        if (c == '\b') Backspace();
        else if (c == '\r' || c == '\n') ConfirmName();
        else TypeChar(c);
    }
#endif

    // ---------- สร้าง UI ----------

    private void Build()
    {
        board = new GameObject("StartBoard", typeof(RectTransform), typeof(Canvas));
        board.transform.SetParent(transform, false);
        var canvas = board.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        var rt = (RectTransform)board.transform;
        rt.sizeDelta = new Vector2(Width, Height);
        rt.localScale = Vector3.one * PixelScale;
        Panel(rt, "Background", Vector2.zero, new Vector2(Width, Height), backgroundColor);
        Label(rt, "Brand", new Vector2(0, 280), new Vector2(760, 50), 30, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f))
            .text = "Cookverse Thai · ครัวป้าแสง";

        BuildNamePage(rt);
        BuildMenuPage(rt);
    }

    private void BuildNamePage(RectTransform root)
    {
        namePage = Group(root, "NamePage");
        var page = (RectTransform)namePage.transform;
        Label(page, "Title", new Vector2(0, 215), new Vector2(760, 50), 34, TextAnchor.MiddleCenter, Color.white).text = "ใส่ชื่อผู้เล่น";
        Panel(page, "NameFieldBg", new Vector2(0, 145), new Vector2(520, 72), new Color(0.15f, 0.13f, 0.11f, 1f));
        nameField = Label(page, "NameField", new Vector2(0, 145), new Vector2(500, 72), 44, TextAnchor.MiddleCenter, Color.white);
        nameHint = Label(page, "Hint", new Vector2(0, 88), new Vector2(760, 36), 22, TextAnchor.MiddleCenter, Color.white);

        const float key = 62f, gap = 8f;
        float y = 25f;
        foreach (string row in KeyRows)
        {
            float x0 = -(row.Length - 1) * (key + gap) / 2f;
            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];
                Button(page, "Key_" + c, new Vector2(x0 + i * (key + gap), y), new Vector2(key, key), c.ToString(), 30,
                    KeyColor, KeyHover, () => TypeChar(c));
            }
            y -= key + gap;
        }

        Button(page, "Key_Back", new Vector2(-150, y - 10), new Vector2(220, 70), "ลบ", 30, KeyColor, KeyHover, Backspace);
        Button(page, "Key_OK", new Vector2(150, y - 10), new Vector2(260, 70), "ตกลง", 32, AccentColor, AccentHover, ConfirmName);
    }

    private void BuildMenuPage(RectTransform root)
    {
        menuPage = Group(root, "MenuPage");
        var page = (RectTransform)menuPage.transform;
        menuTitle = Label(page, "Title", new Vector2(0, 200), new Vector2(760, 90), 32, TextAnchor.MiddleCenter, Color.white);

        IReadOnlyList<MenuDefinition> list = Menus;
        int count = list.Count;
        // 1–2 เมนู = การ์ดใหญ่ 2 คอลัมน์ · 3 เมนูขึ้นไป = 3 คอลัมน์
        bool compact = count > 2;
        float cardW = compact ? 250f : 360f, cardH = compact ? 290f : 300f, gap = compact ? 18f : 24f;
        int wrap = compact ? 17 : 26;
        int cols = Mathf.Min(compact ? 3 : 2, Mathf.Max(1, count));
        for (int i = 0; i < count; i++)
        {
            MenuDefinition menu = list[i];
            int col = i % cols, row = i / cols;
            float x = (col - (cols - 1) / 2f) * (cardW + gap);
            float y = -15f - row * (cardH + gap);
            RectTransform card = Button(page, "Menu_" + menu.menuId, new Vector2(x, y), new Vector2(cardW, cardH),
                null, 0, KeyColor, KeyHover, () => SelectMenu(menu));
            Label(card, "Name", new Vector2(0, 95), new Vector2(cardW - 30, 60), compact ? 28 : 34, TextAnchor.MiddleCenter, Color.white).text = menu.nameThai;
            Label(card, "Level", new Vector2(0, 45), new Vector2(cardW - 30, 36), 26, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.3f))
                .text = "ความยาก " + new string('★', menu.difficulty) + new string('☆', 3 - menu.difficulty);
            Label(card, "Desc", new Vector2(0, -50), new Vector2(cardW - 40, 150), 22, TextAnchor.UpperCenter, new Color(0.88f, 0.88f, 0.82f))
                .text = CollectionFeedbackUI.WrapThai(menu.descriptionThai ?? "", wrap);
            menuButtons.Add((menu, card.GetComponent<XRSimpleInteractable>()));
        }

        Button(page, "ChangeName", new Vector2(0, -275), new Vector2(240, 56), "เปลี่ยนชื่อ", 24, KeyColor, KeyHover,
            () => ShowPage(false));
    }

    private RectTransform Button(RectTransform parent, string name, Vector2 pos, Vector2 size, string text, int fontSize,
                                 Color color, Color hover, Action onPress)
    {
        RectTransform button = Panel(parent, name, pos, size, color);
        if (!string.IsNullOrEmpty(text))
            Label(button, "Text", Vector2.zero, size, fontSize, TextAnchor.MiddleCenter, Color.white).text = text;
        var box = button.gameObject.AddComponent<BoxCollider>();
        box.size = new Vector3(size.x, size.y, 30f);   // หน่วย Canvas (สเกล 0.0012 → ลึก 3.6 ซม. ให้นิ้วจิ้มโดน)
        var image = button.GetComponent<Image>();
        var interactable = button.gameObject.AddComponent<XRSimpleInteractable>();
        interactable.hoverEntered.AddListener(_ => image.color = hover);
        interactable.hoverExited.AddListener(_ => image.color = color);
        interactable.selectEntered.AddListener(_ => onPress());
        return button;
    }

    private static GameObject Group(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = parent.sizeDelta;
        return go;
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
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
}
