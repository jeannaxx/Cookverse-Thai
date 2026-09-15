using TMPro;
using UnityEngine;

public class CollectionFeedbackUI : MonoBehaviour
{
    [SerializeField] private TMP_Text feedbackText;

    [Header("Thai feedback messages")]
    [SerializeField] private string promptMessage = "นำกะเพรามาวางที่นี่";
    [SerializeField] private string correctMessage = "ถูกต้อง! เก็บกะเพราสำเร็จ";
    [SerializeField] private string wrongMessage = "วัตถุดิบไม่ถูกต้อง ต้องใช้กะเพรา";

    [Header("Message colors")]
    [SerializeField] private Color promptColor = Color.white;
    [SerializeField] private Color correctColor = new Color(0.3f, 1f, 0.35f);
    [SerializeField] private Color wrongColor = new Color(1f, 0.35f, 0.25f);

    private void Awake()
    {
        ShowPrompt();
    }

    public void ShowPrompt()
    {
        SetFeedback(promptMessage, promptColor);
    }

    public void ShowCorrect()
    {
        SetFeedback(correctMessage, correctColor);
    }

    public void ShowWrong()
    {
        SetFeedback(wrongMessage, wrongColor);
    }

    private void SetFeedback(string message, Color color)
    {
        if (feedbackText == null)
        {
            Debug.LogWarning("[CollectionFeedbackUI] Feedback Text is not assigned.", this);
            return;
        }

        feedbackText.text = message;
        feedbackText.color = color;
    }
}
