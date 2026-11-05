using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ต้นพริกในสวน + ลูกพริกที่ผู้เล่นเก็บ (ห้อยบนต้น)
// - อ่าน Assets/Gameplay/Art/Models/ChiliModels.json (สร้างโดย Tools/ModelGen/chili_models.py)
//   แล้วสร้าง Mesh M1_ChiliPlant.asset / M1_ChiliFruit.asset และวัสดุ M1_Chili_*.mat
// - วางต้นพริก PlantCount ต้น (ไม่มี Collider) เป็นตาราง 2 แถวบนแปลงพริก
// - ลูกพริกทุกลูกเก็บได้: ต้นละ FruitsPerPlant ลูก · ใช้พริกเดิม 3 ลูก (Chili_Box*) แล้วโคลนเพิ่ม (Chili_Pick_NN)
//   เปลี่ยนหน้าตาจากกล่องเป็นลูกพริก ห้อยที่ช่องบนต้น · ภารกิจยังต้องการ 3 ลูก
//   ระบบเก็บ/ล้าง/หั่น/Reset ใช้ได้เหมือนเดิม (Rigidbody kinematic เดิม · Collider กล่องขยายให้จับง่าย)
// รันซ้ำได้: ลบต้นพริกเดิมแล้วสร้างใหม่ · Mesh/วัสดุเดิมถูกเขียนทับ (GUID เดิม)
public static class M1ChiliPlantSetupEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string ModelFolder = "Assets/Gameplay/Art/Models";
    private const string DataPath = ModelFolder + "/ChiliModels.json";
    private const string TemplateMaterialPath = "Assets/Gameplay/Prefabs/Stations/M1_Wood.mat";
    private const string RootName = "ChiliPlants";

    private const float PlantScale = 1.2f;   // ต้นสูงราว 35 ซม.
    private const float FruitScale = 1.5f;   // ลูกที่เก็บได้ยาวราว 7 ซม. (ใหญ่กว่าจริง หยิบในแว่นง่าย)
    private const float MinGrabSize = 0.04f; // Collider กว้างอย่างน้อย 4 ซม. ทุกแกน
    private const int PlantCount = 10;
    private const int FruitsPerPlant = 4;    // 10 × 4 = 40 ชิ้นจับได้ (90 หนักเกินสำหรับ WebXR)
    private const float RowGap = 0.4f;       // ระยะระหว่าง 2 แถว (แกน X)
    private const string ClonePrefix = "Chili_Pick_";
    private const string ChiliPrefabPath = "Assets/Gameplay/Prefabs/Ingredients/Chili.prefab";

    [Serializable] private class SubData { public string name; public float[] color; public int[] triangles; }
    [Serializable] private class MeshData { public float[] vertices; public SubData[] submeshes; }
    [Serializable] private class ModelFile { public MeshData plant; public MeshData fruit; public float[] plantFruitSlots; }

    [MenuItem("Tools/Cookverse Thai/M1/Setup Chili Plants (Garden)")]
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
            Debug.LogError($"[ต้นพริก] ไม่พบ {DataPath} หรือวัสดุต้นแบบ {TemplateMaterialPath} จึงยกเลิก");
            return;
        }

        ModelFile data = JsonUtility.FromJson<ModelFile>(json.text);
        Mesh plantMesh = SaveMesh("M1_ChiliPlant", data.plant);
        Mesh fruitMesh = SaveMesh("M1_ChiliFruit", data.fruit);
        Material[] plantMaterials = data.plant.submeshes.Select(s => SaveMaterial(s, template)).ToArray();
        Material[] fruitMaterials = data.fruit.submeshes.Select(s => SaveMaterial(s, template)).ToArray();
        var slots = new List<Vector3>();
        for (int i = 0; i + 2 < data.plantFruitSlots.Length; i += 3)
        {
            slots.Add(new Vector3(data.plantFruitSlots[i], data.plantFruitSlots[i + 1], data.plantFruitSlots[i + 2]));
        }

        int perPlant = Mathf.Min(FruitsPerPlant, slots.Count);

        // ที่ตั้ง: แปลงดิน GardenBase/Chili เป็นศูนย์กลาง · ลูกพริกอยู่ในกลุ่ม Garden_Blockout/Chili
        // ลูกพริกที่ขาดสร้างใหม่จาก Chili.prefab (ไม่ต้องพึ่งพริกเดิมในฉาก รันซ้ำได้เสมอ)
        Transform garden = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name.Trim() == "Garden_Blockout");
        Transform bed = garden != null ? garden.Find("GardenBase/Chili") : null;
        Transform group = garden != null
            ? garden.Cast<Transform>().FirstOrDefault(t => t.name.Trim() == "Chili")
            : null;
        GameObject chiliPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChiliPrefabPath);

        if (garden == null || bed == null || chiliPrefab == null)
        {
            Debug.LogError($"[ต้นพริก] ไม่พบ Garden_Blockout, แปลง GardenBase/Chili หรือ {ChiliPrefabPath} จึงยกเลิก");
            return;
        }

        if (group == null)
        {
            group = new GameObject("Chili").transform;
            Undo.RegisterCreatedObjectUndo(group.gameObject, "สร้างกลุ่มลูกพริก");
            group.SetParent(garden, false);
            group.position = bed.position;
        }

        Transform previous = garden.Find(RootName);
        if (previous != null)
        {
            Undo.DestroyObjectImmediate(previous.gameObject);
        }

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "สร้างต้นพริก");
        root.transform.SetParent(garden, false);
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        // ตัวแม่อาจมีสเกล จึงตั้งสเกลโลกของกลุ่มเป็น 1
        Vector3 parentScale = garden.lossyScale;
        root.transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);

        Vector3 center = bed.position;
        int perRow = Mathf.CeilToInt(PlantCount / 2f);
        float spacing = 0.26f; // 5 ต้นกินยาว ~1.04 ม. อยู่ในแปลงยาว 1.2 ม.

        int need = PlantCount * perPlant;
        List<IngredientIdentity> chilis = group.GetComponentsInChildren<IngredientIdentity>(true)
            .Where(i => i.IngredientId == ObjectiveManager.ChiliIngredientId)
            .OrderBy(i => i.name.StartsWith(ClonePrefix) ? 1 : 0).ThenBy(i => i.name)
            .ToList();

        foreach (IngredientIdentity extra in chilis.Skip(need).ToList())
        {
            Undo.DestroyObjectImmediate(extra.gameObject);
        }

        chilis = chilis.Take(need).ToList();
        for (int n = chilis.Count; n < need; n++)
        {
            var created = (GameObject)PrefabUtility.InstantiatePrefab(chiliPrefab, group);
            Undo.RegisterCreatedObjectUndo(created, "เพิ่มพริกที่เก็บได้");
            PrefabUtility.UnpackPrefabInstance(created, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            created.name = $"{ClonePrefix}{n + 1:00}";
            var id = new SerializedObject(created.GetComponent<IngredientIdentity>());
            id.FindProperty("instanceId").stringValue = string.Empty;
            id.ApplyModifiedPropertiesWithoutUndo();
            var body = created.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;   // ห้อยนิ่งบนต้นจนกว่าจะหยิบ (เหมือนพริกเดิม)
                body.useGravity = false;
            }

            chilis.Add(created.GetComponent<IngredientIdentity>());
        }

        // ปิด Collider ของพริกชั่วคราว เพื่อยิง Ray หาผิวแปลงใต้พริก
        var colliders = chilis.SelectMany(c => c.GetComponentsInChildren<Collider>()).Where(c => c.enabled).ToList();
        colliders.ForEach(c => c.enabled = false);
        Physics.SyncTransforms();

        try
        {
            for (int i = 0; i < PlantCount; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                Vector3 p = center + new Vector3((row - 0.5f) * RowGap, 0f, (col - (perRow - 1) / 2f) * spacing);
                float ground = Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    ? hit.point.y
                    : p.y - 0.1f;

                // แถวซ้ายหันช่องลูกพริกไป -X แถวขวาไป +X (ด้านนอกแปลง เอื้อมถึงง่าย) · ส่ายเล็กน้อยให้ไม่ซ้ำกัน
                var plant = new GameObject($"ChiliPlant_{i + 1}");
                Undo.RegisterCreatedObjectUndo(plant, "สร้างต้นพริก");
                plant.transform.SetParent(root.transform, false);
                plant.transform.position = new Vector3(p.x, ground, p.z);
                plant.transform.rotation = Quaternion.Euler(0f, (row == 0 ? 180f : 0f) + (col % 3 - 1) * 25f, 0f);
                plant.transform.localScale = Vector3.one * PlantScale;
                plant.AddComponent<MeshFilter>().sharedMesh = plantMesh;
                plant.AddComponent<MeshRenderer>().sharedMaterials = plantMaterials;

                for (int f = 0; f < perPlant; f++)
                {
                    HangChili(chilis[i * perPlant + f], plant.transform.TransformPoint(slots[f]), fruitMesh, fruitMaterials);
                }
            }
        }
        finally
        {
            colliders.ForEach(c => c.enabled = true);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[ต้นพริก] วางต้นพริก {PlantCount} ต้น · พริกที่เก็บได้ {PlantCount * perPlant} ลูก ห้อยบนต้นแล้ว " +
                  $"(ต้น {plantMesh.triangles.Length / 3} สามเหลี่ยม · ลูก {fruitMesh.triangles.Length / 3} สามเหลี่ยม)");
    }

    // ลูกพริกที่เก็บได้: Mesh ลูกพริก · สเกลโลก FruitScale · ห้อยที่จุด attach (จุดกำเนิดของ Mesh อยู่ที่ขั้ว)
    private static void HangChili(IngredientIdentity chili, Vector3 attachWorld, Mesh mesh, Material[] materials)
    {
        Transform t = chili.transform;
        Undo.RecordObject(t, "ห้อยพริกบนต้น");
        Vector3 parentScale = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(FruitScale / parentScale.x, FruitScale / parentScale.y, FruitScale / parentScale.z);
        t.rotation = Quaternion.identity;
        t.position = attachWorld;

        var filter = chili.GetComponent<MeshFilter>();
        var renderer = chili.GetComponent<MeshRenderer>();
        if (filter != null) { Undo.RecordObject(filter, "เปลี่ยนเป็นลูกพริก"); filter.sharedMesh = mesh; }
        if (renderer != null) { Undo.RecordObject(renderer, "เปลี่ยนเป็นลูกพริก"); renderer.sharedMaterials = materials; }

        var box = chili.GetComponent<BoxCollider>();
        if (box != null)
        {
            Undo.RecordObject(box, "ปรับ Collider ลูกพริก");
            Bounds b = mesh.bounds;
            float min = MinGrabSize / FruitScale;
            box.center = b.center;
            box.size = new Vector3(Mathf.Max(b.size.x, min), Mathf.Max(b.size.y, min), Mathf.Max(b.size.z, min));
        }

        EditorUtility.SetDirty(chili.gameObject);
    }

    private static Mesh SaveMesh(string name, MeshData data)
    {
        string path = $"{ModelFolder}/{name}.asset";
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = mesh == null;
        if (isNew)
        {
            mesh = new Mesh { name = name };
        }

        mesh.Clear();

        var vertices = new Vector3[data.vertices.Length / 3];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new Vector3(data.vertices[i * 3], data.vertices[i * 3 + 1], data.vertices[i * 3 + 2]);
        }

        mesh.vertices = vertices;
        mesh.subMeshCount = data.submeshes.Length;
        for (int s = 0; s < data.submeshes.Length; s++)
        {
            mesh.SetTriangles(data.submeshes[s].triangles, s);
        }

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (isNew)
        {
            AssetDatabase.CreateAsset(mesh, path);
        }
        else
        {
            EditorUtility.SetDirty(mesh);
        }

        AssetDatabase.SaveAssets();
        return mesh;
    }

    private static Material SaveMaterial(SubData sub, Material template)
    {
        string path = $"{ModelFolder}/M1_Chili_{sub.name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = material == null;
        if (isNew)
        {
            material = new Material(template) { name = $"M1_Chili_{sub.name}" };
        }

        var color = new Color(sub.color[0], sub.color[1], sub.color[2]);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", null);

        // ใบไม้สองหน้าทำใน Mesh แล้ว ไม่ต้องปิด Culling
        material.name = $"M1_Chili_{sub.name}";
        if (isNew) AssetDatabase.CreateAsset(material, path);
        else EditorUtility.SetDirty(material);
        return material;
    }
}
