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
                "[ผู้ให้ภารกิจ] ยังไม่ได้เชื่อมระบบจัดการภารกิจ",
                this);
            return;
        }

        Debug.Log(
            $"[ผู้ให้ภารกิจ] {npcName} ได้รับคำขอรับภารกิจ",
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
