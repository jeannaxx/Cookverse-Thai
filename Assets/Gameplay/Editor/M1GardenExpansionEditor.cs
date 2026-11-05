using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ขยายสวนให้ครบผัก 19 ชนิด (ดู Docs/Planning/Gameplay/MENU_LIST_AND_GARDEN.md)
// - พื้นสวนต่อทางขวา (+X) GardenBase_Ext กว้างพอ 2 คอลัมน์แปลง ระดับเดียวกับ GardenBase (เทเลพอร์ตได้เหมือนเดิม)
// - แปลงยกพื้นใหม่ 4 แปลง (จุดยึด + RaisedBed_<ชื่อ>): Galangal ข่า · Shallot หอมแดง ·
//   BasilMint โหระพา+สะระแหน่ · EggplantChives มะเขือเปราะ+มะเขือพวง+กุยช่าย
// - กระบะค้าง TrellisPlanter ริมสวนด้านหลัง (+Z): ถั่วฝักยาว + มะเขือเทศ
// - ต้นไม้ริมสวนด้านหลัง: Tree_KaffirLime มะกรูด · Tree_Papaya มะละกอ · Tree_Mango มะม่วง
// - ทุกอย่างอยู่ใน Garden_Blockout/GardenExtension แล้วเรียก Setup Garden Veggies ปลูกผักทั้งสวน
// ต้องรัน Setup Garden Beds (Raised) มาก่อน (ใช้ Mesh/วัสดุแปลงชุดเดียวกัน) · รันซ้ำได้: ลบ GardenExtension เดิมแล้วสร้างใหม่
public static class M1GardenExpansionEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string ModelFolder = "Assets/Gameplay/Art/Models";
    private const string BedDataPath = ModelFolder + "/GardenBedModels.json";
    private const float ColumnGap = 1.5f;   // เท่าระยะคอลัมน์แปลงเดิม (8.05 → 9.5 → 11.0)
    private const float EdgeMargin = 0.38f; // ขอบพื้นเลยแปลงออกไป

    [Serializable] private class SubData { public string name; public float[] color; public int[] triangles; }
    [Serializable] private class MeshData { public float[] vertices; public SubData[] submeshes; }
    [Serializable] private class BedFile { public MeshData planter; public float soilBase; }

    [MenuItem("Tools/Cookverse Thai/M1/Setup Garden Expansion (All Veggies)")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");
        }

        Material[] bedMaterials = new[] { "Soil", "Plank", "Post" }
            .Select(n => AssetDatabase.LoadAssetAtPath<Material>($"{ModelFolder}/M1_GardenBed_{n}.mat")).ToArray();
        Mesh ridge2 = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelFolder}/M1_GardenBed_ridge2.asset");
        Mesh ridge3 = AssetDatabase.LoadAssetAtPath<Mesh>($"{ModelFolder}/M1_GardenBed_ridge3.asset");
        var json = AssetDatabase.LoadAssetAtPath<TextAsset>(BedDataPath);
        if (bedMaterials.Any(m => m == null) || ridge2 == null || ridge3 == null || json == null)
        {
            Debug.LogError("[ขยายสวน] ยังไม่มี Mesh/วัสดุแปลงยกพื้น · รัน Setup Garden Beds (Raised) ก่อน");
            return;
        }

        BedFile bedFile = JsonUtility.FromJson<BedFile>(json.text);
        if (bedFile.planter == null || bedFile.planter.vertices == null || bedFile.planter.vertices.Length == 0)
        {
            Debug.LogError("[ขยายสวน] GardenBedModels.json ไม่มี planter · รัน python3 Tools/ModelGen/garden_bed_models.py ใหม่");
            return;
        }

        if (!M1VeggieSetupEditor.FindGarden(scene, out Transform garden, out Transform gardenBase, "[ขยายสวน]")) return;
        Transform krapao = gardenBase != null ? gardenBase.Find("Krapao") : null;
        Transform chili = gardenBase != null ? gardenBase.Find("Chili") : null;
        Renderer baseRenderer = gardenBase != null ? gardenBase.GetComponent<Renderer>() : null;
        if (baseRenderer == null || krapao == null || chili == null)
        {
            Debug.LogError($"[ขยายสวน] ข้อมูลสวนไม่ครบ จึงยกเลิก · GardenBase={gardenBase.name} (Renderer {(baseRenderer != null ? "มี" : "ไม่มี")}) · " +
                $"Krapao {(krapao != null ? "มี" : "ไม่มี")} · Chili {(chili != null ? "มี" : "ไม่มี")} · ลูกของ GardenBase: " +
                string.Join(", ", gardenBase.Cast<Transform>().Select(t => t.name)));
            return;
        }

        Mesh planterMesh = SaveMesh("M1_GardenBed_planter", bedFile.planter);

        Transform old = garden.Find(M1VeggieSetupEditor.ExtensionName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var root = new GameObject(M1VeggieSetupEditor.ExtensionName);
        Undo.RegisterCreatedObjectUndo(root, "ขยายสวน");
        root.transform.SetParent(garden, false);
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        Bounds gb = baseRenderer.bounds;
        float groundY = gb.max.y;
        float col1 = gb.max.x + 0.32f;              // คอลัมน์ใหม่ห่างคอลัมน์ขวาสุดเดิมเท่ากัน
        float col2 = col1 + ColumnGap;
        float backZ = krapao.position.z;            // แถวหลัง (กะเพรา)
        float frontZ = chili.position.z;            // แถวหน้า (พริก)

        // พื้นสวนส่วนต่อ
        float padWidth = col2 + 0.4f + EdgeMargin - gb.max.x;
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "GardenBase_Ext";
        pad.transform.SetParent(root.transform, false);
        pad.transform.position = new Vector3(gb.max.x + padWidth / 2f, gb.center.y, gb.center.z);
        pad.transform.localScale = new Vector3(padWidth, gb.size.y, gb.size.z);
        pad.GetComponent<Renderer>().sharedMaterial = baseRenderer.sharedMaterial;
        pad.isStatic = true;
        // ให้เทเลพอร์ตบนพื้นส่วนต่อได้เหมือน GardenBase: คัดลอกคอมโพเนนต์ TeleportationArea (ไม่อ้างชนิดตรง ๆ)
        Component teleport = gardenBase.GetComponents<Component>().FirstOrDefault(c => c != null && c.GetType().Name == "TeleportationArea");
        if (teleport != null && UnityEditorInternal.ComponentUtility.CopyComponent(teleport))
        {
            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(pad);
        }

        // แปลงยกพื้นใหม่ 4 แปลง
        MakeBed(root.transform, "Galangal", new Vector3(col1, groundY, backZ), ridge2, bedMaterials);
        MakeBed(root.transform, "Shallot", new Vector3(col1, groundY, frontZ), ridge3, bedMaterials);
        MakeBed(root.transform, "BasilMint", new Vector3(col2, groundY, backZ), ridge2, bedMaterials);
        MakeBed(root.transform, "EggplantChives", new Vector3(col2, groundY, frontZ), ridge3, bedMaterials);

        Physics.SyncTransforms();

        // ริมสวนด้านหลัง: กระบะค้าง (ยาวตามแกน X) + ต้นไม้ 3 ต้น วางบนพื้นดินจริง
        float rimZ = gb.max.z + 0.45f;
        Vector3 planterPos = Ground(new Vector3(gb.center.x, 0f, rimZ));
        GameObject planter = MakeBed(root.transform, "TrellisPlanter", planterPos, planterMesh, bedMaterials);
        planter.transform.parent.rotation = Quaternion.Euler(0f, 90f, 0f);   // หมุนจุดยึด ให้แกนยาวกระบะ (Z) ขนานขอบสวน (X)

        MakeAnchor(root.transform, "Tree_KaffirLime", Ground(new Vector3(gb.min.x + 0.2f, 0f, gb.max.z + 0.6f)));
        MakeAnchor(root.transform, "Tree_Papaya", Ground(new Vector3(gb.max.x - 0.55f, 0f, gb.max.z + 0.6f)));
        MakeAnchor(root.transform, "Tree_Mango", Ground(new Vector3(col2 - 0.1f, 0f, gb.max.z + 1.3f)));

        Physics.SyncTransforms();
        Debug.Log($"[ขยายสวน] พื้นต่อกว้าง {padWidth:0.00} ม. · แปลงใหม่ 4 แปลง (x={col1:0.00}, {col2:0.00}) · กระบะค้าง + ต้นไม้ 3 ต้นที่ z≈{rimZ:0.00}");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        M1VeggieSetupEditor.Setup();
    }

    // จุดยึดชื่อเดียวกับแผนใน M1VeggieSetupEditor + แปลงยกพื้นเป็นลูก (มี MeshCollider ให้ Ray ลงดิน)
    private static GameObject MakeBed(Transform root, string name, Vector3 position, Mesh mesh, Material[] materials)
    {
        Transform anchor = MakeAnchor(root, name, position);
        var bed = new GameObject($"RaisedBed_{name}");
        bed.transform.SetParent(anchor, false);
        bed.AddComponent<MeshFilter>().sharedMesh = mesh;
        bed.AddComponent<MeshRenderer>().sharedMaterials = materials;
        bed.AddComponent<MeshCollider>().sharedMesh = mesh;
        bed.isStatic = true;
        return bed;
    }

    private static Transform MakeAnchor(Transform root, string name, Vector3 position)
    {
        var anchor = new GameObject(name);
        anchor.transform.SetParent(root, false);
        anchor.transform.SetPositionAndRotation(position, Quaternion.identity);
        return anchor.transform;
    }

    private static Vector3 Ground(Vector3 p)
    {
        float y = Physics.Raycast(new Vector3(p.x, 5f, p.z), Vector3.down, out RaycastHit hit, 10f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point.y : 0.05f;
        return new Vector3(p.x, y, p.z);
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
}
