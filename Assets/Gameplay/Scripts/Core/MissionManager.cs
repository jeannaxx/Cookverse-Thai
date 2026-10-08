using UnityEngine;
using UnityEngine.Events;

public class MissionManager : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private ObjectiveManager objectiveManager;

    [Header("Mission Events")]
    [SerializeField] private UnityEvent onMissionAccepted;
    [SerializeField] private UnityEvent onMissionAcceptRejected;
    [SerializeField] private UnityEvent onMissionReset;

    public bool IsMissionActive =>
        objectiveManager != null && objectiveManager.MissionAccepted;

    public void AcceptMission()
    {
        if (objectiveManager == null)
        {
            Debug.LogError(
                "[ภารกิจ] ยังไม่ได้เชื่อมระบบจัดการเป้าหมาย",
                this);
            return;
        }

        if (objectiveManager.TryAcceptMission())
        {
            Debug.Log("[ภารกิจ] รับภารกิจสำเร็จ", this);
            onMissionAccepted?.Invoke();
            return;
        }

        Debug.LogWarning(
            "[ภารกิจ] ไม่สามารถรับภารกิจนี้ได้",
            this);
        onMissionAcceptRejected?.Invoke();
    }

    public void ResetMission()
    {
        if (objectiveManager == null)
        {
            Debug.LogError(
                "[ภารกิจ] ยังไม่ได้เชื่อมระบบจัดการเป้าหมาย",
                this);
            return;
        }

        objectiveManager.ResetObjectives();

        Debug.Log("[ภารกิจ] รีเซ็ตภารกิจแล้ว", this);
        onMissionReset?.Invoke();
    }

    [ContextMenu("Test/Accept Mission")]
    private void TestAcceptMission()
    {
        AcceptMission();
    }

    [ContextMenu("Test/Reset Mission")]
    private void TestResetMission()
    {
        ResetMission();
    }
}
