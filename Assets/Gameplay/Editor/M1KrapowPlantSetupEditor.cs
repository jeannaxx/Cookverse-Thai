using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// พุ่มกะเพราแดงในสวน + ยอดกะเพราที่เก็บได้ (ปักอยู่บนพุ่ม)
// - อ่าน Assets/Gameplay/Art/Models/KrapowModels.json (Tools/ModelGen/krapow_models.py)
//   สร้าง Mesh M1_KrapowBush.asset / M1_KrapowSprig.asset และวัสดุ M1_Krapow_*.mat
// - วางพุ่ม BushCount พุ่ม เป็นตาราง Columns × Rows ในแปลงกะเพรา (ตำแหน่งเดิมของ Krapao_Block)
// - ยอดทุกยอดเก็บได้: พุ่มละ SprigsPerBush ยอด · สร้างจาก Krapow.prefab ไว้ในกลุ่ม KrapowPicks (Krapow_Pick_NN)
//   ก้อนกะเพราแบบเก่าใต้ Karpao (หลายชิ้นลูก เปลี่ยนหน้าตาไม่ได้) ถูกลบแทนที่
//   ระบบเก็บ/ล้าง/ใส่กระทะ/Reset เหมือนเดิม · ภารกิจยังต้องการ 1 กำ
// รันซ้ำได้: ลบพุ่มและยอดที่โคลนไว้เดิมแล้วสร้างใหม่ ที่ตำแหน่งเดิม
public static class M1KrapowPlantSetupEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string ModelFolder = "Assets/Gameplay/Art/Models";
    private const string DataPath = ModelFolder + "/KrapowModels.json";
    private const string TemplateMaterialPath = "Assets/Gameplay/Prefabs/Stations/M1_Wood.mat";
    private const string RootName = "KrapowPlants";
    private const string ClonePrefix = "Krapow_Pick_";
    private const string PicksName = "KrapowPicks";
    private const string KrapowPrefabPath = "Assets/Gameplay/Prefabs/Ingredients/Krapow.prefab";

    private const int Columns = 2;            // แกน X
    private const int Rows = 3;               // แกน Z
    private const float ColumnGap = 0.42f;
    private const float RowGap = 0.355f;
    private const int SprigsPerBush = 3;      // 6 × 3 = 18 ยอด
    private const float BushScale = 1.2f;     // พุ่มสูงราว 30 ซม.
    private const float SprigScale = 1.3f;    // ยอดที่เก็บได้ยาวราว 14 ซม.
    private const float MinGrabSize = 0.05f;

    [Serializable] private class SubData { public string name; public float[] color; public int[] triangles; }
    [Serializable] private class MeshData { public float[] vertices; public SubData[] submeshes; }
    [Serializable] private class ModelFile { public MeshData plant; public MeshData fruit; public float[] slots; }

    [MenuItem("Tools/Cookverse Thai/M1/Setup Krapow Bushes (Garden)")]
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
            Debug.LogError($"[กะเพรา] ไม่พบ {DataPath} หรือวัสดุต้นแบบ {TemplateMaterialPath} จึงยกเลิก");
            return;
        }

        ModelFile data = JsonUtility.FromJson<ModelFile>(json.text);
        Mesh bushMesh = SaveMesh("M1_KrapowBush", data.plant);
        Mesh sprigMesh = SaveMesh("M1_KrapowSprig", data.fruit);
        Material[] bushMaterials = data.plant.submeshes.Select(s => SaveMaterial(s, template)).ToArray();
        Material[] sprigMaterials = data.fruit.submeshes.Select(s => SaveMaterial(s, template)).ToArray();

        var slots = new List<(Vector3 pos, Vector3 dir)>();
        for (int i = 0; i + 5 < data.slots.Length; i += 6)
        {
            slots.Add((new Vector3(data.slots[i], data.slots[i + 1], data.slots[i + 2]),
                new Vector3(data.slots[i + 3], data.slots[i + 4], data.slots[i + 5])));
        }

        int perBush = Mathf.Min(SprigsPerBush, slots.Count);

        // ยอดที่เก็บได้อยู่ในกลุ่ม KrapowPicks ใต้ Garden_Blockout · สร้างจาก Krapow.prefab ถ้ายังไม่มี/ไม่พอ
        Transform garden = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name.Trim() == "Garden_Blockout");
        GameObject krapowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(KrapowPrefabPath);

        if (garden == null || krapowPrefab == null)
        {
            Debug.LogError($"[กะเพรา] ไม่พบ Garden_Blockout หรือ {KrapowPrefabPath} จึงยกเลิก");
            return;
        }

        // ศูนย์กลางแปลง: ตำแหน่งกลุ่มพุ่มเดิม (KrapowPlants) → ก้อนกะเพราเดิมใต้ Karpao → ตั้งค่าไม่ได้
        Transform previous = garden.Find(RootName);
        Transform oldBed = garden.Find("Karpao");
        Vector3 center;
        if (previous != null)
        {
            center = previous.position;
            Undo.DestroyObjectImmediate(previous.gameObject);
        }
        else if (oldBed != null && oldBed.childCount > 0)
        {
            center = oldBed.Cast<Transform>().Aggregate(Vector3.zero, (a, c) => a + c.position) / oldBed.childCount;
        }
        else
        {
            Debug.LogError("[กะเพรา] ไม่รู้ตำแหน่งแปลงกะเพรา (ไม่มี KrapowPlants หรือ Karpao) จึงยกเลิก");
            return;
        }

        // มีแปลงดิน GardenBase/Krapao → จัดกึ่งกลางตามแปลง (แกน X/Z) ให้ไม่ล้นขอบแปลงยกพื้น
        Transform krapaoBed = garden.Find("GardenBase/Krapao");
        if (krapaoBed != null)
        {
            center = new Vector3(krapaoBed.position.x, center.y, krapaoBed.position.z);
        }

        // ก้อนกะเพราแบบเก่า (หลายชิ้นลูก) ใต้ Karpao ถูกแทนด้วยยอดใหม่ทั้งหมด
        if (oldBed != null)
        {
            Undo.DestroyObjectImmediate(oldBed.gameObject);
        }

        Transform picks = garden.Find(PicksName);
        if (picks == null)
        {
            picks = new GameObject(PicksName).transform;
            Undo.RegisterCreatedObjectUndo(picks.gameObject, "สร้างกลุ่มยอดกะเพรา");
            picks.SetParent(garden, false);
            picks.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        int bushCount = Columns * Rows;
        int need = bushCount * perBush;
        List<IngredientIdentity> sprigs = picks.GetComponentsInChildren<IngredientIdentity>(true)
            .Where(i => i.IngredientId == ObjectiveManager.KrapowLeafIngredientId)
            .OrderBy(i => i.name).ToList();

        foreach (IngredientIdentity extra in sprigs.Skip(need).ToList())
        {
            Undo.DestroyObjectImmediate(extra.gameObject);
        }

        sprigs = sprigs.Take(need).ToList();
        for (int n = sprigs.Count; n < need; n++)
        {
            var created = (GameObject)PrefabUtility.InstantiatePrefab(krapowPrefab, picks);
            Undo.RegisterCreatedObjectUndo(created, "เพิ่มยอดกะเพราที่เก็บได้");
            PrefabUtility.UnpackPrefabInstance(created, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            created.name = $"{ClonePrefix}{n + 1:00}";
            sprigs.Add(created.GetComponent<IngredientIdentity>());
        }

        foreach (IngredientIdentity sprig in sprigs)
        {
            PrepareSprigObject(sprig.gameObject);
        }

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "สร้างพุ่มกะเพรา");
        root.transform.SetParent(garden, false);
        root.transform.SetPositionAndRotation(center, Quaternion.identity);
        Vector3 gs = garden.lossyScale;
        root.transform.localScale = new Vector3(1f / gs.x, 1f / gs.y, 1f / gs.z);

        var colliders = sprigs.SelectMany(c => c.GetComponentsInChildren<Collider>()).Where(c => c.enabled).ToList();
        colliders.ForEach(c => c.enabled = false);
        Physics.SyncTransforms();

        try
        {
            for (int i = 0; i < bushCount; i++)
            {
                int col = i % Columns;
                int row = i / Columns;
                Vector3 p = center + new Vector3((col - (Columns - 1) / 2f) * ColumnGap, 0f, (row - (Rows - 1) / 2f) * RowGap);
                float ground = Physics.Raycast(p + Vector3.up * 0.6f, Vector3.down, out RaycastHit hit, 2f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point.y : p.y - 0.1f;

                var bush = new GameObject($"KrapowBush_{i + 1}");
                Undo.RegisterCreatedObjectUndo(bush, "สร้างพุ่มกะเพรา");
                bush.transform.SetParent(root.transform, false);
                bush.transform.position = new Vector3(p.x, ground, p.z);
                bush.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
                bush.transform.localScale = Vector3.one * BushScale;
                bush.AddComponent<MeshFilter>().sharedMesh = bushMesh;
                bush.AddComponent<MeshRenderer>().sharedMaterials = bushMaterials;

                for (int s = 0; s < perBush; s++)
                {
                    Vector3 basePos = bush.transform.TransformPoint(slots[s].pos);
                    Vector3 dir = bush.transform.TransformDirection(slots[s].dir).normalized;
                    PlaceSprig(sprigs[i * perBush + s], basePos, Quaternion.FromToRotation(Vector3.up, dir),
                        sprigMesh, sprigMaterials);
                }
            }
        }
        finally
        {
            colliders.ForEach(c => c.enabled = true);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[กะเพรา] วางพุ่มกะเพรา {bushCount} พุ่ม · ยอดที่เก็บได้ {bushCount * perBush} ยอด " +
                  $"(พุ่ม {bushMesh.triangles.Length / 3} สามเหลี่ยม · ยอด {sprigMesh.triangles.Length / 3} สามเหลี่ยม)");
    }

    // ยอดกะเพราที่เก็บได้ = วัตถุเดียว: ลบชิ้นลูกแบบเก่า (ใบ/ก้านแยกชิ้น) · มี MeshFilter/Renderer/BoxCollider ที่ตัวแม่
    // อยู่นิ่งบนพุ่มจนกว่าจะหยิบ (kinematic ไม่มีแรงโน้มถ่วง เหมือนวัตถุดิบในสวนเดิม)
    private static void PrepareSprigObject(GameObject sprig)
    {
        for (int c = sprig.transform.childCount - 1; c >= 0; c--)
        {
            Undo.DestroyObjectImmediate(sprig.transform.GetChild(c).gameObject);
        }

        foreach (Collider other in sprig.GetComponents<Collider>().Where(c => !(c is BoxCollider)).ToList())
        {
            Undo.DestroyObjectImmediate(other);
        }

        if (sprig.GetComponent<MeshFilter>() == null) Undo.AddComponent<MeshFilter>(sprig);
        if (sprig.GetComponent<MeshRenderer>() == null) Undo.AddComponent<MeshRenderer>(sprig);
        if (sprig.GetComponent<BoxCollider>() == null) Undo.AddComponent<BoxCollider>(sprig);

        var body = sprig.GetComponent<Rigidbody>();
        if (body != null)
        {
            Undo.RecordObject(body, "ตั้งยอดกะเพราให้อยู่นิ่งบนพุ่ม");
            body.isKinematic = true;
            body.useGravity = false;
        }

        // Collider ลูกถูกลบแล้ว ล้างรายการเดิมใน XRGrabInteractable ให้ใช้ Collider ตัวแม่
        var grab = sprig.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grab != null)
        {
            var grabData = new SerializedObject(grab);
            SerializedProperty list = grabData.FindProperty("m_Colliders");
            if (list != null)
            {
                list.ClearArray();
                grabData.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        var id = new SerializedObject(sprig.GetComponent<IngredientIdentity>());
        id.FindProperty("instanceId").stringValue = string.Empty;
        id.ApplyModifiedPropertiesWithoutUndo();
    }

    // ยอดกะเพรา: จุดกำเนิดของ Mesh อยู่ที่โคนก้าน · ปักที่ช่องบนพุ่ม ชี้ออกและขึ้น
    private static void PlaceSprig(IngredientIdentity sprig, Vector3 basePos, Quaternion rotation, Mesh mesh, Material[] materials)
    {
        Transform t = sprig.transform;
        Undo.RecordObject(t, "ปักยอดกะเพรา");
        Vector3 ps = t.parent != null ? t.parent.lossyScale : Vector3.one;
        t.localScale = new Vector3(SprigScale / ps.x, SprigScale / ps.y, SprigScale / ps.z);
        t.SetPositionAndRotation(basePos, rotation);

        var filter = sprig.GetComponent<MeshFilter>();
        var renderer = sprig.GetComponent<MeshRenderer>();
        if (filter != null) { Undo.RecordObject(filter, "เปลี่ยนเป็นยอดกะเพรา"); filter.sharedMesh = mesh; }
        if (renderer != null) { Undo.RecordObject(renderer, "เปลี่ยนเป็นยอดกะเพรา"); renderer.sharedMaterials = materials; }

        var box = sprig.GetComponent<BoxCollider>();
        if (box != null)
        {
            Undo.RecordObject(box, "ปรับ Collider ยอดกะเพรา");
            Bounds b = mesh.bounds;
            float min = MinGrabSize / SprigScale;
            box.center = b.center;
            box.size = new Vector3(Mathf.Max(b.size.x, min), Mathf.Max(b.size.y, min), Mathf.Max(b.size.z, min));
        }

        EditorUtility.SetDirty(sprig.gameObject);
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
        if (isNew) AssetDatabase.CreateAsset(mesh, path);
        else EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    private static Material SaveMaterial(SubData sub, Material template)
    {
        string path = $"{ModelFolder}/M1_Krapow_{sub.name}.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = material == null;
        if (isNew)
        {
            material = new Material(template);
        }

        material.name = $"M1_Krapow_{sub.name}";
        var color = new Color(sub.color[0], sub.color[1], sub.color[2]);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", null);
        if (isNew) AssetDatabase.CreateAsset(material, path);
        else EditorUtility.SetDirty(material);
        return material;
    }
}
