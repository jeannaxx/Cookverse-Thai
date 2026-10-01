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

    private void Awake()
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            CreateSessionId();
        }
    }

    public void StartNewSession()
    {
        CreateSessionId();

        if (missionManager == null)
        {
            Debug.LogError(
                "[SessionManager] MissionManager is not assigned.",
                this);
            return;
        }

        missionManager.ResetMission();

        Debug.Log(
            $"[SessionManager] New session started: {sessionId}",
            this);

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