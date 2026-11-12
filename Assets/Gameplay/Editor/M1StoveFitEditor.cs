using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ย้ายของบนเตา (ฐานกระทะ/หม้อ เปลวไฟ โซนใส่ของ ปุ่มบิด) ให้ลงไปวางบนโมเดลเตาของ M5 (M5_Stove)
// ปัญหาเดิม: สถานีเตาสร้างตามกล่อง blockout ที่สูง/ใหญ่กว่าโมเดล → หม้อลอย ปุ่มบิดหลุดจากตัวเตา · รันซ้ำได้
public static class M1StoveFitEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private static readonly Vector2 KnobOffset = Vector2.zero;   // เลื่อนปุ่ม (เมตร) ถ้ายังไม่ตรงปุ่มโมเดล
    private const float KnobScale = 0.65f;                       // ปุ่มเกม 7 ซม. → ~4.5 ซม.

    [MenuItem("Tools/Cookverse Thai/M1/Fit Stove Station To Stove Model")]
    public static void Fit()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");

        Transform[] all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform model = all.FirstOrDefault(t => t.name == "M5_Stove");
        PanContents pan = all.Select(t => t.GetComponent<PanContents>()).FirstOrDefault(c => c != null);
        if (model == null || pan == null)
        {
            Debug.LogError("[เตา] ไม่พบโมเดล M5_Stove (รัน Dress Kitchen ของ M5 ก่อน) หรือ PanContents");
            return;
        }

        Renderer[] rs = model.GetComponentsInChildren<Renderer>(true);
        Bounds stove = rs[0].bounds;
        foreach (Renderer r in rs.Skip(1)) stove.Encapsulate(r.bounds);

        Transform station = pan.transform.parent;
        Transform panBase = station.Find("Pan_Base");
        Bounds baseBounds = panBase.GetComponent<Renderer>().bounds;
        Vector3 delta = new Vector3(stove.center.x, stove.max.y, stove.center.z) -
                        new Vector3(baseBounds.center.x, baseBounds.min.y, baseBounds.center.z);

        // ของที่อยู่บนหัวเตา ย้ายเท่ากันทั้งชุด
        var onBurner = new[] { "Pan_Base", "Pan_Handle", "Pan_Contents_Zone", "Burner_Flame" }
            .Select(n => station.Find(n))
            .Concat(new[] { station.Find("PanCooking/Pan_Rim"), station.Find("TomYumCooking/Pot_OnStove"),
                            station.Find("TomYumCooking/Pot_Liquid") })
            .Where(t => t != null)
            .ToArray();

        foreach (Transform t in onBurner)
        {
            Undo.RecordObject(t, "Fit stove");
            t.position += delta;
        }

        // เปลวไฟอยู่ใต้ก้นภาชนะพอดี
        Transform flame = station.Find("Burner_Flame");
        if (flame != null)
        {
            Vector3 p = flame.position;
            flame.position = new Vector3(p.x, stove.max.y - 0.005f, p.z);
        }

        // ปุ่มบิดเกมครอบปุ่มบนโมเดลเตา (หน้าเตา +Z · ฝั่ง -X = ขวามือผู้เล่น) ย่อให้ขนาดใกล้ปุ่มจริง
        // ผู้เล่นหมุนตรงปุ่มของโมเดล และเห็นปุ่มหมุนตาม · ปรับละเอียดได้ที่ KnobOffset
        Transform knob = station.Find("HeatKnob");
        if (knob != null)
        {
            Undo.RecordObject(knob, "Fit stove knob");
            knob.position = new Vector3(stove.center.x - stove.extents.x * 0.62f + KnobOffset.x,
                                        stove.center.y - stove.extents.y * 0.25f + KnobOffset.y,
                                        stove.max.z + 0.008f);
            knob.localScale = Vector3.one * KnobScale;
        }

        // ป้ายเตา ตามลงมาด้วย (ถ้ามี)
        Transform label = all.FirstOrDefault(t => t.name == "StoveStationUI");
        if (label != null)
        {
            Undo.RecordObject(label, "Fit stove label");
            label.position += new Vector3(delta.x, delta.y, delta.z);
        }

        DressWok(station, stove);
        FitSpatula(station);

        // แสดงของตามเมนูปัจจุบัน (กะเพรา = กระทะ · ต้มยำ = หม้อ)
        MenuSceneSwitcher switcher = all.Select(t => t.GetComponent<MenuSceneSwitcher>()).FirstOrDefault(c => c != null);
        if (switcher != null) switcher.Apply();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[เตา] ย้ายของบนเตาลง {delta.y * 100f:0.0} ซม. (แนวนอน {new Vector2(delta.x, delta.z).magnitude * 100f:0.0} ซม.) · " +
                  $"เตาโมเดลกว้าง {stove.size.x * 100f:0} × {stove.size.z * 100f:0} ซม. สูง {stove.size.y * 100f:0} ซม. · ปุ่มบิดย้ายมาหน้าเตาแล้ว");
    }

    // ---------- กระทะโมเดลจริงบนเตา (แทนกล่อง blockout) ----------
    // ซ่อนภาพ Pan_Base / Pan_Rim / Pan_Handle ทุกเมนู (คง Collider ไว้รองของในกระทะ/หม้อ)
    // วางโมเดล Wok ของ M5 บนหัวเตา ด้ามชี้ไปทาง +X (ที่เดิมของด้ามกล่อง) · เฉพาะเมนูกระทะ (MenuOnly)
    private const string WokPrefabPath = "Assets/XR-Web/Art/Kitchen/Prefabs/Wok.prefab";
    private const float WokBowlDiameter = 0.30f;

    private static void DressWok(Transform station, Bounds stove)
    {
        foreach (string n in new[] { "Pan_Base", "Pan_Handle", "PanCooking/Pan_Rim" })
        {
            Transform t = station.Find(n);
            if (t == null) continue;
            MenuOnly tag = t.GetComponent<MenuOnly>() ?? Undo.AddComponent<MenuOnly>(t.gameObject);
            tag.Configure(new string[0], true);   // ไม่แสดงในเมนูไหนเลย
            tag.Apply(string.Empty);
            EditorUtility.SetDirty(tag);
        }

        Transform old = station.Find("Wok_OnStove");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WokPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[เตา] ไม่พบ " + WokPrefabPath + " จึงไม่ได้วางกระทะโมเดล");
            return;
        }

        var wok = (GameObject)PrefabUtility.InstantiatePrefab(prefab, station);
        PrefabUtility.UnpackPrefabInstance(wok, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        Undo.RegisterCreatedObjectUndo(wok, "วางกระทะบนเตา");
        wok.name = "Wok_OnStove";
        foreach (Collider c in wok.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
        foreach (Rigidbody r in wok.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(r);
        wok.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        // หาแกนยาว (แนวด้าม) และฝั่งด้าม จาก vertex: ปลายที่แคบกว่า = ด้าม
        Vector3[] verts = WorldVertices(wok);
        if (verts.Length == 0) return;
        Bounds b = Encapsulate(verts);
        bool alongX = b.size.x >= b.size.z;
        Func<Vector3, float> along = v => alongX ? v.x : v.z;
        Func<Vector3, float> across = v => alongX ? v.z : v.x;
        float min = verts.Min(along), max = verts.Max(along), len = max - min;
        float SpreadAt(bool high) {
            var end = verts.Where(v => high ? along(v) > max - len * 0.08f : along(v) < min + len * 0.08f).ToArray();
            return end.Length == 0 ? 0f : end.Max(across) - end.Min(across);
        }
        bool handleHigh = SpreadAt(true) < SpreadAt(false);
        Vector3 handleDir = alongX ? (handleHigh ? Vector3.right : Vector3.left) : (handleHigh ? Vector3.forward : Vector3.back);
        wok.transform.rotation = Quaternion.FromToRotation(handleDir, Vector3.right);

        // ย่อ/ขยายให้ตัวกระทะ (ด้านแคบ) กว้าง WokBowlDiameter
        verts = WorldVertices(wok);
        b = Encapsulate(verts);
        float bowl = Mathf.Max(0.01f, b.size.z);
        wok.transform.localScale *= WokBowlDiameter / bowl;

        // ศูนย์กลางตัวกระทะ = ฝั่งตรงข้ามด้าม (min X) + รัศมี · ก้นวางบนหัวเตา
        verts = WorldVertices(wok);
        b = Encapsulate(verts);
        Vector3 bowlCenterBottom = new(b.min.x + WokBowlDiameter / 2f, b.min.y, b.center.z);
        Vector3 target = new(stove.center.x, stove.max.y, stove.center.z);
        wok.transform.position += target - bowlCenterBottom;

        MenuOnly wokTag = Undo.AddComponent<MenuOnly>(wok);
        wokTag.Configure(new[] { MenuCatalog.KrapowChickenId }, false);
        Debug.Log($"[เตา] วางกระทะโมเดลบนเตาแล้ว (ด้ามชี้ +X) · ซ่อนกล่องกระทะเดิม");
    }

    // อ่าน vertex ได้แม้ Mesh ไม่เปิด Read/Write (ใช้ใน Editor) · ไม่มี Mesh = มุมกล่อง Renderer
    // ตะหลิวจริงยาวราว 33 ซม. (ใบ ~10 ซม.) · ย่อทั้งชิ้น (โมเดล + Collider + ปลายตะหลิว) ไม่ให้ใหญ่กว่ากระทะ 30 ซม.
    private const float SpatulaLength = 0.33f;

    private static void FitSpatula(Transform station)
    {
        Transform spatula = station.Find("PanCooking/Spatula_Grabbable");
        if (spatula == null) return;

        Renderer[] rs = spatula.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
        if (rs.Length == 0) return;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs.Skip(1)) b.Encapsulate(r.bounds);
        float length = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (length < 0.01f || Mathf.Abs(length - SpatulaLength) < 0.01f) return;

        Undo.RecordObject(spatula, "Fit spatula");
        float k = SpatulaLength / length;
        Vector3 bottom = new(b.center.x, b.min.y, b.center.z);
        spatula.localScale *= k;
        // วางบนเคาน์เตอร์ที่เดิม (ไม่ลอย/ไม่จม)
        Bounds nb = rs[0].bounds;
        foreach (Renderer r in rs.Skip(1)) nb.Encapsulate(r.bounds);
        spatula.position += bottom - new Vector3(nb.center.x, nb.min.y, nb.center.z);
        Debug.Log($"[เตา] ย่อตะหลิวจาก {length * 100f:0} ซม. เหลือ {SpatulaLength * 100f:0} ซม.");
    }

    private static Vector3[] WorldVertices(GameObject go)
    {
        Vector3[] pts = go.GetComponentsInChildren<MeshFilter>(true)
            .Where(f => f.sharedMesh != null)
            .SelectMany(f => f.sharedMesh.vertices.Select(v => f.transform.TransformPoint(v)))
            .ToArray();
        if (pts.Length > 0) return pts;
        return go.GetComponentsInChildren<Renderer>(true)
            .SelectMany(r => new[] { r.bounds.min, r.bounds.max }).ToArray();
    }

    private static Bounds Encapsulate(Vector3[] pts)
    {
        var b = new Bounds(pts[0], Vector3.zero);
        foreach (Vector3 p in pts) b.Encapsulate(p);
        return b;
    }
}
