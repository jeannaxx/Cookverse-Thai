using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CollectionFeedbackUI : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private Text feedbackText;

    [Header("Display Settings")]
    [SerializeField] private float visibleSeconds = 2.5f;
    [SerializeField] private bool faceMainCamera = true;
    [SerializeField] private string initialMessage =
        "นำใบกะเพราและพริกมาวางบนโต๊ะเตรียมวัตถุดิบ";

    [Header("Colors")]
    [SerializeField] private Color correctColor =
        new Color(0.35f, 1f, 0.45f, 1f);
    [SerializeField] private Color warningColor =
        new Color(1f, 0.82f, 0.2f, 1f);
    [SerializeField] private Color errorColor =
        new Color(1f, 0.35f, 0.35f, 1f);

    [Header("Background (อ่านง่ายบนผนังสีอ่อน)")]
    [SerializeField] private Color backgroundColor =
        new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Vector2 backgroundPadding = new Vector2(28f, 16f);

    private Coroutine hideRoutine;
    private RectTransform background;

    public void Configure(Text textComponent)
    {
        feedbackText = textComponent;
    }

    private void Awake()
    {
        EnsureThaiFont();
        EnsureBackground();
        ShowMessage(initialMessage, Color.white, false);
    }

    private void LateUpdate()
    {
        if (!faceMainCamera || Camera.main == null)
        {
            return;
        }

        Vector3 directionToCamera =
            Camera.main.transform.position - transform.position;

        // Canvas แบบ World Space อ่านถูกด้านเมื่อด้านหน้า (+Z) หันออกจากผู้มอง
        // จึงหันไปทางตรงข้ามกับกล้อง (ถ้าหันเข้าหากล้อง ตัวหนังสือจะกลับด้าน)
        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
        }
    }

    public void ShowResult(
        IngredientCollectionResult result,
        IngredientIdentity ingredient)
    {
        switch (result)
        {
            case IngredientCollectionResult.Correct:
                string ingredientName = ingredient != null
                    ? ingredient.DisplayNameThai
                    : "วัตถุดิบ";
                ShowMessage(
                    $"ถูกต้อง วาง{ingredientName}บนโต๊ะแล้ว",
                    correctColor);
                break;

            case IngredientCollectionResult.WrongIngredient:
                ShowMessage(
                    "วัตถุดิบไม่ถูกต้อง ลองตรวจภารกิจอีกครั้ง",
                    warningColor);
                break;

            case IngredientCollectionResult.Duplicate:
                ShowMessage(
                    "วัตถุดิบชิ้นนี้ถูกนับแล้ว",
                    warningColor);
                break;

            case IngredientCollectionResult.WrongObjective:
                ShowMessage(
                    "ยังไม่ถึงขั้นเก็บวัตถุดิบจากสวน",
                    warningColor);
                break;

            default:
                ShowMessage(
                    "ไม่พบข้อมูลของวัตถุดิบ",
                    errorColor);
                break;
        }
    }

    // ข้อความจากจุดทำงาน เช่น อ่างล้าง เขียง เตา กระทะ
    public void ShowStationMessage(string message, bool success)
    {
        ShowMessage(message, success ? correctColor : warningColor);
    }

    public void ShowSystemError()
    {
        ShowMessage("ระบบรับวัตถุดิบยังไม่พร้อม", errorColor);
    }

    public void ResetFeedback()
    {
        ShowMessage(initialMessage, Color.white, false);
    }

    private void ShowMessage(
        string message,
        Color color,
        bool hideAutomatically = true)
    {
        Debug.Log($"[ผลการเก็บ] {message}", this);

        if (feedbackText == null)
        {
            return;
        }

        feedbackText.text = message;
        feedbackText.color = color;
        feedbackText.gameObject.SetActive(true);
        FitBackground();

        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        if (hideAutomatically)
        {
            hideRoutine = StartCoroutine(HideAfterDelay());
        }
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(visibleSeconds);

        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }

        if (background != null)
        {
            background.gameObject.SetActive(false);
        }

        hideRoutine = null;
    }

    // พื้นหลังสร้างตอนเริ่มเกม ไม่ต้องแก้ Prefab/ฉาก · วางก่อนตัวหนังสือจึงอยู่ด้านหลัง
    private void EnsureBackground()
    {
        if (feedbackText == null || background != null)
        {
            return;
        }

        var backgroundObject = new GameObject("FeedbackBackground", typeof(RectTransform), typeof(Image));
        background = (RectTransform)backgroundObject.transform;
        background.SetParent(feedbackText.transform.parent, false);
        background.SetSiblingIndex(feedbackText.transform.GetSiblingIndex());

        var image = backgroundObject.GetComponent<Image>();
        image.color = backgroundColor;
        image.raycastTarget = false;
    }

    // ปรับขนาดพื้นหลังให้พอดีกับข้อความปัจจุบัน
    private void FitBackground()
    {
        if (background == null || feedbackText == null)
        {
            return;
        }

        RectTransform textRect = feedbackText.rectTransform;
        float width = textRect.rect.width;
        float height = Mathf.Max(feedbackText.preferredHeight, feedbackText.fontSize * 1.2f);
        float textWidth = Mathf.Min(feedbackText.preferredWidth, width > 1f ? width : feedbackText.preferredWidth);

        background.anchorMin = textRect.anchorMin;
        background.anchorMax = textRect.anchorMax;
        background.pivot = new Vector2(0.5f, 0.5f);
        background.anchoredPosition = textRect.anchoredPosition +
            new Vector2((0.5f - textRect.pivot.x) * width, (0.5f - textRect.pivot.y) * textRect.rect.height);
        background.sizeDelta = Vector2.zero;
        background.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth + backgroundPadding.x * 2f);
        background.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height + backgroundPadding.y * 2f);
        background.localRotation = Quaternion.identity;
        background.localScale = Vector3.one;
        background.gameObject.SetActive(true);
    }

    private void EnsureThaiFont()
    {
        if (feedbackText == null)
        {
            return;
        }

        // ใช้ Sarabun ที่ฝังในโปรเจกต์เสมอ ฟอนต์เดิม (LegacyRuntime)
        // แสดงไทยได้บน Mac เท่านั้น
        Font thaiFont = ThaiFont.Get();

        if (thaiFont != null)
        {
            feedbackText.font = thaiFont;
        }
    }
}
