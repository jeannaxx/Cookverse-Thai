using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// ย้ายจุดล้างผักลงอ่างโมเดลใหม่ (M5_Sink จาก Dress Kitchen) + น้ำไหลจากก๊อกจริง
// - หาปากก๊อกและก้นอ่างจากจุดยอดของโมเดล → วาง WaterSpout / BasinPoint (ลากปรับใน Scene ได้ แล้วรันซ้ำ = ใช้ตำแหน่งที่ลากไว้)
// - ย้าย WashStation_Zone ให้อยู่ระหว่างปากก๊อกกับก้นอ่าง · ป้าย WashStationUI ไปเหนืออ่าง
// - สร้าง Water_Fx (สายน้ำใส + หยดน้ำกระเซ็น · WaterStreamFx) ให้ StationZone เปิดตอนกำลังล้าง
// - ซ่อนก๊อกกล่องเดิม (Faucet_Pipe/Spout) · ปิดกล่องลอยเดิม (VegetableWashPoint, HandWashPoint)
// - ลด Collider ของเคาน์เตอร์ที่ทับอ่าง (WashingCounter / Faucer) ให้สูงแค่ก้นอ่าง → เอาผักลงไปในแอ่งได้
// ใช้ได้ทุกฉากที่มี WashStation + M5_Sink · รันซ้ำได้
public static class M1WashSinkEditor
{
    private const string WaterFolder = "Assets/Gameplay/Art/Water";
    private const string StreamMatPath = WaterFolder + "/M1_WaterStream.mat";
    private const string DropMatPath = WaterFolder + "/M1_WaterDrop.mat";

    [MenuItem("Tools/Cookverse Thai/M1/Fit Wash Station To Sink Model")]
    public static void FitAuto() => Fit(false);

    // ใช้ตำแหน่ง WaterSpout / BasinPoint ที่ลากปรับเองใน Scene
    [MenuItem("Tools/Cookverse Thai/M1/Fit Wash Station (Use My Spout + Basin)")]
    public static void FitManual() => Fit(true);

    private static void Fit(bool useMarkers)
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("หยุด Play ก่อน");
        Scene scene = SceneManager.GetActiveScene();
        Transform[] all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform Find(string n) => all.FirstOrDefault(t => t != null && t.name.Trim() == n);   // บางตัวถูกลบระหว่างทาง (Water_Fx เดิม)

        StationZone zone = all.Where(t => t != null).Select(t => t.GetComponent<StationZone>()).FirstOrDefault(z => z != null && z.name == "WashStation_Zone");
        Transform group = Find("WashStation");
        Transform sink = Find("M5_Sink");
        if (zone == null || group == null || sink == null)
        {
            Debug.LogError("[อ่างล้าง] ต้องมี WashStation_Zone (Setup Cooking Stations) และ M5_Sink (M5 › Dress Kitchen) ในฉากนี้");
            return;
        }

        // ---------- วิเคราะห์โมเดลอ่าง ----------
        var verts = new List<Vector3>();
        foreach (MeshFilter mf in sink.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null) continue;
            foreach (Vector3 v in mf.sharedMesh.vertices) verts.Add(mf.transform.TransformPoint(v));
        }
        if (verts.Count == 0) { Debug.LogError("[อ่างล้าง] M5_Sink ไม่มี Mesh"); return; }
        var b = new Bounds(verts[0], Vector3.zero);
        foreach (Vector3 v in verts) b.Encapsulate(v);
        float h = b.size.y;

        // ระดับท็อปเคาน์เตอร์ = ชั้นความสูงที่มีจุดยอดมากสุดในครึ่งบน (ไม่นับก๊อกที่อยู่สูงสุด)
        float counterTop = verts.Where(v => v.y > b.min.y + 0.45f * h && v.y < b.max.y - 0.08f * h)
            .GroupBy(v => Mathf.Round(v.y * 100f)).OrderByDescending(g => g.Count()).Select(g => g.Key / 100f)
            .DefaultIfEmpty(b.min.y + 0.75f * h).First();

        Transform spoutT = group.Find("WaterSpout");
        Transform basinT = group.Find("BasinPoint");
        Vector3 spout, basinPoint;
        if (useMarkers && spoutT != null && basinT != null)
        {
            spout = spoutT.position;        // ใช้ตำแหน่งที่ผู้ใช้ลากปรับไว้
            basinPoint = basinT.position;
        }
        else
        {
            // ก๊อก = จุดยอดที่สูงกว่าแผ่นกันน้ำหลังอ่าง (เกินท็อป 10 ซม.) · ท่อตั้ง = แถบความสูง 10–14 ซม.
            // ปากก๊อก = จุดของก๊อกที่ยื่นห่างจากท่อตั้งมากสุด (ปลายคอห่าน)
            var faucet = verts.Where(v => v.y > counterTop + 0.10f).ToList();
            if (faucet.Count > 0)
            {
                var pipe = faucet.Where(v => v.y < counterTop + 0.14f).DefaultIfEmpty(faucet.OrderBy(v => v.y).First()).ToList();
                Vector2 pipeXZ = new(pipe.Average(v => v.x), pipe.Average(v => v.z));
                Vector3 tip = faucet.OrderByDescending(v => (new Vector2(v.x, v.z) - pipeXZ).sqrMagnitude).First();
                spout = tip + Vector3.down * 0.015f;
            }
            else
            {
                spout = new Vector3(b.center.x, b.max.y - 0.05f, b.center.z);
            }

            // ก้นอ่าง: ยิง Ray จากเหนือขอบอ่างลงใต้ปากก๊อก ชน Mesh ของโมเดล (MeshCollider ชั่วคราว)
            var temp = new List<MeshCollider>();
            foreach (MeshFilter mf in sink.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                temp.Add(mc);
            }
            Physics.SyncTransforms();
            float basinY = counterTop - 0.15f;
            Vector3 from = new(spout.x, counterTop + 0.02f, spout.z);
            RaycastHit[] hits = Physics.RaycastAll(from, Vector3.down, 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            RaycastHit[] own = hits.Where(hh => temp.Contains(hh.collider as MeshCollider)).OrderBy(hh => hh.distance).ToArray();
            if (own.Length > 0) basinY = own[0].point.y;
            foreach (MeshCollider mc in temp) UnityEngine.Object.DestroyImmediate(mc);
            basinPoint = new Vector3(spout.x, basinY, spout.z);
        }

        spoutT = Marker(group, "WaterSpout", spout);
        basinT = Marker(group, "BasinPoint", basinPoint);

        // ---------- จุดล้าง ----------
        Undo.RecordObject(zone.transform, "ย้ายจุดล้าง");
        var box = zone.GetComponent<BoxCollider>();
        Undo.RecordObject(box, "ย้ายจุดล้าง");
        float zoneBottom = basinPoint.y + 0.01f, zoneTop = spout.y + 0.02f;
        zone.transform.SetPositionAndRotation(new Vector3(spout.x, (zoneBottom + zoneTop) / 2f, spout.z), Quaternion.identity);
        zone.transform.localScale = Vector3.one;
        box.center = Vector3.zero;
        float w = Mathf.Clamp(Mathf.Min(b.size.x, b.size.z) * 0.6f, 0.2f, 0.4f);
        Vector3 ls = zone.transform.lossyScale;
        box.size = new Vector3(w / ls.x, Mathf.Max(0.1f, zoneTop - zoneBottom) / ls.y, w / ls.z);

        // ---------- น้ำ ----------
        GameObject fx = BuildWater(group, spoutT, basinT);
        var zso = new SerializedObject(zone);
        zso.FindProperty("workingVisual").objectReferenceValue = fx;
        zso.ApplyModifiedPropertiesWithoutUndo();

        // ---------- ของเดิม ----------
        foreach (string n in new[] { "Faucet_Pipe", "Faucet_Spout" })
        {
            Transform t = group.Find(n);
            var mr = t != null ? t.GetComponent<Renderer>() : null;
            if (mr != null) { Undo.RecordObject(mr, "ซ่อนก๊อกเดิม"); mr.enabled = false; }
        }
        Transform oldWater = group.Find("Water_Stream");
        if (oldWater != null) { Undo.RecordObject(oldWater.gameObject, "ปิดสายน้ำเดิม"); oldWater.gameObject.SetActive(false); }

        foreach (string n in new[] { "VegetableWashPoint", "HandWashPoint" })
        {
            Transform t = Find(n);
            if (t == null) continue;
            foreach (Renderer r in t.GetComponents<Renderer>()) { Undo.RecordObject(r, "ซ่อนกล่องเดิม"); r.enabled = false; }
            foreach (Collider c in t.GetComponents<Collider>()) { Undo.RecordObject(c, "ปิดกล่องเดิม"); c.enabled = false; }
        }

        // Collider เคาน์เตอร์ที่ทับแอ่งอ่าง → ลดให้ด้านบนอยู่ที่ก้นอ่าง (ยังเป็นพื้นให้ของวางได้)
        int lowered = 0;
        foreach (string n in new[] { "WashingCounter", "Faucer", "Sink" })
        {
            Transform t = Find(n);
            var c = t != null ? t.GetComponent<BoxCollider>() : null;
            if (c == null || !c.enabled || c.isTrigger || c.bounds.max.y <= basinPoint.y + 0.01f) continue;
            Undo.RecordObject(c, "ลด Collider เคาน์เตอร์");
            Bounds wb = c.bounds;
            float newTop = basinPoint.y;
            if (newTop <= wb.min.y + 0.05f) { c.enabled = false; lowered++; continue; }
            float sy = t.lossyScale.y;
            float bottomLocal = c.center.y - c.size.y / 2f;
            float topLocal = (newTop - t.position.y) / sy;
            c.center = new Vector3(c.center.x, (bottomLocal + topLocal) / 2f, c.center.z);
            c.size = new Vector3(c.size.x, topLocal - bottomLocal, c.size.z);
            lowered++;
        }

        // ป้ายอ่างล้างเหนืออ่าง
        Transform label = Find("WashStationUI");
        if (label != null)
        {
            Undo.RecordObject(label, "ย้ายป้ายอ่างล้าง");
            label.position = new Vector3(b.center.x, b.max.y + 0.3f, b.center.z);
        }

        Debug.Log($"[อ่างล้าง] ฉาก {scene.name}: ปากก๊อก {Fmt(spout)} · ก้นอ่าง y={basinPoint.y:0.000} · ท็อปเคาน์เตอร์ y={counterTop:0.000} · " +
                  $"จุดล้าง {w * 100:0}×{(zoneTop - zoneBottom) * 100:0}×{w * 100:0} ซม. · ลด Collider เคาน์เตอร์ {lowered} ชิ้น · " +
                  "ถ้าน้ำไม่ออกตรงปากก๊อก ลาก WashStation/WaterSpout (และ BasinPoint) แล้วกด Fit Wash Station (Use My Spout + Basin)");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Transform Marker(Transform parent, string name, Vector3 pos)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            t = new GameObject(name).transform;
            Undo.RegisterCreatedObjectUndo(t.gameObject, "สร้างจุดน้ำ");
            t.SetParent(parent, false);
        }
        t.position = pos;
        t.rotation = Quaternion.identity;
        return t;
    }

    private static GameObject BuildWater(Transform group, Transform spout, Transform basin)
    {
        Transform old = group.Find("Water_Fx");
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var fx = new GameObject("Water_Fx");
        Undo.RegisterCreatedObjectUndo(fx, "สร้างน้ำ");
        fx.transform.SetParent(group, false);
        fx.transform.position = spout.position;

        GameObject stream = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stream.name = "Stream";
        UnityEngine.Object.DestroyImmediate(stream.GetComponent<Collider>());
        stream.transform.SetParent(fx.transform, false);
        var sr = stream.GetComponent<MeshRenderer>();
        sr.sharedMaterial = TransparentMaterial(StreamMatPath, "Universal Render Pipeline/Unlit", new Color(0.70f, 0.86f, 1f, 0.45f));
        sr.shadowCastingMode = ShadowCastingMode.Off;
        sr.receiveShadows = false;

        var splashGo = new GameObject("Splash");
        splashGo.transform.SetParent(fx.transform, false);
        splashGo.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // ยิงขึ้น แล้วตกด้วยแรงโน้มถ่วง
        var ps = splashGo.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.duration = 1f;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.014f);
        main.startColor = new Color(0.80f, 0.92f, 1f, 0.85f);
        main.gravityModifier = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 150;
        var emission = ps.emission;
        emission.rateOverTime = 70f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 55f;
        shape.radius = 0.012f;
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLife.color = grad;
        var pr = splashGo.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = TransparentMaterial(DropMatPath, "Universal Render Pipeline/Particles/Unlit", Color.white);
        pr.renderMode = ParticleSystemRenderMode.Billboard;
        pr.shadowCastingMode = ShadowCastingMode.Off;

        var water = fx.AddComponent<WaterStreamFx>();
        water.Configure(spout, basin, stream.transform, ps);
        fx.SetActive(false);   // StationZone เปิดเองตอนกำลังล้าง
        return fx;
    }

    private static Material TransparentMaterial(string path, string shaderName, Color color)
    {
        if (!AssetDatabase.IsValidFolder(WaterFolder)) AssetDatabase.CreateFolder("Assets/Gameplay/Art", "Water");
        Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find(shaderName);
        if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
        else if (shader != null) m.shader = shader;
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        return m;
    }

    private static string Fmt(Vector3 v) => $"({v.x:0.00}, {v.y:0.00}, {v.z:0.00})";
}
