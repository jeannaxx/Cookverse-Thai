using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

// แก้ตัวผู้เล่นสูงเกินเมื่อเล่นผ่านเว็บ (WebXR)
// WebXR Export ไม่รองรับการตั้ง Tracking Origin Mode ทำให้ XR Origin มองว่าเป็นโหมด Device
// แล้วบวก Camera Y Offset (1.6 ม.) ซ้ำกับความสูงหัวจริงที่เบราว์เซอร์ส่งมา (local-floor) → สายตา ~3.2 ม.
// บนเว็บจึงตั้ง Camera Y Offset = 0 ให้ใช้ความสูงจริงอย่างเดียว
// ทำงานเฉพาะใน Build แบบ Web เท่านั้น ไม่กระทบ Quest APK หรือ Simulator ใน Editor
public static class WebXRHeightFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        if (Application.platform != RuntimePlatform.WebGLPlayer) return;
        Apply();
        SceneManager.sceneLoaded += (_, _) => Apply();
    }

    private static void Apply()
    {
        foreach (var origin in Object.FindObjectsByType<XROrigin>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            origin.CameraYOffset = 0f;
            if (origin.CameraFloorOffsetObject != null)
            {
                var p = origin.CameraFloorOffsetObject.transform.localPosition;
                origin.CameraFloorOffsetObject.transform.localPosition = new Vector3(p.x, 0f, p.z);
            }
            Debug.Log("[WebXRHeightFix] " + origin.name + ": Camera Y Offset = 0 (ใช้ความสูงจริงจาก WebXR)");
        }
    }
}
