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

    private Coroutine hideRoutine;

    public void Configure(Text textComponent)
    {
        feedbackText = textComponent;
    }

    private void Awake()
    {
        EnsureThaiFont();
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

        if (directionToCamera.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(directionToCamera);
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

        hideRoutine = null;
    }

    private void EnsureThaiFont()
    {
        if (feedbackText == null || feedbackText.font != null)
        {
            return;
        }

        feedbackText.font = Font.CreateDynamicFontFromOSFont(
            new[] { "Thonburi", "Tahoma", "Arial Unicode MS", "Arial" },
            32);
    }
}
