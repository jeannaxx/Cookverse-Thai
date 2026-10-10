using UnityEngine;
using UnityEngine.UI;

public class MissionChecklistUI : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private Text checklistText;
    [SerializeField] private Transform followTarget;

    [Header("World Space Placement")]
    [SerializeField] private Vector3 localOffset =
        new Vector3(0.38f, 0.08f, 0.9f);

    public void Configure(
        ObjectiveManager objective,
        Text textComponent,
        Transform target)
    {
        Unsubscribe();
        objectiveManager = objective;
        checklistText = textComponent;
        followTarget = target;
        Subscribe();
        RefreshChecklist();
    }

    private void OnEnable()
    {
        EnsureThaiFont();
        Subscribe();
        RefreshChecklist();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void LateUpdate()
    {
        Canvas checklistCanvas = GetComponent<Canvas>();

        if (checklistCanvas != null &&
            checklistCanvas.renderMode != RenderMode.WorldSpace)
        {
            return;
        }

        if (followTarget == null)
        {
            return;
        }

        transform.position = followTarget.TransformPoint(localOffset);
        transform.rotation = followTarget.rotation;
    }

    public void RefreshChecklist()
    {
        if (checklistText == null)
        {
            return;
        }

        if (objectiveManager == null || !objectiveManager.MissionAccepted)
        {
            checklistText.text =
                "ภารกิจ: ผัดกะเพรา\n\n" +
                "เดินไปหาป้าแสงเพื่อรับภารกิจ";
            return;
        }

        string krapowMark = objectiveManager.CollectedKrapowCount >=
            objectiveManager.RequiredKrapowCount ? "✓" : "□";
        string chiliMark = objectiveManager.CollectedChiliCount >=
            objectiveManager.RequiredChiliCount ? "✓" : "□";

        string message =
            "ป้าแสง: ช่วยทำผัดกะเพราให้หน่อยจ้ะ\n" +
            "เก็บวัตถุดิบจากสวนแล้วนำไปวางบนโต๊ะเตรียม\n\n" +
            "รายการวัตถุดิบ\n" +
            $"{krapowMark} ใบกะเพรา " +
            $"{objectiveManager.CollectedKrapowCount}/" +
            $"{objectiveManager.RequiredKrapowCount}\n" +
            $"{chiliMark} พริก " +
            $"{objectiveManager.CollectedChiliCount}/" +
            $"{objectiveManager.RequiredChiliCount}";

        if (objectiveManager.IsGardenCollectionComplete)
        {
            message += "\n\nวัตถุดิบครบแล้ว ✓";
        }

        checklistText.text = message;
    }

    private void Subscribe()
    {
        if (objectiveManager != null)
        {
            objectiveManager.ObjectiveChanged -= RefreshChecklist;
            objectiveManager.ObjectiveChanged += RefreshChecklist;
        }
    }

    private void Unsubscribe()
    {
        if (objectiveManager != null)
        {
            objectiveManager.ObjectiveChanged -= RefreshChecklist;
        }
    }

    private void EnsureThaiFont()
    {
        if (checklistText == null || checklistText.font != null)
        {
            return;
        }

        checklistText.font = Font.CreateDynamicFontFromOSFont(
            new[] { "Thonburi", "Tahoma", "Arial Unicode MS", "Arial" },
            32);
    }
}
