using Unity.XR.CoreUtils;
using UnityEngine;

// แก้ตัวผู้เล่นสูงเกินเมื่อเล่นผ่านเว็บ (WebXR)
// WebXR Export ไม่รองรับการตั้ง Tracking Origin Mode ทำให้ XR Origin มองว่าเป็นโหมด Device
// แล้วบวก Camera Y Offset (1.6 ม.) ซ้ำกับความสูงหัวจริงที่เบราว์เซอร์ส่งมา (local-floor) → สายตา ~3.2 ม.
//
// เดิม (v4–v5) ตั้ง Offset = 0 ตั้งแต่เปิดเว็บ → หน้าเว็บก่อนกดเข้า VR กล้องอยู่ที่พื้น ป้ายภารกิจจมดิน
// ใหม่ (2 พ.ย., M5): คง Offset 1.6 ไว้ก่อนเข้า VR แล้วตั้งเป็น 0 เมื่อแว่นเริ่มส่งความสูงหัวจริงมา
// (ตำแหน่งกล้องเทียบ Camera Offset สูงเกิน 0.3 ม. = มีการ Track หัวแล้ว)
// ไม่เรียก API ของ WebXR Export ตรง ๆ เพราะแพ็กเกจตั้ง autoReferenced: false
// ทำงานเฉพาะใน Build แบบ Web เท่านั้น ไม่กระทบ Quest APK หรือ Simulator ใน Editor
public sealed class WebXRHeightFix : MonoBehaviour
{
    private const float TrackedHeadThreshold = 0.3f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer) return;

        var go = new GameObject("[WebXRHeightFix]");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<WebXRHeightFix>();
    }

    private XROrigin[] origins = new XROrigin[0];
    private float nextScan;

    private void Update()
    {
        // ค้นหา XR Origin ทุก 2 วินาที (รองรับการเปลี่ยนฉาก) ไม่ค้นทุกเฟรม
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 2f;
            origins = FindObjectsByType<XROrigin>(FindObjectsSortMode.None);
        }

        foreach (var origin in origins)
        {
            if (origin == null || origin.Camera == null) continue;

            Transform offsetObject = origin.CameraFloorOffsetObject != null ? origin.CameraFloorOffsetObject.transform : null;
            bool headTracked = origin.Camera.transform.localPosition.y >= TrackedHeadThreshold;

            if (!headTracked)
            {
                // ยังไม่เข้า VR: ให้กล้องอยู่ระดับสายตาปกติ (Camera Y Offset เช่น 1.6 ม.) ไม่ใช่ที่พื้น
                if (offsetObject != null && offsetObject.localPosition.y < 0.1f && origin.CameraYOffset > 0.1f)
                {
                    var q = offsetObject.localPosition;
                    offsetObject.localPosition = new Vector3(q.x, origin.CameraYOffset, q.z);
                }
                continue;
            }

            if (Mathf.Approximately(origin.CameraYOffset, 0f) && (offsetObject == null || Mathf.Approximately(offsetObject.localPosition.y, 0f))) continue;

            // เข้า VR แล้ว: ใช้ความสูงหัวจริงจาก WebXR อย่างเดียว
            origin.CameraYOffset = 0f;
            if (offsetObject != null)
            {
                var p = offsetObject.localPosition;
                offsetObject.localPosition = new Vector3(p.x, 0f, p.z);
            }
            Debug.Log("[WebXRHeightFix] " + origin.name + ": เข้า VR แล้ว → Camera Y Offset = 0 (ใช้ความสูงจริงจาก WebXR)");
        }
    }
}
