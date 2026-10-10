using System;
using UnityEngine;
using UnityEngine.Events;

public class SessionManager : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private MissionManager missionManager;

    [Header("Current Session")]
    [SerializeField] private string sessionId;

    [Header("Session Events")]
    [SerializeField] private UnityEvent onSessionStarted;

    public string SessionId => sessionId;

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
}
