using System;
using System.Collections.Generic;
using UnityEngine;

// เหตุการณ์หนึ่งรายการตาม Action Log Contract v1
// (Docs/Planning/Gameplay/GAMEPLAY_CONTENT_V1.md)
[Serializable]
public class ActionLogEntry
{
    public string sessionId;
    public string mode;
    public string timestampUtc;
    public float elapsedSeconds;
    public string action;
    public string ingredientId;
    public string instanceId;
    public string stepId;
    public string stateBefore;
    public string stateAfter;
    public string value;
    public bool isCorrect;
}

// ที่เก็บเหตุการณ์กลางของรอบการเล่น ทุกระบบส่งเหตุการณ์เข้าที่นี่
// ใช้ต่อสำหรับคำนวณคะแนนตาม Rubric และส่งให้ AI อธิบายผล
public static class ActionLog
{
    // ชื่อ Action ขั้นต่ำตาม Contract v1
    public static class Actions
    {
        public const string MissionAccepted = "MissionAccepted";
        public const string IngredientCollected = "IngredientCollected";
        public const string IngredientPlaced = "IngredientPlaced";
        public const string IngredientWashed = "IngredientWashed";
        public const string IngredientCut = "IngredientCut";
        public const string HeatChanged = "HeatChanged";
        public const string IngredientAddedToPan = "IngredientAddedToPan";
        public const string FoodStirred = "FoodStirred";
        public const string FoodStateChanged = "FoodStateChanged";
        public const string HintRequested = "HintRequested";
        public const string DishServed = "DishServed";
        public const string MissionCompleted = "MissionCompleted";
        public const string MissionReset = "MissionReset";
    }

    [Serializable]
    private class SessionLog
    {
        public string sessionId;
        public string mode;
        public List<ActionLogEntry> entries;
    }

    private static readonly List<ActionLogEntry> entries = new();
    private static float sessionStartTime;

    public static string SessionId { get; private set; } = string.Empty;
    public static string Mode { get; private set; } = string.Empty;
    public static bool LogToConsole { get; set; } = true;
    public static IReadOnlyList<ActionLogEntry> Entries => entries;

    public static event Action<ActionLogEntry> EntryAdded;

    // รองรับกรณีปิด Domain Reload ใน Enter Play Mode Options
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        entries.Clear();
        sessionStartTime = 0f;
        SessionId = string.Empty;
        Mode = string.Empty;
        LogToConsole = true;
        EntryAdded = null;
    }

    // เริ่มบันทึกรอบใหม่ ล้างเหตุการณ์ของรอบก่อนทั้งหมด
    public static void BeginSession(string sessionId, string mode)
    {
        entries.Clear();
        SessionId = sessionId ?? string.Empty;
        Mode = mode ?? string.Empty;
        sessionStartTime = Time.time;

        if (LogToConsole)
        {
            Debug.Log($"[บันทึกเหตุการณ์] เริ่มรอบ {SessionId} โหมด {Mode}");
        }
    }

    public static ActionLogEntry Record(
        string action,
        string stepId,
        IngredientIdentity ingredient,
        IngredientState? stateBefore = null,
        IngredientState? stateAfter = null,
        bool isCorrect = true,
        string value = null)
    {
        return Record(
            action,
            stepId,
            ingredient != null ? ingredient.IngredientId : null,
            ingredient != null ? ingredient.InstanceId : null,
            stateBefore,
            stateAfter,
            isCorrect,
            value);
    }

    public static ActionLogEntry Record(
        string action,
        string stepId,
        string ingredientId = null,
        string instanceId = null,
        IngredientState? stateBefore = null,
        IngredientState? stateAfter = null,
        bool isCorrect = true,
        string value = null)
    {
        var entry = new ActionLogEntry
        {
            sessionId = SessionId,
            mode = Mode,
            timestampUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            elapsedSeconds = Mathf.Max(0f, Time.time - sessionStartTime),
            action = action ?? string.Empty,
            ingredientId = ingredientId ?? string.Empty,
            instanceId = instanceId ?? string.Empty,
            stepId = stepId ?? string.Empty,
            stateBefore = stateBefore.HasValue ? stateBefore.Value.ToString() : string.Empty,
            stateAfter = stateAfter.HasValue ? stateAfter.Value.ToString() : string.Empty,
            value = value ?? string.Empty,
            isCorrect = isCorrect
        };

        entries.Add(entry);

        if (LogToConsole)
        {
            Debug.Log(Describe(entry));
        }

        EntryAdded?.Invoke(entry);
        return entry;
    }

    public static string ToJson(bool prettyPrint = true)
    {
        var log = new SessionLog
        {
            sessionId = SessionId,
            mode = Mode,
            entries = new List<ActionLogEntry>(entries)
        };

        return JsonUtility.ToJson(log, prettyPrint);
    }

    private static string Describe(ActionLogEntry entry)
    {
        string result = entry.isCorrect ? "ถูก" : "ผิด";
        string ingredient = string.IsNullOrEmpty(entry.ingredientId)
            ? string.Empty
            : $" วัตถุดิบ={entry.ingredientId}";
        string state = string.IsNullOrEmpty(entry.stateBefore) &&
                       string.IsNullOrEmpty(entry.stateAfter)
            ? string.Empty
            : $" สถานะ={entry.stateBefore}→{entry.stateAfter}";
        string value = string.IsNullOrEmpty(entry.value)
            ? string.Empty
            : $" ค่า={entry.value}";

        return $"[บันทึกเหตุการณ์] #{entries.Count} {entry.action} " +
               $"ขั้น={entry.stepId}{ingredient}{state}{value} ({result}) " +
               $"t={entry.elapsedSeconds:0.0}s";
    }
}
