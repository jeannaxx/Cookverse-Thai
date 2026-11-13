using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.Hands;

// ป้ายตรวจอาการในแว่น (สร้างเองตอนเริ่มเกม ไม่ต้องตั้งในฉาก) · ลอยใต้สายตาเล็กน้อย
// แสดง: FPS · XR เปิดอยู่ไหม · จอยซ้าย/ขวาถูกจับไหม · มือจับนิ้ว · จำนวน Error + ข้อความล่าสุด
// ใช้หาสาเหตุ "เดินไม่ได้/มือไม่ขึ้น" บนแว่นจริง (ไม่ต้องต่อสาย USB) · ปิด: ตั้ง Enabled = false
public class M1DebugHUD : MonoBehaviour
{
    public static bool Enabled = true;

    private Text text;
    private Transform panel;
    private int errorCount;
    private string lastError = "-";
    private float fps, timer;
    private int frames;
    private readonly List<XRHandSubsystem> handSubsystems = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (!Enabled || Application.isEditor) return;
        var go = new GameObject("M1DebugHUD");
        DontDestroyOnLoad(go);
        go.AddComponent<M1DebugHUD>();
    }

    private void Awake()
    {
        Application.logMessageReceived += OnLog;

        var canvasGo = new GameObject("HUD", typeof(RectTransform), typeof(Canvas));
        canvasGo.transform.SetParent(transform, false);
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var rt = (RectTransform)canvasGo.transform;
        rt.sizeDelta = new Vector2(520, 170);
        rt.localScale = Vector3.one * 0.0009f;
        panel = rt;

        var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(rt, false);
        ((RectTransform)bg.transform).sizeDelta = rt.sizeDelta;
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        bg.GetComponent<Image>().raycastTarget = false;

        var tGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        tGo.transform.SetParent(rt, false);
        ((RectTransform)tGo.transform).sizeDelta = rt.sizeDelta - new Vector2(16, 10);
        text = tGo.GetComponent<Text>();
        text.font = ThaiFont.Get();
        text.fontSize = 20;
        text.color = new Color(0.7f, 1f, 0.7f);
        text.alignment = TextAnchor.UpperLeft;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private void OnDestroy() => Application.logMessageReceived -= OnLog;

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        errorCount++;
        lastError = (type == LogType.Exception ? "EX " : "ERR ") + (condition.Length > 140 ? condition.Substring(0, 140) : condition);
    }

    private void LateUpdate()
    {
        frames++;
        timer += Time.unscaledDeltaTime;
        if (timer >= 0.5f)
        {
            fps = frames / timer;
            frames = 0;
            timer = 0f;
            try { text.text = Status(); }
            catch (System.Exception e) { text.text = "HUD error: " + e.Message; }
        }

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 fwd = cam.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();
        Vector3 target = cam.transform.position + fwd * 0.9f + Vector3.down * 0.45f;
        panel.position = Vector3.Lerp(panel.position, target, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
        panel.rotation = Quaternion.LookRotation(panel.position - cam.transform.position, Vector3.up);
    }

    private string Status()
    {
        bool xr = UnityEngine.XR.XRSettings.isDeviceActive;
        string Ctl(XRController c) => c == null ? "ไม่มี" : c.isTracked == null ? "มี" : (c.isTracked.isPressed ? "จับได้" : "ไม่ถูกจับ");

        string hands = "ไม่มีระบบ";
        handSubsystems.Clear();
        SubsystemManager.GetSubsystems(handSubsystems);
        foreach (XRHandSubsystem h in handSubsystems)
        {
            if (!h.running) { hands = "ระบบปิด"; continue; }
            hands = $"ซ้าย {(h.leftHand.isTracked ? "✓" : "✗")} ขวา {(h.rightHand.isTracked ? "✓" : "✗")}";
            break;
        }

        return $"FPS {fps:0} · XR {(xr ? "เปิด" : "ปิด")} · กระดาน {(StartBoardUI.IsOpen ? "เปิด" : "ปิด")}\n" +
               $"จอยซ้าย {Ctl(XRController.leftHand)} · จอยขวา {Ctl(XRController.rightHand)}\n" +
               $"มือจับนิ้ว: {hands}\n" +
               $"Error {errorCount}: {lastError}";
    }
}
