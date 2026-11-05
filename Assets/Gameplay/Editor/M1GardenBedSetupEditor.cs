using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// แปลงผักยกพื้นขอบไม้ 6 แปลงในสวน (แทนกล่องแปลงสีเรียบเดิม)
// - อ่าน Assets/Gameplay/Art/Models/GardenBedModels.json (สร้างโดย Tools/ModelGen/garden_bed_models.py)
//   แล้วสร้าง Mesh M1_GardenBed_<แบบ>.asset และวัสดุ M1_GardenBed_<Soil|Plank|Post>.mat
// - ทุกลูกของ GardenBase (Chili, Krapao, Lion, Coriander, Lime, Lemongrass): ซ่อน MeshRenderer + ปิด Collider
//   (ไม่ลบ เพราะสคริปต์ต้นพริก/กะเพราใช้ตำแหน่งแปลงเดิมเป็นจุดอ้างอิง)
// - วาง RaisedBed_<ชื่อ> ในกลุ่ม Garden_Blockout/RaisedBeds ที่ตำแหน่งเดิม บนผิวพื้นสวน มี MeshCollider ให้ Ray ลงดิน
// - ยกกล่องผักตกแต่ง (<ชื่อ>_Box) ให้ตั้งบนดินแปลงใหม่
// - จากนั้นรัน Setup Chili Plants + Setup Krapow Bushes + Setup Garden Veggies ซ้ำ ให้ต้นไม้ลงดินระดับใหม่
// รันซ้ำได้: ลบ RaisedBeds เดิมแล้วสร้างใหม่ · Mesh/วัสดุถูกเขียนทับ (GUID เดิม)
public static class M1GardenBedSetupEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string ModelFolder = "Assets/Gameplay/Art/Models";
    private const string DataPath = ModelFolder + "/GardenBedModels.json";
    private const string TemplateMaterialPath = "Assets/Gameplay/Prefabs/Stations/M1_Wood.mat";
    private const string RootName = "RaisedBeds";

    // แบบสันดินของแต่ละแปลง (ชื่อลูกของ GardenBase)
    private static readonly Dictionary<string, string> BedKinds = new Dictionary<string, string>
    {
        { "Chili", "ridge2" }, { "Krapao", "ridge2" }, { "Lemongrass", "ridge2" },
        { "Lion", "ridge3" }, { "Coriander", "ridge3" }, { "Lime", "mound" },
    };

    [Serializable] private class SubData { public string name; public float[] color; public int[] triangles; }
    [Serializable] private class MeshData { public float[] vertices; public SubData[] submeshes; }
    [Serializable] private class ModelFile { public MeshData mound; public MeshData ridge1; public MeshData ridge2; public MeshData ridge3; public float soilBase; public float ridgeHeight; }

    [MenuItem("Tools/Cookverse Thai/M1/Setup Garden Beds (Raised)")]
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
            Debug.LogError($"[แปลงผัก] ไม่พบ {DataPath} หรือวัสดุต้นแบบ {TemplateMaterialPath} จึงยกเลิก");
            return;
        }

        ModelFile data = JsonUtility.FromJson<ModelFile>(json.text);
        var meshes = new Dictionary<string, Mesh>
        {
            { "mound", SaveMesh("M1_GardenBed_mound", data.mound) },
            { "ridge1", SaveMesh("M1_GardenBed_ridge1", data.ridge1) },
            { "ridge2", SaveMesh("M1_GardenBed_ridge2", data.ridge2) },
            { "ridge3", SaveMesh("M1_GardenBed_ridge3", data.ridge3) },
        };
        Material[] materials = data.ridge2.submeshes.Select(s => SaveMaterial(s, template)).ToArray();

        Transform garden = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name.Trim() == "Garden_Blockout");
        Transform gardenBase = garden != null ? garden.Find("GardenBase") : null;
        if (gardenBase == null)
        {
            Debug.LogError("[แปลงผัก] ไม่พบ Garden_Blockout/GardenBase จึงยกเลิก");
            return;
        }

        Renderer baseRenderer = gardenBase.GetComponent<Renderer>();
        float groundY = baseRenderer != null ? baseRenderer.bounds.max.y : gardenBase.position.y;

        Transform oldRoot = garden.Find(RootName);
        if (oldRoot != null) Undo.DestroyObjectImmediate(oldRoot.gameObject);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "สร้างแปลงผักยกพื้น");
        root.transform.SetParent(garden, false);
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        // ชดเชยกรณี Garden_Blockout มีสเกล/หมุน: ตั้งค่าโลกโดยตรง
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        float soilTop = groundY + data.soilBase + data.ridgeHeight;
        int made = 0;
        foreach (Transform bed in gardenBase.Cast<Transform>().ToList())
        {
            string key = bed.name.Trim();
            if (!BedKinds.TryGetValue(key, out string kind)) continue;

            // ขนาดแปลงเดิม: ถ้ายาวตามแกน X ให้หมุน 90°
            Renderer bedRenderer = bed.GetComponent<Renderer>();
            Bounds b = bedRenderer != null ? bedRenderer.bounds : new Bounds(bed.position, new Vector3(0.8f, 0.04f, 1.2f));
            float yaw = b.size.x > b.size.z ? 90f : 0f;

            if (bedRenderer != null)
            {
                Undo.RecordObject(bedRenderer, "ซ่อนแปลงเดิม");
                bedRenderer.enabled = false;
            }
            foreach (Collider c in bed.GetComponents<Collider>())
            {
                Undo.RecordObject(c, "ปิด Collider แปลงเดิม");
                c.enabled = false;
            }

            var go = new GameObject($"RaisedBed_{key}");
            Undo.RegisterCreatedObjectUndo(go, "สร้างแปลงผักยกพื้น");
            go.transform.SetParent(root.transform, true);
            go.transform.SetPositionAndRotation(new Vector3(b.center.x, groundY, b.center.z), Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = meshes[kind];
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            go.AddComponent<MeshCollider>().sharedMesh = meshes[kind];
            go.isStatic = true;
            made++;

            // กล่องผักตกแต่งของแปลงนี้ (ถ้ามี) ยกให้ตั้งบนดิน
            Transform box = garden.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name.Trim() == $"{key}_Box");
            if (box != null)
            {
                Renderer[] rs = box.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    float bottom = rs.Min(r => r.bounds.min.y);
                    Undo.RecordObject(box, "ยกกล่องผักขึ้นบนดิน");
                    box.position += Vector3.up * (soilTop - bottom);
                }
            }
        }

        Physics.SyncTransforms();
        Debug.Log($"[แปลงผัก] สร้างแปลงยกพื้น {made} แปลง · ผิวพื้นสวน y={groundY:0.000} · ดินสูงสุด y={soilTop:0.000}");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // ปลูกต้นพริก/กะเพราใหม่ให้ลงดินระดับใหม่
        M1ChiliPlantSetupEditor.Setup();
        Physics.SyncTransforms();
        M1KrapowPlantSetupEditor.Setup();
        Physics.SyncTransforms();
        M1VeggieSetupEditor.Setup();
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
        string matName = $"M1_GardenBed_{sub.name}";
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
