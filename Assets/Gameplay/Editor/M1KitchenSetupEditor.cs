using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// สร้างของสำหรับขั้น collect_kitchen ใน GameplayTest_M1 ด้วยคลิกเดียว
// - Prefab ไก่และกระเทียม (คัดลอกจาก Chili.prefab เพื่อให้ค่าการหยิบเหมือนกัน)
// - ตู้เย็นแบบกลวงพร้อมชั้นวางและประตูที่จับเพื่อเปิดได้
// - ไก่ 1 ชิ้นบนชั้นในตู้เย็น และกระเทียม 3 กลีบแทนกระเทียมตกแต่งในตะกร้า
// รันซ้ำได้: ถ้ามีของอยู่แล้วจะข้ามส่วนนั้น
public static class M1KitchenSetupEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string IngredientFolder = "Assets/Gameplay/Prefabs/Ingredients";
    private const string ChiliPrefabPath = IngredientFolder + "/Chili.prefab";
    private const string ChickenPrefabPath = IngredientFolder + "/Chicken.prefab";
    private const string GarlicPrefabPath = IngredientFolder + "/Garlic.prefab";
    private const string FridgeRootName = "Fridge_Interactive";
    private const int GarlicCount = 3;

    private static readonly Vector3 ChickenSize = new(0.18f, 0.06f, 0.12f);
    private static readonly Vector3 GarlicSize = new(0.07f, 0.07f, 0.07f);
    private static readonly Color ChickenColor = new(0.98f, 0.72f, 0.70f);
    private static readonly Color GarlicColor = new(0.96f, 0.94f, 0.86f);

    [MenuItem("Tools/Cookverse Thai/M1/Setup Kitchen (Fridge + Chicken + Garlic)")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (EditorApplication.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                "เปิด GameplayTest_M1 และหยุด Play ก่อน");
        }

        Transform fridge = FindInScene(scene, "Fridge", "Ingredient_Zone");
        Transform garlicBasket = FindInScene(scene, "GarlicBasket", null);
        Transform gameplayObjects = FindInScene(scene, "GameplayObjects", null);

        if (fridge == null || garlicBasket == null || gameplayObjects == null)
        {
            Debug.LogError(
                "[ตั้งค่าครัว] ไม่พบ Fridge, GarlicBasket หรือ GameplayObjects " +
                "ใน GameplayTest_M1 จึงยกเลิก");
            return;
        }

        GameObject chickenPrefab = CreateIngredientPrefab(
            ChickenPrefabPath, "Chicken",
            ObjectiveManager.ChickenIngredientId, "เนื้อไก่",
            ChickenSize, ChickenColor, "M1_Chicken_Raw");
        GameObject garlicPrefab = CreateIngredientPrefab(
            GarlicPrefabPath, "Garlic",
            ObjectiveManager.GarlicIngredientId, "กระเทียม",
            GarlicSize, GarlicColor, "M1_Garlic");

        if (chickenPrefab == null || garlicPrefab == null)
        {
            return;
        }

        float shelfTop = BuildFridge(fridge, out Bounds fridgeBounds);
        PlaceChicken(scene, chickenPrefab, gameplayObjects, fridgeBounds, shelfTop);
        PlaceGarlic(scene, garlicPrefab, gameplayObjects, garlicBasket);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            "[ตั้งค่าครัว] เสร็จแล้ว: ตู้เย็นเปิดได้ ไก่ 1 ชิ้นอยู่บนชั้นในตู้เย็น " +
            "กระเทียม 3 กลีบอยู่ในตะกร้า นำไปวางที่โต๊ะเตรียมเพื่อผ่านขั้น collect_kitchen");
    }

    private static GameObject CreateIngredientPrefab(
        string path,
        string name,
        string ingredientId,
        string displayNameThai,
        Vector3 size,
        Color color,
        string materialName)
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (existing != null)
        {
            return existing;
        }

        if (!AssetDatabase.CopyAsset(ChiliPrefabPath, path))
        {
            Debug.LogError($"[ตั้งค่าครัว] คัดลอก {ChiliPrefabPath} ไป {path} ไม่สำเร็จ");
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            root.name = name;
            root.transform.localScale = size;

            // อยู่นิ่งจนกว่าจะถูกหยิบ ไม่ให้ชนชั้นวางหรือตะกร้าแล้วกระเด็น
            root.GetComponent<Rigidbody>().isKinematic = true;

            // ไก่และกระเทียมเป็นกล่องเดียว ไม่เอาชิ้นลูกของพริก (เช่นขั้ว) มาด้วย
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            var identity = new SerializedObject(root.GetComponent<IngredientIdentity>());
            identity.FindProperty("ingredientId").stringValue = ingredientId;
            identity.FindProperty("legacyPrefabName").stringValue = name;
            identity.FindProperty("displayNameThai").stringValue = displayNameThai;
            identity.FindProperty("instanceId").stringValue = string.Empty;
            identity.FindProperty("currentState").enumValueIndex =
                (int)IngredientState.Raw;
            identity.ApplyModifiedPropertiesWithoutUndo();

            MeshRenderer renderer = root.GetComponent<MeshRenderer>();

            if (renderer != null && renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial = CreateColorMaterial(
                    renderer.sharedMaterial, color, materialName);
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"[ตั้งค่าครัว] สร้าง {path} แล้ว");
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static Material CreateColorMaterial(
        Material source,
        Color color,
        string materialName)
    {
        string path = $"{IngredientFolder}/{materialName}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
        {
            return existing;
        }

        var material = new Material(source) { name = materialName };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // คืนค่าความสูงผิวบนของชั้นวางในตู้เย็น
    private static float BuildFridge(Transform fridge, out Bounds bounds)
    {
        Renderer fridgeRenderer = fridge.GetComponent<Renderer>();
        Collider fridgeCollider = fridge.GetComponent<Collider>();
        Transform existingRoot = fridge.parent.Find(FridgeRootName);

        bounds = fridgeRenderer != null
            ? fridgeRenderer.bounds
            : new Bounds(fridge.position, fridge.lossyScale);

        // ชั้นวางวางที่ระดับ RefrigeratedIngredients ที่จัดไว้ในฉาก
        Transform shelfMarker = fridge.Find("RefrigeratedIngredients");
        float shelfCenterY = shelfMarker != null
            ? shelfMarker.position.y - 0.05f
            : bounds.min.y + bounds.size.y / 3f;
        const float shelfThickness = 0.03f;
        float shelfTop = shelfCenterY + shelfThickness / 2f;

        if (existingRoot != null)
        {
            Debug.Log("[ตั้งค่าครัว] มีตู้เย็นที่เปิดได้อยู่แล้ว จึงข้ามการสร้าง");
            return shelfTop;
        }

        Material material = fridgeRenderer != null ? fridgeRenderer.sharedMaterial : null;
        Vector3 c = bounds.center;
        Vector3 s = bounds.size;
        const float wall = 0.04f;

        var root = new GameObject(FridgeRootName);
        Undo.RegisterCreatedObjectUndo(root, "สร้างตู้เย็นที่เปิดได้");
        root.transform.SetParent(fridge.parent, false);
        root.transform.position = c;
        root.transform.rotation = Quaternion.identity;

        // ด้านหน้าตู้เย็นหันไปทาง +Z (เข้าหากลางบ้าน)
        CreatePanel(root.transform, "Fridge_Back",
            new Vector3(c.x, c.y, bounds.min.z + wall / 2f), new Vector3(s.x, s.y, wall), material);
        CreatePanel(root.transform, "Fridge_Left",
            new Vector3(bounds.min.x + wall / 2f, c.y, c.z), new Vector3(wall, s.y, s.z), material);
        CreatePanel(root.transform, "Fridge_Right",
            new Vector3(bounds.max.x - wall / 2f, c.y, c.z), new Vector3(wall, s.y, s.z), material);
        CreatePanel(root.transform, "Fridge_Top",
            new Vector3(c.x, bounds.max.y - wall / 2f, c.z), new Vector3(s.x, wall, s.z), material);
        CreatePanel(root.transform, "Fridge_Bottom",
            new Vector3(c.x, bounds.min.y + wall / 2f, c.z), new Vector3(s.x, wall, s.z), material);
        CreatePanel(root.transform, "Fridge_Shelf",
            new Vector3(c.x, shelfCenterY, c.z), new Vector3(s.x - 2f * wall, shelfThickness, s.z - wall), material);

        // บานพับอยู่ขอบซ้าย (min X) ประตูยื่นไปทาง +X
        var hinge = new GameObject("Fridge_Door_Hinge");
        hinge.transform.SetParent(root.transform, false);
        hinge.transform.position = new Vector3(bounds.min.x, c.y, bounds.max.z + wall / 2f);

        GameObject door = CreatePanel(hinge.transform, "Fridge_Door",
            Vector3.zero, new Vector3(s.x, s.y, wall), material);
        door.transform.localPosition = new Vector3(s.x / 2f, 0f, 0f);

        var interactable = door.AddComponent<XRSimpleInteractable>();
        var fridgeDoor = hinge.AddComponent<FridgeDoor>();
        var doorData = new SerializedObject(fridgeDoor);
        doorData.FindProperty("doorInteractable").objectReferenceValue = interactable;
        doorData.ApplyModifiedPropertiesWithoutUndo();

        // ซ่อนกล่องตู้เย็นเดิม (ไม่ลบ เผื่อย้อนกลับ)
        if (fridgeRenderer != null)
        {
            Undo.RecordObject(fridgeRenderer, "ซ่อนตู้เย็นเดิม");
            fridgeRenderer.enabled = false;
        }

        if (fridgeCollider != null)
        {
            Undo.RecordObject(fridgeCollider, "ปิด Collider ตู้เย็นเดิม");
            fridgeCollider.enabled = false;
        }

        Debug.Log("[ตั้งค่าครัว] สร้างตู้เย็นที่เปิดได้แล้ว (จับที่ประตูเพื่อเปิด/ปิด)");
        return shelfTop;
    }

    private static GameObject CreatePanel(
        Transform parent,
        string name,
        Vector3 worldPosition,
        Vector3 size,
        Material material)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = name;
        panel.transform.SetParent(parent, false);
        panel.transform.position = worldPosition;
        panel.transform.localScale = size;

        if (material != null)
        {
            panel.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        return panel;
    }

    private static void PlaceChicken(
        Scene scene,
        GameObject prefab,
        Transform parent,
        Bounds fridgeBounds,
        float shelfTop)
    {
        if (CountIngredients(scene, ObjectiveManager.ChickenIngredientId) > 0)
        {
            Debug.Log("[ตั้งค่าครัว] มีไก่ในฉากแล้ว จึงข้าม");
            return;
        }

        Vector3 position = new(
            fridgeBounds.center.x,
            shelfTop + ChickenSize.y / 2f + 0.005f,
            fridgeBounds.center.z + 0.05f);

        CreateInstance(prefab, parent, "Chicken_Collectible", position, ChickenSize);
    }

    private static void PlaceGarlic(
        Scene scene,
        GameObject prefab,
        Transform parent,
        Transform garlicBasket)
    {
        int existing = CountIngredients(scene, ObjectiveManager.GarlicIngredientId);

        if (existing >= GarlicCount)
        {
            Debug.Log("[ตั้งค่าครัว] มีกระเทียมครบในฉากแล้ว จึงข้าม");
            return;
        }

        Transform[] decorative = garlicBasket
            .Cast<Transform>()
            .Where(t => t.name.Trim().StartsWith("Garlic_", StringComparison.Ordinal))
            .OrderBy(t => t.name)
            .ToArray();

        for (int n = existing; n < GarlicCount; n++)
        {
            Vector3 position = n < decorative.Length
                ? decorative[n].position
                : garlicBasket.position + new Vector3(0.1f * n, 0.06f, 0f);

            CreateInstance(prefab, parent, "Garlic_Collectible_" + (n + 1), position, GarlicSize);

            if (n < decorative.Length && decorative[n].gameObject.activeSelf)
            {
                Undo.RecordObject(decorative[n].gameObject, "ซ่อนกระเทียมตกแต่ง");
                decorative[n].gameObject.SetActive(false);
            }
        }
    }

    private static void CreateInstance(
        GameObject prefab,
        Transform parent,
        string name,
        Vector3 worldPosition,
        Vector3 worldSize)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, "เพิ่ม " + name);
        instance.name = name;
        instance.transform.position = worldPosition;
        instance.transform.rotation = Quaternion.identity;

        Vector3 parentScale = parent.lossyScale;
        instance.transform.localScale = new Vector3(
            worldSize.x / parentScale.x,
            worldSize.y / parentScale.y,
            worldSize.z / parentScale.z);

        // แต่ละชิ้นต้องมี Instance ID ของตัวเอง ระบบจะสร้างใหม่ตอนเริ่มเล่น
        var identity = new SerializedObject(instance.GetComponent<IngredientIdentity>());
        identity.FindProperty("instanceId").stringValue = string.Empty;
        identity.ApplyModifiedPropertiesWithoutUndo();
    }

    private static int CountIngredients(Scene scene, string ingredientId)
    {
        return scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<IngredientIdentity>(true))
            .Count(i => i.IngredientId == ingredientId);
    }

    private static Transform FindInScene(Scene scene, string name, string parentName)
    {
        return scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t =>
                t.name.Trim() == name &&
                (parentName == null ||
                 (t.parent != null && t.parent.name.Trim() == parentName)));
    }
}
