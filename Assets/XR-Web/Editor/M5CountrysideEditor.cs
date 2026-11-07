using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// M5 (7 พ.ย.): ฉากชนบทรอบบ้านแบบฟรี ไม่ต้องโหลดไฟล์ภายนอก
// - ท้องฟ้า Skybox/Procedural (ของ Unity) + หมอกบาง ๆ ให้ขอบพื้นกลืนกับฟ้า
// - แปลงนา (Quad 2 สามเหลี่ยม/แปลง) + คันนา + ภูเขาไกล + ต้นมะพร้าวทรงเรียบง่าย
// - คำนวณขอบเขตบ้าน/สวนจาก Renderer ในฉาก แล้วไม่วางอะไรทับพื้นที่เล่น
// - ไม่มี Collider (เดินเข้าไม่ได้ ไม่กินฟิสิกส์) · ตั้ง Batching Static · กดซ้ำได้ (ลบของเก่าก่อน)
// เมนู: Tools > Cookverse Thai > M5 > Setup Countryside (Free) / Remove Countryside
public static class M5CountrysideEditor
{
    private const string RootName = "Countryside_M5";
    private const string ArtFolder = "Assets/XR-Web/Art/Countryside";
    private const float FenceMargin = 2f; // ระยะรั้ว/กำแพงล่องหนจากขอบบ้าน+สวน (ม.) · ปรับตรงนี้

    [MenuItem("Tools/Cookverse Thai/M5/Setup Countryside (Free)")]
    private static void Setup()
    {
        var old = GameObject.Find(RootName);
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
        }

        EnsureFolder();
        Bounds play = ComputePlayBounds();
        float groundY = FindGroundY(play);
        float paddyY = groundY - 0.02f;

        Material young = GetMat("Paddy_Young", new Color(0.56f, 0.78f, 0.30f));
        Material green = GetMat("Paddy_Green", new Color(0.38f, 0.63f, 0.22f));
        Material gold = GetMat("Paddy_Gold", new Color(0.83f, 0.72f, 0.31f));
        Material water = GetMat("Paddy_Water", new Color(0.47f, 0.62f, 0.58f), 0.55f);
        Material dike = GetMat("Paddy_Dike", new Color(0.52f, 0.40f, 0.26f));
        Material far = GetMat("Far_Ground", new Color(0.42f, 0.60f, 0.27f));
        Material trunk = GetMat("Palm_Trunk", new Color(0.46f, 0.34f, 0.23f));
        Material frond = GetMat("Palm_Frond", new Color(0.24f, 0.50f, 0.19f));
        // ภูเขา: ใกล้ = เขียวอ่อน · ไกล = ฟ้าอมเทาจาง (ยกด้านเงาด้วย Emission ไม่ให้มืด)
        Material hillNear = GetMat("Hill_Near", new Color(0.52f, 0.70f, 0.45f), 0f, 0.45f);
        Material hillFar = GetMat("Hill_Far", new Color(0.62f, 0.74f, 0.82f), 0f, 0.6f);
        Material cloud = GetMat("Cloud", new Color(1f, 1f, 1f));

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Setup Countryside");
        var rnd = new System.Random(7);

        // พื้นที่ห้ามวาง = บ้าน + สวน + ขอบ 2 ม.
        var keepOut = new Rect(play.min.x - 2f, play.min.z - 2f, play.size.x + 4f, play.size.z + 4f);
        var center = new Vector2(play.center.x, play.center.z);

        // พื้นไกล: ปิดช่องว่างใต้เส้นขอบฟ้า
        var farGround = Prim(PrimitiveType.Quad, "FarGround", root.transform, far);
        farGround.transform.position = new Vector3(center.x, paddyY - 0.06f, center.y);
        farGround.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        farGround.transform.localScale = new Vector3(700f, 700f, 1f);

        // แปลงนา + คันนา
        var paddies = new GameObject("Paddies");
        paddies.transform.SetParent(root.transform, false);
        const float tileW = 14f, tileD = 10f, radius = 110f;
        int tiles = 0;
        for (float x = center.x - radius; x < center.x + radius; x += tileW)
        {
            for (float z = center.y - radius; z < center.y + radius; z += tileD)
            {
                var r = new Rect(x, z, tileW, tileD);
                if (r.Overlaps(keepOut) || (r.center - center).magnitude > radius)
                {
                    continue;
                }

                double p = rnd.NextDouble();
                Material m = p < 0.15 ? water : p < 0.45 ? young : p < 0.85 ? green : gold;

                var q = Prim(PrimitiveType.Quad, "Paddy", paddies.transform, m);
                q.transform.position = new Vector3(r.center.x, paddyY, r.center.y);
                q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                q.transform.localScale = new Vector3(tileW - 0.5f, tileD - 0.5f, 1f);

                var d1 = Prim(PrimitiveType.Cube, "Dike", paddies.transform, dike);
                d1.transform.position = new Vector3(r.center.x, paddyY + 0.1f, z + tileD);
                d1.transform.localScale = new Vector3(tileW, 0.25f, 0.45f);

                var d2 = Prim(PrimitiveType.Cube, "Dike", paddies.transform, dike);
                d2.transform.position = new Vector3(x + tileW, paddyY + 0.1f, r.center.y);
                d2.transform.localScale = new Vector3(0.45f, 0.25f, tileD);
                tiles++;
            }
        }

        // ภูเขา 2 ชั้น: เนินเขียวใกล้ + ภูเขาฟ้าไกล (หมอกช่วยให้จางเป็นธรรมชาติ)
        var hills = new GameObject("Hills");
        hills.transform.SetParent(root.transform, false);
        const int hillCount = 12;
        const int farCount = 10;
        for (int i = 0; i < hillCount + farCount; i++)
        {
            bool isFar = i >= hillCount;
            int n = isFar ? farCount : hillCount;
            int idx = isFar ? i - hillCount : i;
            float ang = (idx * 360f / n + (isFar ? 18f : 0f) + (float)rnd.NextDouble() * 15f) * Mathf.Deg2Rad;
            float dist = isFar ? 235f + (float)rnd.NextDouble() * 30f : 160f + (float)rnd.NextDouble() * 25f;
            var h = Prim(PrimitiveType.Sphere, isFar ? "Mountain_Far" : "Hill_Near", hills.transform, isFar ? hillFar : hillNear);
            h.transform.position = new Vector3(center.x + Mathf.Cos(ang) * dist, paddyY - (isFar ? 6f : 4f), center.y + Mathf.Sin(ang) * dist);
            h.transform.localScale = isFar
                ? new Vector3(80f + (float)rnd.NextDouble() * 50f, 45f + (float)rnd.NextDouble() * 30f, 40f + (float)rnd.NextDouble() * 20f)
                : new Vector3(45f + (float)rnd.NextDouble() * 35f, 14f + (float)rnd.NextDouble() * 12f, 28f + (float)rnd.NextDouble() * 18f);
            h.transform.rotation = Quaternion.Euler(0f, ang * Mathf.Rad2Deg, 0f);
        }

        // ต้นมะพร้าว
        var palms = new GameObject("CoconutTrees");
        palms.transform.SetParent(root.transform, false);
        float edge = Mathf.Max(play.extents.x, play.extents.z);
        int palmCount = 0;
        for (int tries = 0; tries < 200 && palmCount < 28; tries++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float dist = edge + 6f + (float)rnd.NextDouble() * 55f;
            var pos = new Vector2(center.x + Mathf.Cos(ang) * dist, center.y + Mathf.Sin(ang) * dist);
            if (keepOut.Contains(pos))
            {
                continue;
            }

            CreatePalm(palms.transform, new Vector3(pos.x, paddyY, pos.y), rnd, trunk, frond);
            palmCount++;
        }

        // ขอบเขตเดิน: พื้นรองรับกันตกแมพ + กำแพงล่องหน + รั้วไม้ไผ่ให้เห็นแนว
        var boundary = new GameObject("Boundary");
        boundary.transform.SetParent(root.transform, false);
        var safety = boundary.AddComponent<BoxCollider>();
        safety.center = new Vector3(center.x, paddyY - 0.5f, center.y);
        safety.size = new Vector3(600f, 1f, 600f);

        const float wallH = 3f, wallT = 0.4f;
        float minX = play.min.x - FenceMargin, maxX = play.max.x + FenceMargin;
        float minZ = play.min.z - FenceMargin, maxZ = play.max.z + FenceMargin;
        AddWall(boundary.transform, new Vector3((minX + maxX) * 0.5f, paddyY + wallH * 0.5f, minZ), new Vector3(maxX - minX + wallT, wallH, wallT));
        AddWall(boundary.transform, new Vector3((minX + maxX) * 0.5f, paddyY + wallH * 0.5f, maxZ), new Vector3(maxX - minX + wallT, wallH, wallT));
        AddWall(boundary.transform, new Vector3(minX, paddyY + wallH * 0.5f, (minZ + maxZ) * 0.5f), new Vector3(wallT, wallH, maxZ - minZ + wallT));
        AddWall(boundary.transform, new Vector3(maxX, paddyY + wallH * 0.5f, (minZ + maxZ) * 0.5f), new Vector3(wallT, wallH, maxZ - minZ + wallT));

        // แทนรั้ว: ป้ายเตือนเมื่อเดินใกล้ขอบ (รั้วรอบตัวทำให้รู้สึกถูกขัง)
        boundary.AddComponent<BoundaryHint>().Configure(Rect.MinMaxRect(minX, minZ, maxX, maxZ), 1.2f);

        // เมฆการ์ตูน: ก้อนกลมโพลีกอนต่ำ (~120 สามเหลี่ยม/ก้อน) หลายก้อนรวมเป็นเมฆ ลอยหมุนช้า ๆ
        var clouds = new GameObject("Clouds");
        clouds.transform.SetParent(root.transform, false);
        clouds.transform.position = new Vector3(center.x, paddyY, center.y);
        clouds.AddComponent<CloudDrift>();
        Mesh puff = GetPuffMesh();
        int cloudCount = 16;
        for (int i = 0; i < cloudCount; i++)
        {
            float ang = (float)rnd.NextDouble() * Mathf.PI * 2f;
            float dist = 70f + (float)rnd.NextDouble() * 100f;
            var c = new GameObject("Cloud");
            c.transform.SetParent(clouds.transform, false);
            c.transform.localPosition = new Vector3(Mathf.Cos(ang) * dist, 40f + (float)rnd.NextDouble() * 25f, Mathf.Sin(ang) * dist);
            c.transform.localRotation = Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f);
            int puffs = 4 + rnd.Next(3);
            for (int j = 0; j < puffs; j++)
            {
                var p = new GameObject("Puff");
                p.transform.SetParent(c.transform, false);
                p.transform.localPosition = new Vector3((j - puffs * 0.5f) * 5.5f + (float)rnd.NextDouble() * 3f, (float)rnd.NextDouble() * 2.5f, (float)rnd.NextDouble() * 4f - 2f);
                float sz = 8f + (float)rnd.NextDouble() * 7f;
                p.transform.localScale = new Vector3(sz * 1.3f, sz * 0.6f, sz);
                p.AddComponent<MeshFilter>().sharedMesh = puff;
                var mr = p.AddComponent<MeshRenderer>();
                mr.sharedMaterial = cloud;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }

        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            bool moving = t.IsChildOf(clouds.transform);
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, moving ? 0 : StaticEditorFlags.BatchingStatic);
        }

        // ท้องฟ้า + หมอก
        RenderSettings.skybox = GetSky();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.78f, 0.85f, 0.88f);
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 330f;
        DynamicGI.UpdateEnvironment();

        int tris = 0;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.sharedMesh != null)
            {
                tris += mf.sharedMesh.triangles.Length / 3;
            }
        }

        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log($"[ชนบท] สร้างแล้ว: แปลงนา {tiles} · มะพร้าว {palmCount} · ภูเขา {hillCount}+{farCount} · เมฆ {cloudCount} · ป้ายเตือนขอบ + กำแพงล่องหน {play.size.x + FenceMargin * 2f:0.#}×{play.size.z + FenceMargin * 2f:0.#} ม. · รวม ~{tris:N0} สามเหลี่ยม · พื้นที่เล่น {play.size.x:0.#}×{play.size.z:0.#} ม. · ระดับพื้น {groundY:0.##} · อย่าลืม Cmd+S");
    }

    [MenuItem("Tools/Cookverse Thai/M5/Remove Countryside")]
    private static void Remove()
    {
        var old = GameObject.Find(RootName);
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
        }

        RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
        RenderSettings.fog = false;
        DynamicGI.UpdateEnvironment();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[ชนบท] ลบฉากชนบทแล้ว คืนท้องฟ้าเดิม ปิดหมอก");
    }

    private static void AddWall(Transform parent, Vector3 center, Vector3 size)
    {
        var w = new GameObject("InvisibleWall");
        w.transform.SetParent(parent, false);
        var bc = w.AddComponent<BoxCollider>();
        bc.center = center;
        bc.size = size;
    }

    // รั้วไม้ไผ่เตี้ย: เสาทุก 2.5 ม. + ราว 2 เส้น (ไม่มี Collider ใช้กำแพงล่องหนกันแทน)
    private static int FenceLine(Transform parent, Vector3 a, Vector3 b, Material mat)
    {
        Vector3 dir = b - a;
        float len = dir.magnitude;
        int n = Mathf.Max(1, Mathf.RoundToInt(len / 2.5f));
        for (int i = 0; i <= n; i++)
        {
            var post = Prim(PrimitiveType.Cylinder, "Post", parent, mat);
            post.transform.position = a + dir * (i / (float)n) + Vector3.up * 0.45f;
            post.transform.localScale = new Vector3(0.08f, 0.45f, 0.08f);
        }

        Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        foreach (float y in new[] { 0.35f, 0.7f })
        {
            var rail = Prim(PrimitiveType.Cube, "Rail", parent, mat);
            rail.transform.position = (a + b) * 0.5f + Vector3.up * y;
            rail.transform.rotation = rot;
            rail.transform.localScale = new Vector3(0.05f, 0.05f, len);
        }

        return n + 1;
    }

    private static void CreatePalm(Transform parent, Vector3 pos, System.Random rnd, Material trunkMat, Material frondMat)
    {
        var tree = new GameObject("CoconutTree");
        tree.transform.SetParent(parent, false);
        tree.transform.position = pos;
        tree.transform.rotation = Quaternion.Euler((float)(rnd.NextDouble() * 12 - 6), (float)(rnd.NextDouble() * 360), (float)(rnd.NextDouble() * 12 - 6));

        float h = 4f + (float)rnd.NextDouble() * 2.5f;
        var trunk = Prim(PrimitiveType.Cylinder, "Trunk", tree.transform, trunkMat);
        trunk.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
        trunk.transform.localScale = new Vector3(0.28f, h * 0.5f, 0.28f);

        for (int i = 0; i < 7; i++)
        {
            var pivot = new GameObject("FrondPivot");
            pivot.transform.SetParent(tree.transform, false);
            pivot.transform.localPosition = new Vector3(0f, h, 0f);
            pivot.transform.localRotation = Quaternion.Euler(0f, i * 360f / 7f, 0f) * Quaternion.Euler(28f + (float)rnd.NextDouble() * 12f, 0f, 0f);

            var f = Prim(PrimitiveType.Cube, "Frond", pivot.transform, frondMat);
            f.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            f.transform.localScale = new Vector3(0.4f, 0.06f, 2.4f);
        }
    }

    private static GameObject Prim(PrimitiveType type, string name, Transform parent, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null)
        {
            Object.DestroyImmediate(col);
        }

        go.transform.SetParent(parent, false);
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        return go;
    }

    // ขอบเขตบ้าน + สวน จาก MeshRenderer ทั้งฉาก (ไม่นับ UI, XR Origin, ของชิ้นใหญ่ผิดปกติ)
    private static Bounds ComputePlayBounds()
    {
        bool has = false;
        Bounds b = default;
        foreach (var r in Object.FindObjectsByType<MeshRenderer>())
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy)
            {
                continue;
            }

            string rootName = r.transform.root.name;
            if (rootName == RootName || rootName.Contains("XR Origin") || r.GetComponentInParent<Canvas>() != null)
            {
                continue;
            }

            Bounds bb = r.bounds;
            if (bb.size.magnitude > 200f)
            {
                continue;
            }

            if (!has)
            {
                b = bb;
                has = true;
            }
            else
            {
                b.Encapsulate(bb);
            }
        }

        return has ? b : new Bounds(Vector3.zero, new Vector3(30f, 1f, 30f));
    }

    // ยิงลงที่ขอบพื้นที่เล่นหา Collider พื้น · ไม่เจอใช้ 0
    private static float FindGroundY(Bounds play)
    {
        float best = float.MaxValue;
        float top = play.max.y + 20f;
        for (int i = 0; i < 8; i++)
        {
            float t = i / 8f;
            float x = Mathf.Lerp(play.min.x + 0.5f, play.max.x - 0.5f, t);
            float z = Mathf.Lerp(play.min.z + 0.5f, play.max.z - 0.5f, (i * 3 % 8) / 8f);
            foreach (var p in new[] { new Vector3(x, top, play.min.z + 0.5f), new Vector3(x, top, play.max.z - 0.5f), new Vector3(play.min.x + 0.5f, top, z), new Vector3(play.max.x - 0.5f, top, z) })
            {
                foreach (var hit in Physics.RaycastAll(p, Vector3.down, 200f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.point.y > -2f && hit.point.y < 1f && hit.point.y < best)
                    {
                        best = hit.point.y;
                    }
                }
            }
        }

        return best == float.MaxValue ? 0f : best;
    }

    private static Material GetMat(string name, Color color, float smooth = 0f, float emission = 0f)
    {
        string path = $"{ArtFolder}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(sh) { name = name, enableInstancing = true };
            AssetDatabase.CreateAsset(m, path);
        }

        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Color")) m.SetColor("_Color", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
        if (emission > 0f && m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", color * emission);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    // ทรงกลมโพลีกอนต่ำสำหรับก้อนเมฆ (10×6 ช่อง) เก็บเป็น asset ใช้ร่วมกันทุกก้อน
    private static Mesh GetPuffMesh()
    {
        string path = $"{ArtFolder}/CloudPuff.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            return existing;
        }

        const int lon = 10, lat = 6;
        var verts = new Vector3[(lon + 1) * (lat + 1)];
        for (int a = 0; a <= lat; a++)
        {
            float th = Mathf.PI * a / lat;
            for (int o = 0; o <= lon; o++)
            {
                float ph = Mathf.PI * 2f * o / lon;
                verts[a * (lon + 1) + o] = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph)) * 0.5f;
            }
        }

        var tris = new int[lat * lon * 6];
        int k = 0;
        for (int a = 0; a < lat; a++)
        {
            for (int o = 0; o < lon; o++)
            {
                int i0 = a * (lon + 1) + o, i1 = i0 + lon + 1;
                tris[k++] = i0; tris[k++] = i1; tris[k++] = i0 + 1;
                tris[k++] = i0 + 1; tris[k++] = i1; tris[k++] = i1 + 1;
            }
        }

        // ให้ด้านหน้าหันออกนอก (เช็กสามเหลี่ยมแถวกลาง ถ้ากลับด้านก็สลับทั้งหมด)
        int t0 = (lat / 2) * lon * 6;
        Vector3 v0 = verts[tris[t0]], v1 = verts[tris[t0 + 1]], v2 = verts[tris[t0 + 2]];
        if (Vector3.Dot(Vector3.Cross(v1 - v0, v2 - v0), v0) < 0f)
        {
            for (int i = 0; i < tris.Length; i += 3)
            {
                (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
            }
        }

        var m = new Mesh { name = "CloudPuff", vertices = verts, triangles = tris };
        m.RecalculateNormals();
        m.RecalculateBounds();
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    private static Material GetSky()
    {
        string path = $"{ArtFolder}/Sky_Countryside.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find("Skybox/Procedural")) { name = "Sky_Countryside" };
            AssetDatabase.CreateAsset(m, path);
        }

        m.SetFloat("_SunSize", 0.04f);
        m.SetFloat("_AtmosphereThickness", 0.9f);
        m.SetColor("_SkyTint", new Color(0.55f, 0.70f, 0.95f));
        m.SetColor("_GroundColor", new Color(0.45f, 0.58f, 0.36f));
        m.SetFloat("_Exposure", 1.25f);
        EditorUtility.SetDirty(m);
        return m;
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/XR-Web/Art"))
        {
            AssetDatabase.CreateFolder("Assets/XR-Web", "Art");
        }

        if (!AssetDatabase.IsValidFolder(ArtFolder))
        {
            AssetDatabase.CreateFolder("Assets/XR-Web/Art", "Countryside");
        }
    }
}
