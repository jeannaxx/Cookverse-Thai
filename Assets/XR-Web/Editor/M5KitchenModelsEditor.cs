using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// M5: นำเข้าโมเดลครัวจาก Meshy แล้ววางทับของเดิมในฉาก (ของเดิมไม่ถูกลบ แค่ซ่อน Renderer — Collider/สคริปต์ยังทำงาน)
// 1) Build Kitchen Prefabs  : ตั้งค่า FBX + Texture 1024 + Material URP + Prefab (จุดหมุนอยู่กลางฐาน)
// 2) Dress Kitchen (Scene)  : วางโมเดลตามชื่อวัตถุในฉากที่เปิดอยู่ (MainVR / GameplayTest_M1)
// 3) Undress Kitchen        : ถอดโมเดลออก คืน Renderer เดิม
public static class M5KitchenModelsEditor
{
    private const string Root = "Assets/XR-Web/Art/Kitchen";
    private const string PrefabDir = Root + "/Prefabs";
    private const string ModelPrefix = "M5_";

    private static readonly string[] Names =
    {
        "Wok", "Mortar", "Pestle", "Spatula", "Board", "Cleaver", "ChefKnife", "Plate", "Bowl",
        "Tray", "Pot", "PotLid", "Stove", "Rack", "Sink", "Fork",
    };

    private enum Fit { Counter, Free, OnTop, InMortar }

    private sealed class Slot
    {
        public string model;
        public string target;
        public Fit fit;
        public string[] measure;      // ชื่อลูกที่ใช้วัด/ซ่อน (null = ตัว target เอง)
        public string[] hideExtra;    // วัตถุอื่นในฉากที่ต้องซ่อนด้วย
        public Vector3 wideDir;       // ปลายที่กว้างกว่า (ใบมีด/ตัวกระทะ) หันไปทางไหนในแกนของ target
        public float fill = 0.95f;
    }

    private static readonly Slot[] Slots =
    {
        // ของแขวนผนัง
        new Slot { model = "Wok", target = "Wok_Handing", fit = Fit.Free, wideDir = Vector3.down },
        new Slot { model = "Spatula", target = "Spatula__Hanging", fit = Fit.Free, wideDir = Vector3.down },
        // บนเคาน์เตอร์
        new Slot { model = "Pot", target = "Pot_OnCounter", fit = Fit.Counter },
        new Slot { model = "PotLid", target = "Pot_OnCounter", fit = Fit.OnTop },
        new Slot { model = "Stove", target = "GasStove", fit = Fit.Counter },
        new Slot { model = "Board", target = "CuttingBoard", fit = Fit.Counter },
        new Slot { model = "Cleaver", target = "Knife ", fit = Fit.Free, wideDir = Vector3.right },
        new Slot { model = "Mortar", target = "Mortar ", fit = Fit.Counter },
        new Slot { model = "Pestle", target = "Mortar ", fit = Fit.InMortar, wideDir = Vector3.down },
        new Slot { model = "Plate", target = "Plates", fit = Fit.Counter },
        new Slot { model = "Bowl", target = "ServingBowl", fit = Fit.Counter },
        new Slot { model = "Tray", target = "ServingTray", fit = Fit.Counter },
        new Slot { model = "Sink", target = "WashingCounter", fit = Fit.Counter, hideExtra = new[] { "Sink " }, fill = 1f },
        // ของที่หยิบได้ (GameplayTest_M1) — โมเดลเป็นลูกของตัวหยิบ จึงขยับตามมือ
        new Slot { model = "Spatula", target = "Spatula_Grabbable", fit = Fit.Free, measure = new[] { "Handle", "Blade" }, wideDir = Vector3.up },
        new Slot { model = "ChefKnife", target = "Kitchen_Knife", fit = Fit.Free, measure = new[] { "Knife_Handle", "Knife_Blade" }, wideDir = Vector3.right },
        new Slot { model = "Plate", target = "Serving_Plate", fit = Fit.Counter, measure = new[] { "Plate" } },
    };

    // ---------------------------------------------------------------- 1) Prefabs

    [MenuItem("Tools/Cookverse Thai/M5/Kitchen/1 Build Kitchen Prefabs")]
    public static void BuildPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(PrefabDir))
        {
            AssetDatabase.CreateFolder(Root, "Prefabs");
        }

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        int built = 0;
        var report = new List<string>();

        foreach (string n in Names)
        {
            string fbxPath = $"{Root}/{n}/{n}.fbx";
            string texPath = $"{Root}/{n}/{n}_Base.png";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath) == null)
            {
                Debug.LogWarning($"[ครัว] ไม่พบ {fbxPath} — ข้าม");
                continue;
            }

            if (AssetImporter.GetAtPath(texPath) is TextureImporter ti)
            {
                ti.maxTextureSize = 1024;
                ti.sRGBTexture = true;
                ti.mipmapEnabled = true;
                ti.textureCompression = TextureImporterCompression.Compressed;
                ti.SaveAndReimport();
            }

            if (AssetImporter.GetAtPath(fbxPath) is ModelImporter mi)
            {
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importAnimation = false;
                mi.importCameras = false;
                mi.importLights = false;
                mi.animationType = ModelImporterAnimationType.None;
                mi.isReadable = false;
                mi.SaveAndReimport();
            }

            string matPath = $"{Root}/{n}/{n}_Mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(lit);
                AssetDatabase.CreateAsset(mat, matPath);
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            mat.shader = lit;
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_Smoothness", 0.3f);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);

            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var root = new GameObject(n);
            var model = Object.Instantiate(fbx, root.transform);
            model.name = "Mesh";
            int tris = 0;
            foreach (var r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(mat, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
            }

            foreach (var mf in model.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh != null)
                {
                    tris += mf.sharedMesh.triangles.Length / 3;
                }
            }

            // จุดหมุนไว้กลางฐาน
            Bounds b = LocalBounds(root.transform, model.GetComponentsInChildren<Renderer>(true));
            model.transform.localPosition -= new Vector3(b.center.x, b.min.y, b.center.z);

            PrefabUtility.SaveAsPrefabAsset(root, $"{PrefabDir}/{n}.prefab");
            Object.DestroyImmediate(root);
            built++;
            report.Add($"{n} {tris:N0} tris, {b.size.x:F2}×{b.size.y:F2}×{b.size.z:F2}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[ครัว] สร้าง Prefab {built} ชิ้น → {PrefabDir}\n" + string.Join("\n", report));
    }

    // ---------------------------------------------------------------- 2) Dress

    [MenuItem("Tools/Cookverse Thai/M5/Kitchen/2 Dress Kitchen (Active Scene)")]
    public static void Dress()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[ครัว] หยุด Play ก่อน");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        UndressScene(scene, false);

        var placed = new Dictionary<string, Transform>();
        var done = new List<string>();
        var missing = new List<string>();

        foreach (Slot s in Slots)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{s.model}.prefab");
            if (prefab == null)
            {
                missing.Add($"{s.model} (ยังไม่ได้ Build Prefab)");
                continue;
            }

            Transform target = FindInScene(scene, s.target);
            if (target == null)
            {
                missing.Add($"'{s.target}'");
                continue;
            }

            // ตัวที่ใช้วัด/ซ่อน
            var measureRenderers = new List<Renderer>();
            if (s.measure == null)
            {
                var own = target.GetComponent<Renderer>();
                if (own != null)
                {
                    measureRenderers.Add(own);
                }
            }
            else
            {
                foreach (string c in s.measure)
                {
                    Transform ch = target.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == c);
                    var r = ch != null ? ch.GetComponent<Renderer>() : null;
                    if (r != null)
                    {
                        measureRenderers.Add(r);
                    }
                }
            }

            Bounds box;
            if (s.fit == Fit.InMortar)
            {
                // สากพิงในครก: กล่องสูงกว่าครก เอียงด้วยการหมุนหลังวาง
                if (!placed.TryGetValue(s.target, out Transform mortar))
                {
                    missing.Add($"{s.model} (ไม่มีครก)");
                    continue;
                }

                Bounds ub = MeterBounds(target, mortar.GetComponentsInChildren<Renderer>(true));
                float len = ub.size.y * 1.1f;
                box = new Bounds(new Vector3(ub.center.x, ub.min.y + ub.size.y * 0.25f + len * 0.5f, ub.center.z),
                                 new Vector3(ub.size.x * 0.22f, len, ub.size.z * 0.22f));
                measureRenderers.Clear();
            }
            else if (s.fit == Fit.OnTop)
            {
                if (!placed.TryGetValue(s.target, out Transform under))
                {
                    missing.Add($"{s.model} (ไม่มีของข้างล่าง)");
                    continue;
                }

                Bounds ub = MeterBounds(target, under.GetComponentsInChildren<Renderer>(true));
                var size = new Vector3(ub.size.x * 0.8f, 0.09f, ub.size.z * 0.8f);
                box = new Bounds(new Vector3(ub.center.x, ub.max.y - 0.01f + size.y * 0.5f, ub.center.z), size);
                measureRenderers.Clear();
            }
            else
            {
                if (measureRenderers.Count == 0)
                {
                    missing.Add($"'{s.target}' (ไม่มี Renderer)");
                    continue;
                }

                box = MeterBounds(target, measureRenderers);
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(go, "Dress Kitchen");
            go.name = ModelPrefix + s.model;
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            go.transform.localScale = Vector3.one;

            // วัดขนาดโมเดลก่อนผูกกับ target (ยังไม่โดนสเกลของ target)
            Bounds mb = LocalBounds(go.transform, go.GetComponentsInChildren<Renderer>(true));
            List<Vector3> verts = VertsInRoot(go.transform);
            go.transform.SetParent(target, false);
            Place(go.transform, target, box, mb, s, verts);
            if (s.fit == Fit.InMortar)
            {
                // เอียงพิงขอบครก (หมุนรอบจุดกลางกล่อง ในแกนของ target)
                Vector3 pivotWorld = target.position + target.rotation * box.center;
                go.transform.RotateAround(pivotWorld, target.rotation * Vector3.forward, 20f);
            }

            var marker = go.AddComponent<M5KitchenDressed>();
            marker.modelName = s.model;
            foreach (Renderer r in measureRenderers)
            {
                Hide(r, marker);
            }

            if (s.hideExtra != null)
            {
                foreach (string extra in s.hideExtra)
                {
                    Transform t = FindInScene(scene, extra);
                    var r = t != null ? t.GetComponent<Renderer>() : null;
                    if (r != null)
                    {
                        Hide(r, marker);
                    }
                }
            }

            placed[s.target] = go.transform;
            done.Add($"{s.model} → {s.target.Trim()}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[ครัว] วางแล้ว {done.Count} ชิ้นใน {scene.name}\n" + string.Join("\n", done) +
                  (missing.Count > 0 ? "\nข้าม: " + string.Join(", ", missing) : ""));
    }

    [MenuItem("Tools/Cookverse Thai/M5/Kitchen/3 Undress Kitchen (Active Scene)")]
    public static void Undress()
    {
        UndressScene(SceneManager.GetActiveScene(), true);
    }

    private static void UndressScene(Scene scene, bool log)
    {
        int n = 0;
        foreach (GameObject rootGo in scene.GetRootGameObjects())
        {
            foreach (var m in rootGo.GetComponentsInChildren<M5KitchenDressed>(true))
            {
                foreach (Renderer r in m.hiddenRenderers)
                {
                    if (r != null)
                    {
                        Undo.RecordObject(r, "Undress Kitchen");
                        r.enabled = true;
                    }
                }

                Undo.DestroyObjectImmediate(m.gameObject);
                n++;
            }
        }

        if (n > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        if (log)
        {
            Debug.Log($"[ครัว] ถอดโมเดล {n} ชิ้น คืนของเดิมแล้ว");
        }
    }

    // ---------------------------------------------------------------- วางให้พอดีกล่อง

    private static void Place(Transform model, Transform target, Bounds box, Bounds mb, Slot s, List<Vector3> verts)
    {
        Vector3 ms = mb.size;
        Vector3 ts = box.size;
        var perm = new int[3];   // แกนโมเดล i → แกน target perm[i]
        var sign = new float[3] { 1f, 1f, 1f };

        if (s.fit == Fit.Free || s.fit == Fit.InMortar)
        {
            int[] mo = Order(ms);
            int[] to = Order(ts);
            for (int k = 0; k < 3; k++)
            {
                perm[mo[k]] = to[k];
            }

            // ปลายที่กว้างกว่าของแกนยาว หันไปตาม wideDir
            int longM = mo[0];
            int longT = to[0];
            float wd = s.wideDir[longT];
            if (Mathf.Abs(wd) > 0.5f)
            {
                float wideSide = WideEnd(verts, longM, mb);
                sign[longM] = Mathf.Sign(wd) * wideSide;
            }
        }
        else
        {
            perm[1] = 1;
            bool modelXLong = ms.x >= ms.z;
            bool targetXLong = ts.x >= ts.z;
            if (modelXLong == targetXLong)
            {
                perm[0] = 0;
                perm[2] = 2;
            }
            else
            {
                perm[0] = 2;
                perm[2] = 0;
                sign[2] = -1f;
            }
        }

        // ให้เป็นการหมุนจริง (det = +1)
        var m = Matrix4x4.zero;
        for (int i = 0; i < 3; i++)
        {
            Vector3 col = Vector3.zero;
            col[perm[i]] = sign[i];
            m.SetColumn(i, col);
        }

        m[3, 3] = 1f;
        if (m.determinant < 0f)
        {
            int mid = Order(ms)[1];
            sign[mid] = -sign[mid];
            Vector3 col = Vector3.zero;
            col[perm[mid]] = sign[mid];
            m.SetColumn(mid, col);
        }

        Quaternion rot = m.rotation;

        // ขนาดแบบไม่ล้นกล่อง
        float k2 = float.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            if (ms[i] > 1e-5f)
            {
                k2 = Mathf.Min(k2, ts[perm[i]] / ms[i]);
            }
        }

        k2 *= s.fill;

        Vector3 anchorModel = s.fit != Fit.Counter && s.fit != Fit.OnTop ? mb.center : new Vector3(mb.center.x, mb.min.y, mb.center.z);
        Vector3 anchorBox = s.fit != Fit.Counter && s.fit != Fit.OnTop ? box.center : new Vector3(box.center.x, box.min.y, box.center.z);
        Vector3 posMeters = anchorBox - rot * (anchorModel * k2);

        // ชดเชยสเกลไม่เท่ากันของ target (สเกลอยู่บนแกนของ target)
        Vector3 ls = target.lossyScale;
        model.localRotation = rot;
        model.localPosition = new Vector3(posMeters.x / ls.x, posMeters.y / ls.y, posMeters.z / ls.z);
        var scale = new Vector3();
        for (int i = 0; i < 3; i++)
        {
            scale[i] = k2 / Mathf.Abs(ls[perm[i]]) * Mathf.Sign(ls[perm[i]]);
        }

        model.localScale = scale;
    }

    // ปลายด้าน + ของแกน a กว้างกว่าไหม (+1/-1)
    private static float WideEnd(List<Vector3> verts, int a, Bounds mb)
    {
        float lo = mb.min[a];
        float len = mb.size[a];
        int b = (a + 1) % 3;
        int c = (a + 2) % 3;
        float Span(float from, float to)
        {
            float minB = float.MaxValue, maxB = float.MinValue, minC = float.MaxValue, maxC = float.MinValue;
            foreach (Vector3 v in verts)
            {
                float t = (v[a] - lo) / len;
                if (t < from || t > to)
                {
                    continue;
                }

                minB = Mathf.Min(minB, v[b]);
                maxB = Mathf.Max(maxB, v[b]);
                minC = Mathf.Min(minC, v[c]);
                maxC = Mathf.Max(maxC, v[c]);
            }

            return maxB < minB ? 0f : Mathf.Max(maxB - minB, maxC - minC);
        }

        return Span(0.7f, 1f) >= Span(0f, 0.3f) ? 1f : -1f;
    }

    private static int[] Order(Vector3 v)
    {
        return new[] { 0, 1, 2 }.OrderByDescending(i => v[i]).ToArray();
    }

    // ---------------------------------------------------------------- เครื่องมือ

    private static void Hide(Renderer r, M5KitchenDressed marker)
    {
        if (!r.enabled)
        {
            return;
        }

        Undo.RecordObject(r, "Dress Kitchen");
        r.enabled = false;
        marker.hiddenRenderers.Add(r);
    }

    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (GameObject rootGo in scene.GetRootGameObjects())
        {
            foreach (Transform t in rootGo.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name && !t.name.StartsWith(ModelPrefix))
                {
                    return t;
                }
            }
        }

        return null;
    }

    // ขอบเขตในกรอบของ root (root ไม่หมุน/สเกล 1 ตอนสร้าง)
    private static Bounds LocalBounds(Transform root, IEnumerable<Renderer> renderers)
    {
        return MeterBounds(root, renderers);
    }

    // ขอบเขตเป็นเมตร ในแกนของ frame (หมุนตาม frame แต่ไม่คูณสเกล)
    private static Bounds MeterBounds(Transform frame, IEnumerable<Renderer> renderers)
    {
        bool any = false;
        var b = new Bounds();
        Quaternion inv = Quaternion.Inverse(frame.rotation);
        foreach (Renderer r in renderers)
        {
            var mf = r.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                continue;
            }

            Bounds mbx = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? mbx.min.x : mbx.max.x,
                    (i & 2) == 0 ? mbx.min.y : mbx.max.y,
                    (i & 4) == 0 ? mbx.min.z : mbx.max.z);
                Vector3 p = inv * (r.transform.TransformPoint(corner) - frame.position);
                if (!any)
                {
                    b = new Bounds(p, Vector3.zero);
                    any = true;
                }
                else
                {
                    b.Encapsulate(p);
                }
            }
        }

        return b;
    }

    private static List<Vector3> VertsInRoot(Transform root)
    {
        var list = new List<Vector3>();
        Quaternion inv = Quaternion.Inverse(root.rotation);
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh == null)
            {
                continue;
            }

            Vector3[] vs = mf.sharedMesh.vertices;
            for (int i = 0; i < vs.Length; i += 3)
            {
                Vector3 w = mf.transform.TransformPoint(vs[i]);
                Vector3 p = inv * (w - root.position);
                list.Add(new Vector3(p.x / root.lossyScale.x, p.y / root.lossyScale.y, p.z / root.lossyScale.z));
            }
        }

        return list;
    }
}
