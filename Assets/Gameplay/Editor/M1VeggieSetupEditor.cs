using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ผักตกแต่งในสวน (19 ชนิดรวมกะเพรา/พริกที่มีเครื่องมือแยก)
// - อ่าน Assets/Gameplay/Art/Models/VeggieModels.json (สร้างโดย Tools/ModelGen/veggie_models.py)
//   แล้วสร้าง Mesh M1_Veg_<ชนิด>.asset และวัสดุ M1_Veg_<ส่วน>.mat
// - จุดยึด (anchor) หาได้ 2 ที่: GardenBase/<ชื่อ> (แปลงเดิม 6 แปลง) หรือ GardenExtension/<ชื่อ> (สร้างโดย Setup Garden Expansion)
// - วางต้นตามแผนของแต่ละจุดยึด ยิง Ray ลงดิน ในกลุ่ม Garden_Blockout/VeggiePlants · ไม่มี Collider
// - ซ่อนกล่องสีเดิม <ชื่อ>_Box (ปิด Renderer/Collider ไม่ลบ)
// - ชิ้นที่เก็บได้ (ลูก/ยอด/ใบ/เหง้า) จาก VeggiePicks.json: สร้างจาก Krapow.prefab (XRGrab + Rigidbody kinematic + IngredientIdentity)
//   เปลี่ยน ingredientId/ชื่อไทย/Mesh ตามชนิด วางที่ช่องบนต้น · ระบบเก็บ/ล้าง/หั่น/Reset/ActionLog ใช้ได้เหมือนพริก/กะเพรา
//   เก็บผักที่เมนูไม่ใช้ → ObjectiveManager ตอบ "วัตถุดิบไม่ถูกต้อง" และบันทึก ActionLog (ใช้ทำ Rubric ได้)
//   จำนวนชิ้นต่อชนิดกำหนดใน PickPlans (ไม่เกินราว 6 ต่อชนิด เพื่อไม่ให้ WebXR หนัก)
// รันซ้ำได้: ลบ VeggiePlants เดิมแล้วสร้างใหม่ · จุดยึดที่ยังไม่มีจะข้าม (เตือนใน Console)
public static class M1VeggieSetupEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string ModelFolder = "Assets/Gameplay/Art/Models";
    private const string DataPath = ModelFolder + "/VeggieModels.json";
    private const string TemplateMaterialPath = "Assets/Gameplay/Prefabs/Stations/M1_Wood.mat";
    private const string RootName = "VeggiePlants";
    private const string PickDataPath = ModelFolder + "/VeggiePicks.json";
    private const string TemplatePrefabPath = "Assets/Gameplay/Prefabs/Ingredients/Krapow.prefab";
    private const float MinGrabSize = 0.05f;

    // ชนิดต้น → (ชิ้นต่อต้นสูงสุด, ชิ้นรวมสูงสุดของชนิดนี้)
    private static readonly Dictionary<string, (int perPlant, int max)> PickPlans = new Dictionary<string, (int, int)>
    {
        { "scallion", (1, 6) }, { "coriander", (1, 6) }, { "culantro", (1, 6) }, { "lemongrass", (1, 6) },
        { "lime", (6, 6) }, { "galangal", (1, 6) }, { "shallot", (1, 6) }, { "thaiBasil", (1, 4) }, { "mint", (1, 4) },
        { "eggplant", (2, 6) }, { "peaEggplant", (2, 6) }, { "chives", (1, 3) }, { "kaffirLime", (6, 6) },
        { "papaya", (4, 4) }, { "mango", (8, 8) }, { "beanTrellis", (8, 8) }, { "tomato", (2, 6) },
    };

    [Serializable] private class PickItem { public string plant; public string id; public string nameTh; public float scale; public float[] slots; public MeshData mesh; }
    [Serializable] private class PickFile { public PickItem[] items; }
    public const string ExtensionName = "GardenExtension";

    private struct Spot
    {
        public string kind; public Vector3 local; public float yaw; public float scale;
        public Spot(string k, Vector3 l, float y, float s) { kind = k; local = l; yaw = y; scale = s; }
    }

    // ตาราง: คอลัมน์ตามแกนกว้าง X · แถวตามแกนยาว Z ของแปลง · rowKinds = ชนิดผักของแต่ละแถว
    private static List<Spot> Grid(int cols, float colGap, float rowGap, Dictionary<string, float> scales, params string[] rowKinds)
    {
        var spots = new List<Spot>();
        int rows = rowKinds.Length;
        for (int c = 0; c < cols; c++)
        {
            for (int r = 0; r < rows; r++)
            {
                string k = rowKinds[r];
                var local = new Vector3((c - (cols - 1) / 2f) * colGap, 0f, (r - (rows - 1) / 2f) * rowGap);
                spots.Add(new Spot(k, local, c * 70f + r * 40f, scales != null && scales.TryGetValue(k, out float s) ? s : 1f));
            }
        }
        return spots;
    }

    private static List<Spot> Single(string kind, float scale = 1f) => new List<Spot> { new Spot(kind, Vector3.zero, 20f, scale) };

    private static Dictionary<string, List<Spot>> Plans()
    {
        var sc = new Dictionary<string, float> { { "coriander", 1.4f }, { "culantro", 1.2f }, { "shallot", 1.2f } };
        // กระบะค้าง: แกนยาว = Z ของจุดยึด · ค้างถั่ว (โมเดลยาวตามแกน X) หมุน 90° ให้ขนานกระบะ · มะเขือเทศ 3 ต้นอีกครึ่ง
        var trellis = new List<Spot>
        {
            new Spot("beanTrellis", new Vector3(0f, 0f, -0.6f), 90f, 1f),
            new Spot("tomato", new Vector3(0f, 0f, 0.25f), 10f, 1f),
            new Spot("tomato", new Vector3(0f, 0f, 0.65f), 130f, 1f),
            new Spot("tomato", new Vector3(0f, 0f, 1.05f), 250f, 1f),
        };
        return new Dictionary<string, List<Spot>>
        {
            // แปลงเดิม (GardenBase)
            { "Lion", Grid(3, 0.24f, 0.28f, sc, "scallion", "scallion", "scallion", "scallion") },
            { "Coriander", Grid(3, 0.24f, 0.28f, sc, "coriander", "coriander", "culantro", "culantro") },
            { "Lemongrass", Grid(2, 0.4f, 0.36f, sc, "lemongrass", "lemongrass", "lemongrass") },
            { "Lime", Single("lime") },
            // แปลงเพิ่ม (GardenExtension)
            { "Galangal", Grid(2, 0.4f, 0.36f, sc, "galangal", "galangal", "galangal") },
            { "Shallot", Grid(3, 0.24f, 0.34f, sc, "shallot", "shallot", "shallot") },
            { "BasilMint", Grid(2, 0.4f, 0.27f, sc, "thaiBasil", "thaiBasil", "mint", "mint") },
            { "EggplantChives", Grid(3, 0.24f, 0.36f, sc, "eggplant", "peaEggplant", "chives") },
            { "TrellisPlanter", trellis },
            { "Tree_KaffirLime", Single("kaffirLime") },
            { "Tree_Papaya", Single("papaya") },
            { "Tree_Mango", Single("mango") },
        };
    }

    [Serializable] private class SubData { public string name; public float[] color; public int[] triangles; }
    [Serializable] private class MeshData { public float[] vertices; public SubData[] submeshes; }
    [Serializable]
    private class ModelFile
    {
        public MeshData scallion, coriander, lemongrass, lime, galangal, shallot, thaiBasil, mint, eggplant, peaEggplant,
            chives, culantro, kaffirLime, papaya, mango, beanTrellis, tomato;
    }

    [MenuItem("Tools/Cookverse Thai/M1/Setup Garden Veggies (Decor)")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");
        }

        var json = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
        Material template = AssetDatabase.LoadAssetAtPath<Material>(TemplateMaterialPath);
        if (json == null || template == null)
        {
            Debug.LogError($"[ผักตกแต่ง] ไม่พบ {DataPath} หรือวัสดุต้นแบบ {TemplateMaterialPath} จึงยกเลิก");
            return;
        }

        if (!FindGarden(scene, out Transform garden, out Transform gardenBase, "[ผักตกแต่ง]")) return;

        ModelFile data = JsonUtility.FromJson<ModelFile>(json.text);
        var models = typeof(ModelFile).GetFields()
            .Select(f => (name: f.Name, mesh: (MeshData)f.GetValue(data)))
            .Where(m => m.mesh != null && m.mesh.vertices != null && m.mesh.vertices.Length > 0)
            .ToDictionary(m => m.name, m => m.mesh);
        var meshes = new Dictionary<string, Mesh>();
        var materials = new Dictionary<string, Material[]>();
        foreach (var kv in models)
        {
            meshes[kv.Key] = SaveMesh($"M1_Veg_{kv.Key}", kv.Value);
            materials[kv.Key] = kv.Value.submeshes.Select(s => SaveMaterial(s, template)).ToArray();
        }

        Transform extension = garden.Find(ExtensionName);

        Transform old = garden.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "สร้างผักตกแต่ง");
        root.transform.SetParent(garden, false);
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        Dictionary<string, List<Spot>> plans = Plans();

        // ซ่อนกล่องสีเดิมก่อน เพื่อไม่ให้ Ray ไปโดนกล่อง
        foreach (string key in plans.Keys)
        {
            Transform box = garden.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Trim() == $"{key}_Box");
            if (box == null) continue;
            foreach (Renderer r in box.GetComponentsInChildren<Renderer>(true)) { Undo.RecordObject(r, "ซ่อนกล่องผัก"); r.enabled = false; }
            foreach (Collider c in box.GetComponentsInChildren<Collider>(true)) { Undo.RecordObject(c, "ปิด Collider กล่องผัก"); c.enabled = false; }
        }

        Physics.SyncTransforms();
        int made = 0;
        var missing = new List<string>();
        var placed = new List<(string kind, Transform plant, Transform group)>();
        foreach (var kv in plans)
        {
            Transform anchor = gardenBase.Find(kv.Key);
            Quaternion yaw;
            if (anchor != null)
            {
                // แปลงเดิมเป็นกล่องสเกลไม่เท่ากัน: ยาวตาม X ให้หมุน 90°
                Vector3 size = anchor.lossyScale;
                yaw = Quaternion.Euler(0f, size.x > size.z ? 90f : 0f, 0f);
            }
            else
            {
                anchor = extension != null ? extension.Find(kv.Key) : null;
                if (anchor == null) { missing.Add(kv.Key); continue; }
                yaw = Quaternion.Euler(0f, anchor.eulerAngles.y, 0f);
            }

            var group = new GameObject($"{kv.Key}_Plants");
            group.transform.SetParent(root.transform, false);
            group.transform.SetPositionAndRotation(anchor.position, yaw);

            int n = 0;
            foreach (Spot spot in kv.Value)
            {
                if (!meshes.ContainsKey(spot.kind)) { Debug.LogWarning($"[ผักตกแต่ง] ไม่มีโมเดล {spot.kind} ใน JSON"); continue; }
                Vector3 p = anchor.position + yaw * spot.local;
                float ground = Physics.Raycast(new Vector3(p.x, anchor.position.y + 3.5f, p.z), Vector3.down, out RaycastHit hit, 6f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point.y : anchor.position.y;

                var plant = new GameObject($"{spot.kind}_{++n}");
                plant.transform.SetParent(group.transform, false);
                plant.transform.position = new Vector3(p.x, ground, p.z);
                plant.transform.rotation = yaw * Quaternion.Euler(0f, spot.yaw, 0f);
                plant.transform.localScale = Vector3.one * spot.scale;
                plant.AddComponent<MeshFilter>().sharedMesh = meshes[spot.kind];
                plant.AddComponent<MeshRenderer>().sharedMaterials = materials[spot.kind];
                plant.isStatic = true;
                placed.Add((spot.kind, plant.transform, group.transform));
                made++;
            }
        }

        if (missing.Count > 0)
        {
            Debug.LogWarning($"[ผักตกแต่ง] ยังไม่มีจุดยึด {string.Join(", ", missing)} · รัน Setup Garden Expansion ก่อน");
        }
        int picks = CreatePicks(placed, template);
        Debug.Log($"[ผักตกแต่ง] วางผัก/ต้นไม้ {made} ต้น · ชิ้นที่เก็บได้ {picks} ชิ้น");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ชิ้นที่เก็บได้: วางหลังปลูกครบทุกต้น (Collider ของชิ้นจะได้ไม่บัง Ray ตอนปลูก)
    private static int CreatePicks(List<(string kind, Transform plant, Transform group)> placed, Material template)
    {
        var json = AssetDatabase.LoadAssetAtPath<TextAsset>(PickDataPath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefabPath);
        if (json == null || prefab == null)
        {
            Debug.LogError($"[ผักตกแต่ง] ไม่พบ {PickDataPath} หรือ {TemplatePrefabPath} · ข้ามชิ้นที่เก็บได้");
            return 0;
        }

        PickFile file = JsonUtility.FromJson<PickFile>(json.text);
        int total = 0;
        foreach (PickItem item in file.items)
        {
            if (!PickPlans.TryGetValue(item.plant, out var plan) || item.slots == null || item.slots.Length < 6) continue;
            var plants = placed.Where(p => p.kind == item.plant).ToList();
            if (plants.Count == 0) continue;

            Mesh mesh = SaveMesh($"M1_VegPick_{item.plant}", item.mesh);
            Material[] materials = item.mesh.submeshes.Select(s => SaveMaterial(s, template)).ToArray();
            int slotCount = item.slots.Length / 6;
            int perPlant = Mathf.Min(plan.perPlant, slotCount);
            // กระจายชิ้นให้ทั่วแปลง: เลือกทุก ๆ stride ต้น
            int needPlants = Mathf.Max(1, Mathf.CeilToInt(plan.max / (float)perPlant));
            int stride = Mathf.Max(1, plants.Count / needPlants);
            int made = 0;

            for (int i = 0; i < plants.Count && made < plan.max; i += stride)
            {
                Transform plant = plants[i].plant;
                for (int k = 0; k < perPlant && made < plan.max; k++)
                {
                    int o = k * 6;
                    var pos = new Vector3(item.slots[o], item.slots[o + 1], item.slots[o + 2]);
                    var dir = new Vector3(item.slots[o + 3], item.slots[o + 4], item.slots[o + 5]);
                    var created = (GameObject)PrefabUtility.InstantiatePrefab(prefab, plants[i].group);
                    Undo.RegisterCreatedObjectUndo(created, "สร้างผักที่เก็บได้");
                    PrefabUtility.UnpackPrefabInstance(created, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    created.name = $"{item.id}_Pick_{++made:00}";
                    created.isStatic = false;
                    PreparePick(created, item);

                    float scale = plant.lossyScale.x * item.scale;
                    Transform t = created.transform;
                    Vector3 ps = t.parent.lossyScale;
                    t.localScale = new Vector3(scale / ps.x, scale / ps.y, scale / ps.z);
                    t.SetPositionAndRotation(plant.TransformPoint(pos), plant.rotation * Quaternion.FromToRotation(Vector3.up, dir));

                    created.GetComponent<MeshFilter>().sharedMesh = mesh;
                    created.GetComponent<MeshRenderer>().sharedMaterials = materials;
                    var box = created.GetComponent<BoxCollider>();
                    Bounds b = mesh.bounds;
                    float min = MinGrabSize / scale;
                    box.center = b.center;
                    box.size = new Vector3(Mathf.Max(b.size.x, min), Mathf.Max(b.size.y, min), Mathf.Max(b.size.z, min));
                    EditorUtility.SetDirty(created);
                }
            }
            total += made;
        }
        return total;
    }

    // เหมือนยอดกะเพรา: ลบลูกจาก prefab · ใช้ BoxCollider ตัวแม่ · kinematic อยู่นิ่งบนต้น · ตั้งข้อมูลวัตถุดิบใหม่
    private static void PreparePick(GameObject pick, PickItem item)
    {
        for (int c = pick.transform.childCount - 1; c >= 0; c--)
        {
            UnityEngine.Object.DestroyImmediate(pick.transform.GetChild(c).gameObject);
        }
        foreach (Collider other in pick.GetComponents<Collider>().Where(c => !(c is BoxCollider)).ToList())
        {
            UnityEngine.Object.DestroyImmediate(other);
        }
        if (pick.GetComponent<MeshFilter>() == null) pick.AddComponent<MeshFilter>();
        if (pick.GetComponent<MeshRenderer>() == null) pick.AddComponent<MeshRenderer>();
        if (pick.GetComponent<BoxCollider>() == null) pick.AddComponent<BoxCollider>();

        var body = pick.GetComponent<Rigidbody>();
        if (body != null) { body.isKinematic = true; body.useGravity = false; }

        var grab = pick.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
        {
            var grabData = new SerializedObject(grab);
            SerializedProperty list = grabData.FindProperty("m_Colliders");
            if (list != null) { list.ClearArray(); grabData.ApplyModifiedPropertiesWithoutUndo(); }
        }

        var id = new SerializedObject(pick.GetComponent<IngredientIdentity>());
        id.FindProperty("ingredientId").stringValue = item.id;
        id.FindProperty("legacyPrefabName").stringValue = item.id;
        id.FindProperty("displayNameThai").stringValue = item.nameTh;
        id.FindProperty("instanceId").stringValue = string.Empty;
        id.ApplyModifiedPropertiesWithoutUndo();
    }

    // หา GardenBase จากทั้งฉาก (ไม่ยึดว่าต้องเป็นลูกตรงของ Garden_Blockout) · Garden_Blockout = แม่ของ GardenBase
    public static bool FindGarden(Scene scene, out Transform garden, out Transform gardenBase, string tag)
    {
        Transform[] all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        gardenBase = all.FirstOrDefault(t => t.name.Trim() == "GardenBase" && t.GetComponent<Renderer>() != null)
            ?? all.FirstOrDefault(t => t.name.Trim() == "GardenBase");
        garden = gardenBase != null && gardenBase.parent != null
            ? gardenBase.parent
            : all.FirstOrDefault(t => t.name.Trim() == "Garden_Blockout");
        if (gardenBase == null || garden == null)
        {
            string names = string.Join(", ", all.Where(t => t.name.Contains("Garden")).Select(t => t.name).Distinct().Take(12));
            Debug.LogError($"{tag} ไม่พบ GardenBase ในฉาก (พบ: {names}) จึงยกเลิก · ถ้ากด Undo ไป ให้เปิดฉากใหม่ (File > Open Recent) แล้วลองอีกครั้ง");
            return false;
        }
        return true;
    }

    private static Mesh SaveMesh(string name, MeshData data)
    {
        string path = $"{ModelFolder}/{name}.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = mesh == null;
        if (isNew) mesh = new Mesh { name = name };
        mesh.Clear();
        var vertices = new Vector3[data.vertices.Length / 3];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(data.vertices[i * 3], data.vertices[i * 3 + 1], data.vertices[i * 3 + 2]);
        }
        mesh.vertices = vertices;
        mesh.subMeshCount = data.submeshes.Length;
        for (int s = 0; s < data.submeshes.Length; s++) mesh.SetTriangles(data.submeshes[s].triangles, s);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        if (isNew) AssetDatabase.CreateAsset(mesh, path);
        else EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    private static Material SaveMaterial(SubData sub, Material template)
    {
        string matName = $"M1_Veg_{sub.name}";
        string path = $"{ModelFolder}/{matName}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = material == null;
        if (isNew) material = new Material(template) { name = matName };
        var color = new Color(sub.color[0], sub.color[1], sub.color[2]);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", null);
        material.name = matName;
        if (isNew) AssetDatabase.CreateAsset(material, path);
        else EditorUtility.SetDirty(material);
        return material;
    }
}
