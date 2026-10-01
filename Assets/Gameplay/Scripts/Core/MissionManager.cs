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
                "[MissionManager] ObjectiveManager is not assigned.",
                this);
            return;
        }

        if (objectiveManager.TryAcceptMission())
        {
            Debug.Log("[MissionManager] Mission accepted.", this);
            onMissionAccepted?.Invoke();
            return;
        }

        Debug.LogWarning(
            "[MissionManager] Mission accept request was rejected.",
            this);
        onMissionAcceptRejected?.Invoke();
    }

    public void ResetMission()
    {
        if (objectiveManager == null)
        {
            Debug.LogError(
                "[MissionManager] ObjectiveManager is not assigned.",
                this);
            return;
        }

        objectiveManager.ResetObjectives();

        Debug.Log("[MissionManager] Mission reset.", this);
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