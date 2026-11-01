using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// สร้างของสำหรับขั้นปรุงใน GameplayTest_M1 ด้วยคลิกเดียว
// - ขอบกระทะ 4 ด้าน (กันวัตถุดิบไหลตกกระทะ)
// - ขวดเครื่องปรุง 4 ขวด บนชั้นเครื่องปรุง (SeasoningShelf) ตามตำแหน่ง Marker เดิม
// - ตะหลิวที่จับได้ วางข้างกระทะ
// - ป้ายบอกสีขวด และอัปเดตข้อความป้ายเตา
// - อัปเดตตาราง Mission Steps เป็นขั้นปรุง 3 ขั้น
// - ซ่อนขวดตกแต่งเดิมใน 4 ช่องที่ใช้ (ไม่ลบ) เพื่อไม่ให้ Collider ซ้อนกัน
// ของใหม่อยู่ใต้ CookingStations/StoveStation/PanCooking · รันซ้ำได้ (ลบของที่สร้างไว้เดิมแล้วสร้างใหม่)
public static class M1PanSetupEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string RootName = "PanCooking";
    private const string StationFolder = "Assets/Gameplay/Prefabs/Stations";
    private const string FeedbackPrefabPath = "Assets/Gameplay/Prefabs/UI/CollectionFeedbackUI.prefab";
    private const float LabelScale = 0.0015f;

    private static readonly (string id, string nameThai, string marker, string colorThai, Color color)[] Bottles =
    {
        (ObjectiveManager.CookingOilIngredientId, "น้ำมัน", "SeasoningSauce", "เหลือง", new Color(0.95f, 0.80f, 0.20f)),
        (ObjectiveManager.OysterSauceIngredientId, "ซอสหอยนางรม", "OysterSauce", "น้ำตาลเข้ม", new Color(0.35f, 0.20f, 0.10f)),
        (ObjectiveManager.FishSauceIngredientId, "น้ำปลา", "FishSauce", "ส้ม", new Color(0.90f, 0.50f, 0.12f)),
        (ObjectiveManager.SugarIngredientId, "น้ำตาล", "LightSoySauce", "ขาว", new Color(0.97f, 0.97f, 0.95f))
    };

    [MenuItem("Tools/Cookverse Thai/M1/Setup Pan Cooking (Bottles + Spatula)")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (EditorApplication.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");
        }

        ObjectiveManager objective = FindComponent<ObjectiveManager>(scene);
        PanContents pan = FindComponent<PanContents>(scene);
        Transform panBase = FindInScene(scene, "Pan_Base");
        Transform shelf = FindInScene(scene, "SeasoningShelf");
        Transform gameplayUI = FindInScene(scene, "GameplayUI");
        GameObject feedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FeedbackPrefabPath);

        if (objective == null || pan == null || panBase == null || shelf == null ||
            gameplayUI == null || feedbackPrefab == null)
        {
            Debug.LogError(
                "[ตั้งค่าการปรุง] ไม่พบ ObjectiveManager, PanContents, Pan_Base, SeasoningShelf, " +
                "GameplayUI หรือ Prefab ป้าย ให้รัน Setup Cooking Stations ก่อน จึงยกเลิก");
            return;
        }

        // ตารางขั้นภารกิจล่าสุด: ... → ผัดเครื่องหอม → ไก่จนสุก → ปรุงรส+กะเพรา
        Undo.RecordObject(objective, "อัปเดตตารางขั้นภารกิจ (ขั้นปรุง)");
        objective.ApplyDefaultSteps();
        EditorUtility.SetDirty(objective);

        Transform station = pan.transform.parent;

        // รันซ้ำ: ลบของที่เครื่องมือนี้สร้างไว้เดิม แล้วสร้างใหม่ทั้งหมด
        Transform previous = station.Find(RootName);
        if (previous != null)
        {
            Undo.DestroyObjectImmediate(previous.gameObject);
        }

        Transform previousLabel = gameplayUI.Find("SeasoningShelfUI");
        if (previousLabel != null)
        {
            Undo.DestroyObjectImmediate(previousLabel.gameObject);
        }

        HideDecorativeBottles(shelf);

        Material template = panBase.GetComponent<Renderer>().sharedMaterial;
        Material dark = template;
        Material wood = AssetDatabase.LoadAssetAtPath<Material>($"{StationFolder}/M1_Wood.mat") ?? template;
        Material metal = AssetDatabase.LoadAssetAtPath<Material>($"{StationFolder}/M1_Metal.mat") ?? template;

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "สร้างของสำหรับขั้นปรุง");
        root.transform.SetParent(station, false);
        root.transform.position = Vector3.zero;

        Bounds panBounds = panBase.GetComponent<Renderer>().bounds;
        BuildPanRim(root.transform, panBounds, dark);
        Physics.SyncTransforms();

        foreach (var spec in Bottles)
        {
            BuildBottle(root.transform, spec.id, spec.nameThai, spec.marker, spec.color, shelf, pan, template);
        }

        BuildSpatula(root.transform, panBounds, pan, wood, metal);

        string legend = "ชั้นเครื่องปรุง: " + string.Join(" · ",
            Bottles.Select(b => $"{b.colorThai}={b.nameThai}")) +
            "\nถือขวดไปเหนือกระทะ แล้วเอียงคว่ำหรือกดไกเพื่อเท";
        CreateLabel(gameplayUI, feedbackPrefab, "SeasoningShelfUI", shelf.position + Vector3.up * 0.45f, legend);

        UpdateStoveLabel(pan);
        FaceLabels(scene);
        Save(scene);
        Debug.Log(
            "[ตั้งค่าการปรุง] เสร็จแล้ว: ขอบกระทะ · ขวดเครื่องปรุง 4 ขวด · ตะหลิว · ป้ายชั้นเครื่องปรุง " +
            "และตารางขั้นภารกิจมีขั้นปรุง 3 ขั้นแล้ว");
    }

    // ป้ายข้อความหันตามกล้องเฉพาะตอนเล่น ตอนแก้ฉากจึงหันตามค่าที่บันทึกไว้
    // เมนูนี้หันป้ายทุกอัน (ยกเว้นป้ายภารกิจของ M5) ให้อ่านถูกด้านจากจุดเริ่มของผู้เล่น
    [MenuItem("Tools/Cookverse Thai/M1/Face Station Labels To Player")]
    public static void FaceLabelsMenu()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (EditorApplication.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");
        }

        FaceLabels(scene);
        Save(scene);
    }

    private static void FaceLabels(Scene scene)
    {
        Camera camera = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Camera>(true))
            .FirstOrDefault(c => c.CompareTag("MainCamera"));

        if (camera == null)
        {
            Debug.LogWarning("[ตั้งค่าการปรุง] ไม่พบกล้อง MainCamera จึงไม่หันป้าย");
            return;
        }

        int count = 0;

        foreach (CollectionFeedbackUI label in scene.GetRootGameObjects()
                     .SelectMany(g => g.GetComponentsInChildren<CollectionFeedbackUI>(true)))
        {
            Vector3 away = label.transform.position - camera.transform.position;
            away.y = 0f;

            if (away.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            // Canvas อ่านถูกด้านเมื่อด้านหน้า (+Z) หันออกจากผู้มอง (เหมือนตอนเล่น)
            Undo.RecordObject(label.transform, "หันป้ายเข้าหาผู้เล่น");
            label.transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
            count++;
        }

        Debug.Log($"[ตั้งค่าการปรุง] หันป้าย {count} ป้ายให้อ่านถูกด้านจากจุดเริ่มของผู้เล่นแล้ว");
    }

    [MenuItem("Tools/Cookverse Thai/M1/Test Cooking Round (Play Mode)")]
    public static void Test()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
        {
            throw new InvalidOperationException("กด Play ใน GameplayTest_M1 ก่อนทดสอบ");
        }

        if (UnityEngine.Object.FindAnyObjectByType<M1CookingSmokeTest>() != null)
        {
            return;
        }

        new GameObject("M1 Cooking Test Runner").AddComponent<M1CookingSmokeTest>();
    }

    // ช่องบนชั้นมีขวดตกแต่ง (BottleBody/BottleNeck/Cap) ที่มี Collider ซ่อนเฉพาะช่องที่วางขวดจริง
    private static void HideDecorativeBottles(Transform shelf)
    {
        foreach (var spec in Bottles)
        {
            Transform marker = shelf.Find(spec.marker);

            if (marker == null)
            {
                continue;
            }

            foreach (Transform child in marker)
            {
                if (child.gameObject.activeSelf)
                {
                    Undo.RecordObject(child.gameObject, "ซ่อนขวดตกแต่งเดิม");
                    child.gameObject.SetActive(false);
                }
            }
        }

        Physics.SyncTransforms();
    }

    private static void BuildPanRim(Transform root, Bounds b, Material material)
    {
        const float height = 0.05f;
        const float thickness = 0.015f;
        float y = b.max.y + height / 2f;
        Vector3 c = b.center;
        float ex = b.extents.x;
        float ez = b.extents.z;

        var rim = CreateGroup(root, "Pan_Rim");
        Block(rim, "Rim_Front", new Vector3(c.x, y, c.z + ez), new Vector3(ex * 2f, height, thickness), material);
        Block(rim, "Rim_Back", new Vector3(c.x, y, c.z - ez), new Vector3(ex * 2f, height, thickness), material);
        Block(rim, "Rim_Left", new Vector3(c.x - ex, y, c.z), new Vector3(thickness, height, ez * 2f), material);
        Block(rim, "Rim_Right", new Vector3(c.x + ex, y, c.z), new Vector3(thickness, height, ez * 2f), material);
    }

    private static void BuildBottle(
        Transform root, string id, string nameThai, string markerName, Color color,
        Transform shelf, PanContents pan, Material template)
    {
        const float bodyHalfHeight = 0.09f;
        Transform marker = shelf.Find(markerName);
        Vector3 basePosition = marker != null ? marker.position : shelf.position;
        // เริ่มยิงจากเหนือ Marker เล็กน้อย เผื่อ Marker จมอยู่ในแผ่นชั้น
        basePosition = PlaceOnSurface(basePosition + Vector3.up * 0.15f, 0.6f) ?? basePosition;

        Material bodyMaterial = GetOrCreateMaterial($"M1_Bottle_{id}", color, template);

        var bottle = new GameObject($"Seasoning_{id}");
        Undo.RegisterCreatedObjectUndo(bottle, "สร้างขวดเครื่องปรุง");
        bottle.transform.SetParent(root, false);
        bottle.transform.SetPositionAndRotation(basePosition + Vector3.up * (bodyHalfHeight + 0.002f), Quaternion.identity);

        Visual(bottle.transform, "Body", PrimitiveType.Cylinder, Vector3.zero,
            new Vector3(0.06f, bodyHalfHeight, 0.06f), bodyMaterial);
        Visual(bottle.transform, "Neck", PrimitiveType.Cylinder, new Vector3(0f, bodyHalfHeight + 0.02f, 0f),
            new Vector3(0.025f, 0.02f, 0.025f), template);

        var spout = new GameObject("Spout").transform;
        spout.SetParent(bottle.transform, false);
        spout.localPosition = new Vector3(0f, bodyHalfHeight + 0.045f, 0f);

        Transform stream = Visual(bottle.transform, "PourStream", PrimitiveType.Cylinder, Vector3.zero,
            new Vector3(0.012f, 0.12f, 0.012f), bodyMaterial).transform;
        stream.gameObject.SetActive(false);

        var box = bottle.AddComponent<BoxCollider>();
        // ก้นกล่องเท่ากับก้นขวด (ไม่จมชั้น) สูงถึงปากขวด
        float colliderTop = bodyHalfHeight + 0.04f;
        box.center = new Vector3(0f, (colliderTop - bodyHalfHeight) / 2f, 0f);
        box.size = new Vector3(0.065f, colliderTop + bodyHalfHeight, 0.065f);

        var body = bottle.AddComponent<Rigidbody>();
        body.mass = 0.4f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var grab = bottle.AddComponent<XRGrabInteractable>();
        grab.useDynamicAttach = true;

        var seasoning = bottle.AddComponent<SeasoningBottle>();
        seasoning.Configure(id, nameThai, pan, spout, stream);
        bottle.AddComponent<GrabbableResetPose>();
        EditorUtility.SetDirty(seasoning);
    }

    private static void BuildSpatula(Transform root, Bounds panBounds, PanContents pan, Material wood, Material metal)
    {
        // หาที่วางบนเคาน์เตอร์ข้างกระทะ: ซ้าย → หน้า → ขวาเลยด้ามกระทะ
        Vector3[] candidates =
        {
            new Vector3(panBounds.min.x - 0.32f, panBounds.max.y, panBounds.center.z),
            new Vector3(panBounds.center.x, panBounds.max.y, panBounds.max.z + 0.12f),
            new Vector3(panBounds.max.x + 0.55f, panBounds.max.y, panBounds.center.z)
        };

        Vector3 position = candidates[0];
        foreach (Vector3 candidate in candidates)
        {
            Vector3? hit = PlaceOnSurface(candidate + Vector3.up * 0.3f, 0.6f);

            if (hit.HasValue && Mathf.Abs(hit.Value.y - panBounds.min.y) < 0.15f)
            {
                position = hit.Value;
                break;
            }
        }

        var spatula = new GameObject("Spatula_Grabbable");
        Undo.RegisterCreatedObjectUndo(spatula, "สร้างตะหลิว");
        spatula.transform.SetParent(root, false);
        // สร้างตามแกน Y แล้วหมุนให้นอนราบ (ด้านบางของใบตะหลิวชี้ลง) ปลายชี้ +X
        // ความหนาแนวตั้งหลังหมุน = ครึ่งหนึ่งของกล่องแกน Z (0.015) จึงยกขึ้น 0.017
        spatula.transform.SetPositionAndRotation(
            position + Vector3.up * 0.017f,
            Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(90f, 0f, 0f));

        Visual(spatula.transform, "Handle", PrimitiveType.Cylinder, Vector3.zero,
            new Vector3(0.025f, 0.13f, 0.025f), wood);
        Visual(spatula.transform, "Blade", PrimitiveType.Cube, new Vector3(0f, 0.19f, 0f),
            new Vector3(0.08f, 0.12f, 0.008f), metal);

        var tip = new GameObject("Tip").transform;
        tip.SetParent(spatula.transform, false);
        tip.localPosition = new Vector3(0f, 0.24f, 0f);

        var box = spatula.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 0.055f, 0f);
        box.size = new Vector3(0.08f, 0.38f, 0.03f);

        var body = spatula.AddComponent<Rigidbody>();
        body.mass = 0.3f;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        var grab = spatula.AddComponent<XRGrabInteractable>();
        grab.useDynamicAttach = true;

        var stirrer = spatula.AddComponent<SpatulaStirrer>();
        stirrer.Configure(pan, tip);
        spatula.AddComponent<GrabbableResetPose>();
        EditorUtility.SetDirty(stirrer);
    }

    private static void UpdateStoveLabel(PanContents pan)
    {
        var panData = new SerializedObject(pan);
        var ui = panData.FindProperty("feedbackUI").objectReferenceValue as CollectionFeedbackUI;

        if (ui == null)
        {
            return;
        }

        var data = new SerializedObject(ui);
        data.FindProperty("initialMessage").stringValue =
            "เตา: เปิดไฟ → น้ำมัน → กระเทียม+พริก → คน → ไก่จนสุก → " +
            "ซอสหอยนางรม น้ำปลา น้ำตาล → ใบกะเพรา → คน → ปิดไฟ";
        data.ApplyModifiedProperties();
    }

    // ยิง Ray ลงจากจุดที่กำหนด คืนจุดบนพื้นผิว (ไม่นับ Trigger)
    private static Vector3? PlaceOnSurface(Vector3 from, float maxDistance)
    {
        Vector3 origin = from + Vector3.up * 0.05f;
        return Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance + 0.05f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            ? hit.point
            : (Vector3?)null;
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        var group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static void Block(Transform parent, string name, Vector3 worldPosition, Vector3 size, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.position = worldPosition;
        block.transform.localScale = size;
        block.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    // ชิ้นส่วนสำหรับมองเห็นอย่างเดียว (ไม่มี Collider) ใช้ Collider รวมที่ตัวแม่
    private static GameObject Visual(
        Transform parent, string name, PrimitiveType shape, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.identity;
        part.transform.localScale = localScale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        return part;
    }

    private static void CreateLabel(Transform parent, GameObject prefab, string name, Vector3 worldPosition, string message)
    {
        var label = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(label, "สร้างป้ายชั้นเครื่องปรุง");
        label.name = name;
        label.transform.position = worldPosition;
        Vector3 parentScale = parent.lossyScale;
        label.transform.localScale = new Vector3(
            LabelScale / parentScale.x, LabelScale / parentScale.y, LabelScale / parentScale.z);

        var ui = label.GetComponent<CollectionFeedbackUI>();
        var data = new SerializedObject(ui);
        data.FindProperty("initialMessage").stringValue = message;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material GetOrCreateMaterial(string name, Color color, Material source)
    {
        string path = $"{StationFolder}/{name}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
        {
            return existing;
        }

        var material = new Material(source) { name = name };

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

    private static void Save(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static T FindComponent<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true))
            .FirstOrDefault();
    }

    private static Transform FindInScene(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name.Trim() == name);
    }
}
