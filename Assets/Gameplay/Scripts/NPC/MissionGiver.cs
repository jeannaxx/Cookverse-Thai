using UnityEngine;
using UnityEngine.Events;

public class MissionGiver : MonoBehaviour
{
    [Header("NPC Information")]
    [SerializeField] private string npcName = "ป้าแสง";

    [Header("Required References")]
    [SerializeField] private MissionManager missionManager;

    [Header("NPC Events")]
    [SerializeField] private UnityEvent onMissionRequested;

    public string NpcName => npcName;

    public void GiveMission()
    {
        if (missionManager == null)
        {
            Debug.LogError(
                "[MissionGiver] MissionManager is not assigned.",
                this);
            return;
        }

        Debug.Log(
            $"[MissionGiver] {npcName} received a mission request.",
            this);

        missionManager.AcceptMission();
        onMissionRequested?.Invoke();
    }

    [ContextMenu("Test/Give Mission")]
    private void TestGiveMission()
    {
        GiveMission();
    }
}