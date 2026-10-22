using System;
using UnityEngine;
using UnityEngine.Events;

public class SessionManager : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private MissionManager missionManager;

    [Header("Current Session")]
    [SerializeField] private string sessionId;

    [Tooltip("โหมดที่บันทึกลง Action Log (Training หรือ Challenge)")]
    [SerializeField] private string mode = "Training";

    [Header("Session Events")]
    [SerializeField] private UnityEvent onSessionStarted;

    public string SessionId => sessionId;
    public string Mode => mode;

    private void Start()
    {
        StartNewSession();
    }

    public void StartNewSession()
    {
        CreateSessionId();

        if (missionManager == null)
        {
            Debug.LogError(
                "[รอบการเล่น] ยังไม่ได้เชื่อมระบบจัดการภารกิจ",
                this);
            return;
        }

        missionManager.ResetMission();

        // เริ่มบันทึกหลังรีเซ็ต เพื่อให้รอบใหม่เริ่มจาก Log ว่าง
        ActionLog.BeginSession(sessionId, mode);

        Debug.Log($"[รอบการเล่น] เริ่มรอบใหม่: {sessionId}", this);

        onSessionStarted?.Invoke();
    }

    private void CreateSessionId()
    {
        sessionId = $"session-{Guid.NewGuid():N}";
    }

    [ContextMenu("Test/Start New Session")]
    private void TestStartNewSession()
    {
        StartNewSession();
    }

    [ContextMenu("Test/Print Action Log")]
    private void TestPrintActionLog()
    {
        Debug.Log(
            $"[บันทึกเหตุการณ์] ทั้งหมด {ActionLog.Entries.Count} รายการ\n" +
            ActionLog.ToJson(),
            this);
    }
}
