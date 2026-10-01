using UnityEngine;
using UnityEngine.Events;

public class ObjectiveManager : MonoBehaviour
{
    public const string AcceptMissionStepId = "accept_mission";
    public const string CollectGardenStepId = "collect_garden";

    [Header("Current Objective")]
    [SerializeField] private string currentStepId = AcceptMissionStepId;
    [SerializeField] private bool missionAccepted;

    [Header("Events")]
    [SerializeField] private UnityEvent onObjectiveChanged;

    public string CurrentStepId => currentStepId;
    public bool MissionAccepted => missionAccepted;

    public bool TryAcceptMission()
    {
        if (missionAccepted || currentStepId != AcceptMissionStepId)
        {
            Debug.LogWarning("[ObjectiveManager] Mission was already accepted.");
            return false;
        }

        missionAccepted = true;
        currentStepId = CollectGardenStepId;

        Debug.Log("[ObjectiveManager] Mission accepted. Current step: collect_garden");
        onObjectiveChanged?.Invoke();

        return true;
    }

    public void ResetObjectives()
    {
        missionAccepted = false;
        currentStepId = AcceptMissionStepId;

        Debug.Log("[ObjectiveManager] Objectives reset to accept_mission");
        onObjectiveChanged?.Invoke();
    }
    [ContextMenu("Test/Accept Mission")]
    private void TestAcceptMission()
    {
        TryAcceptMission();
    }

    [ContextMenu("Test/Reset Objectives")]
    private void TestResetObjectives()
    {
        ResetObjectives();
    }




    
}