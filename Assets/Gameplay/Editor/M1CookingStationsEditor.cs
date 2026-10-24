using System;
using System.Linq;
using Unity.VRTemplate;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// สร้างจุดทำงานในครัวของ GameplayTest_M1 ด้วยคลิกเดียว
// - อ่างล้าง: ก๊อก + สายน้ำ + โซนล้าง บน VegetableWashPoint
// - เขียง + มีด: บน MeatPrepPoint ข้างโต๊ะรับวัตถุดิบ
// - เตาแก๊ส: ปุ่มบิด 4 ตำแหน่ง (XRKnob) + เปลวไฟ + กระทะ + โซนในกระทะ
// - ป้ายข้อความภาษาไทยเหนือแต่ละจุด
// - อัปเดตตาราง Mission Steps ให้มีขั้นล้าง หั่น และผัด
// ของเดิมในฉากไม่ถูกแก้ ของใหม่อยู่ใต้ GameplaySystems/GameplayObjects/CookingStations
public static class M1CookingStationsEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string RootName = "CookingStations";
    private const string StationFolder = "Assets/Gameplay/Prefabs/Stations";
    private const string FeedbackPrefabPath = "Assets/Gameplay/Prefabs/UI/CollectionFeedbackUI.prefab";
    private const string ChiliPrefabPath = "Assets/Gameplay/Prefabs/Ingredients/Chili.prefab";
    private const float LabelScale = 0.0015f;

    [MenuItem("Tools/Cookverse Thai/M1/Setup Cooking Stations (Wash + Cut + Stove)")]
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (EditorApplication.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");
        }

        ObjectiveManager objective = FindComponent<ObjectiveManager>(scene);
        Transform gameplayObjects = FindInScene(scene, "GameplayObjects", null);
        Transform gameplayUI = FindInScene(scene, "GameplayUI", null);
        Transform washPoint = FindInScene(scene, "VegetableWashPoint", "Washing_Zone");
        Transform prepPoint = FindInScene(scene, "MeatPrepPoint", "Preparation_Zone");
        Transform stove = FindInScene(scene, "GasStove", "Cooking_Zone");
        GameObject feedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FeedbackPrefabPath);
        GameObject chiliTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(ChiliPrefabPath);

        if (objective == null || gameplayObjects == null || gameplayUI == null ||
            washPoint == null || prepPoint == null || stove == null ||
            feedbackPrefab == null || chiliTemplate == null)
        {
            Debug.LogError(
                "[ตั้งค่าจุดทำงาน] ไม่พบ ObjectiveManager, GameplayObjects, GameplayUI, " +
                "VegetableWashPoint, MeatPrepPoint, GasStove หรือ Prefab ที่ต้องใช้ จึงยกเลิก");
            return;
        }

        // ตารางขั้นภารกิจล่าสุด: เก็บสวน → เก็บครัว → ล้าง → หั่น → ผัด
        Undo.RecordObject(objective, "อัปเดตตารางขั้นภารกิจ");
        objective.ApplyDefaultSteps();
        EditorUtility.SetDirty(objective);

        if (gameplayObjects.Find(RootName) != null)
        {
            Debug.Log("[ตั้งค่าจุดทำงาน] มี CookingStations อยู่แล้ว อัปเดตเฉพาะตารางขั้นภารกิจ");
            Save(scene);
            return;
        }

        Material source = chiliTemplate.GetComponent<MeshRenderer>().sharedMaterial;
        var mats = new Materials
        {
            Wood = GetOrCreateMaterial("M1_Wood", new Color(0.55f, 0.38f, 0.22f), source),
            Metal = GetOrCreateMaterial("M1_Metal", new Color(0.62f, 0.64f, 0.67f), source),
            Water = GetOrCreateMaterial("M1_Water", new Color(0.30f, 0.62f, 0.95f), source),
            Flame = GetOrCreateMaterial("M1_Flame", new Color(1.00f, 0.45f, 0.10f), source),
            Dark = GetOrCreateMaterial("M1_PanDark", new Color(0.15f, 0.15f, 0.16f), source),
            White = GetOrCreateMaterial("M1_KnobMark", new Color(0.95f, 0.95f, 0.95f), source)
        };

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "สร้างจุดทำงานในครัว");
        root.transform.SetParent(gameplayObjects, false);
        root.transform.position = Vector3.zero;

        BuildWashStation(root.transform, gameplayUI, washPoint, objective, feedbackPrefab, mats);
        BuildCutStation(root.transform, gameplayUI, prepPoint, objective, feedbackPrefab, chiliTemplate, mats);
        BuildStove(root.transform, gameplayUI, stove, objective, feedbackPrefab, mats);

        Save(scene);
        Debug.Log(
            "[ตั้งค่าจุดทำงาน] เสร็จแล้ว: อ่างล้าง · เขียง + มีด · เตาแก๊สปุ่มบิด + กระทะ " +
            "และตารางขั้นภารกิจมีขั้นล้าง หั่น ผัดแล้ว");
    }

    private struct Materials
    {
        public Material Wood, Metal, Water, Flame, Dark, White;
    }

    private static void BuildWashStation(
        Transform root, Transform uiParent, Transform washPoint,
        ObjectiveManager objective, GameObject feedbackPrefab, Materials mats)
    {
        Bounds b = WorldBounds(washPoint);
        float top = b.max.y;
        var group = CreateGroup(root, "WashStation");

        // ก๊อกน้ำ: ท่อตั้งที่ขอบหลังอ่าง + ปากก๊อกยื่นมาข้างหน้า
        Block(group, "Faucet_Pipe", new Vector3(b.center.x, top + 0.18f, b.min.z + 0.03f),
            new Vector3(0.03f, 0.36f, 0.03f), mats.Metal, false);
        Block(group, "Faucet_Spout", new Vector3(b.center.x, top + 0.35f, b.min.z + 0.10f),
            new Vector3(0.03f, 0.03f, 0.16f), mats.Metal, false);
        GameObject water = Block(group, "Water_Stream",
            new Vector3(b.center.x, top + 0.17f, b.min.z + 0.17f),
            new Vector3(0.02f, 0.33f, 0.02f), mats.Water, false);
        water.SetActive(false);

        CollectionFeedbackUI ui = CreateLabel(uiParent, feedbackPrefab, "WashStationUI",
            new Vector3(b.center.x, top + 0.65f, b.center.z + 0.1f),
            "อ่างล้าง: ถือผักไว้ใต้ก๊อก 2 วินาที");

        StationZone zone = CreateZone<StationZone>(group, "WashStation_Zone",
            new Vector3(b.center.x, top + 0.15f, b.center.z),
            new Vector3(b.size.x, 0.30f, b.size.z));
        zone.Configure(objective, ui, StationAction.Wash, water);
    }

    private static void BuildCutStation(
        Transform root, Transform uiParent, Transform prepPoint,
        ObjectiveManager objective, GameObject feedbackPrefab, GameObject grabTemplate, Materials mats)
    {
        Bounds b = WorldBounds(prepPoint);
        float top = b.max.y;
        var group = CreateGroup(root, "CutStation");

        Vector3 boardSize = new(Mathf.Min(0.42f, b.size.x - 0.05f), 0.025f, 0.28f);
        Vector3 boardCenter = new(b.center.x, top + boardSize.y / 2f, b.center.z - 0.05f);
        Block(group, "CuttingBoard_Station", boardCenter, boardSize, mats.Wood, true);

        // มีด: ด้ามไม้ + ใบมีดโลหะ หยิบได้ วางข้างเขียง
        var knife = new GameObject("Kitchen_Knife");
        Undo.RegisterCreatedObjectUndo(knife, "สร้างมีด");
        knife.transform.SetParent(group, false);
        knife.transform.position = new Vector3(b.center.x, top + 0.02f, b.center.z + 0.17f);
        Block(knife.transform, "Knife_Handle", knife.transform.position + new Vector3(-0.07f, 0f, 0f),
            new Vector3(0.11f, 0.025f, 0.03f), mats.Wood, true);
        Block(knife.transform, "Knife_Blade", knife.transform.position + new Vector3(0.08f, 0f, 0f),
            new Vector3(0.16f, 0.006f, 0.035f), mats.Metal, true);
        CopyComponent(grabTemplate.GetComponent<Rigidbody>(), knife);
        CopyComponent(grabTemplate.GetComponent<XRGrabInteractable>(), knife);
        knife.GetComponent<Rigidbody>().isKinematic = true;
        knife.AddComponent<KitchenKnife>();

        CollectionFeedbackUI ui = CreateLabel(uiParent, feedbackPrefab, "CutStationUI",
            new Vector3(b.center.x, top + 0.55f, b.center.z),
            "เขียง: วางวัตถุดิบ แล้วใช้มีดสับลง 3 ครั้ง");

        StationZone zone = CreateZone<StationZone>(group, "CutStation_Zone",
            new Vector3(boardCenter.x, top + boardSize.y + 0.10f, boardCenter.z),
            new Vector3(boardSize.x, 0.20f, boardSize.z));
        zone.Configure(objective, ui, StationAction.Cut, null);
    }

    private static void BuildStove(
        Transform root, Transform uiParent, Transform stove,
        ObjectiveManager objective, GameObject feedbackPrefab, Materials mats)
    {
        Bounds b = WorldBounds(stove);
        float top = b.max.y;
        var group = CreateGroup(root, "StoveStation");
        float burnerX = b.center.x - 0.10f;

        GameObject flame = Block(group, "Burner_Flame", new Vector3(burnerX, top + 0.02f, b.center.z),
            new Vector3(0.22f, 0.02f, 0.22f), mats.Flame, false, PrimitiveType.Cylinder);
        flame.SetActive(false);

        // กระทะวางบนหัวเตา ยกขึ้นเล็กน้อยให้เห็นเปลวไฟด้านล่าง (ยังยกออกจากเตาไม่ได้)
        Block(group, "Pan_Base", new Vector3(burnerX, top + 0.07f, b.center.z),
            new Vector3(0.28f, 0.02f, 0.28f), mats.Dark, true, PrimitiveType.Cylinder);
        Block(group, "Pan_Handle", new Vector3(burnerX + 0.23f, top + 0.09f, b.center.z),
            new Vector3(0.18f, 0.025f, 0.035f), mats.Dark, true);

        // ปุ่มบิดที่หน้าเตา: แกนหมุนชี้ออกหาผู้เล่น (+Z)
        var knobRoot = new GameObject("HeatKnob");
        Undo.RegisterCreatedObjectUndo(knobRoot, "สร้างปุ่มเตา");
        knobRoot.transform.SetParent(group, false);
        knobRoot.transform.SetPositionAndRotation(
            new Vector3(b.center.x + 0.20f, b.center.y, b.max.z + 0.02f),
            Quaternion.Euler(90f, 0f, 0f));

        var handle = new GameObject("Handle");
        handle.transform.SetParent(knobRoot.transform, false);
        GameObject knobMesh = Block(handle.transform, "Knob_Mesh", Vector3.zero,
            new Vector3(0.07f, 0.02f, 0.07f), mats.Dark, true, PrimitiveType.Cylinder);
        knobMesh.transform.localPosition = Vector3.zero;
        knobMesh.transform.localRotation = Quaternion.identity;
        GameObject mark = Block(handle.transform, "Knob_Mark", Vector3.zero,
            new Vector3(0.008f, 0.004f, 0.03f), mats.White, false);
        mark.transform.localPosition = new Vector3(0f, 0.021f, 0.015f);
        mark.transform.localRotation = Quaternion.identity;

        XRKnob knob = knobRoot.AddComponent<XRKnob>();
        var knobData = new SerializedObject(knob);
        knobData.FindProperty("m_Handle").objectReferenceValue = handle.transform;
        knobData.FindProperty("m_Value").floatValue = 0f;
        knobData.FindProperty("m_ClampedMotion").boolValue = true;
        knobData.FindProperty("m_MinAngle").floatValue = 0f;
        knobData.FindProperty("m_MaxAngle").floatValue = 180f;
        knobData.FindProperty("m_AngleIncrement").floatValue = 60f;
        knobData.FindProperty("m_PositionTrackedRadius").floatValue = 0.05f;
        knobData.ApplyModifiedPropertiesWithoutUndo();

        CollectionFeedbackUI ui = CreateLabel(uiParent, feedbackPrefab, "StoveStationUI",
            new Vector3(b.center.x, top + 0.60f, b.center.z + 0.1f),
            "เตา: บิดปุ่มหน้าเตาเพื่อเปิดไฟ แล้วใส่ไก่ที่หั่นแล้วลงกระทะ");

        GasStove gasStove = group.gameObject.AddComponent<GasStove>();
        gasStove.Configure(knob, flame.transform, ui, objective);

        PanContents pan = CreateZone<PanContents>(group, "Pan_Contents_Zone",
            new Vector3(burnerX, top + 0.17f, b.center.z),
            new Vector3(0.28f, 0.18f, 0.28f));
        pan.Configure(objective, gasStove, ui);
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        var group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject Block(
        Transform parent, string name, Vector3 worldPosition, Vector3 size,
        Material material, bool keepCollider, PrimitiveType shape = PrimitiveType.Cube)
    {
        GameObject block = GameObject.CreatePrimitive(shape);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.position = worldPosition;
        block.transform.localScale = size;
        block.GetComponent<MeshRenderer>().sharedMaterial = material;

        Collider collider = block.GetComponent<Collider>();

        if (!keepCollider)
        {
            UnityEngine.Object.DestroyImmediate(collider);
        }
        else if (shape != PrimitiveType.Cube)
        {
            // ทรงกระบอกใช้กล่องแทน Capsule ให้ขอบแบนตรงกับรูป
            UnityEngine.Object.DestroyImmediate(collider);
            block.AddComponent<BoxCollider>();
        }

        return block;
    }

    private static T CreateZone<T>(Transform parent, string name, Vector3 worldCenter, Vector3 size)
        where T : Component
    {
        var zone = new GameObject(name);
        zone.transform.SetParent(parent, false);
        zone.transform.position = worldCenter;
        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;
        return zone.AddComponent<T>();
    }

    private static CollectionFeedbackUI CreateLabel(
        Transform parent, GameObject prefab, string name, Vector3 worldPosition, string message)
    {
        var label = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(label, "สร้างป้ายจุดทำงาน");
        label.name = name;
        label.transform.position = worldPosition;
        Vector3 parentScale = parent.lossyScale;
        label.transform.localScale = new Vector3(
            LabelScale / parentScale.x, LabelScale / parentScale.y, LabelScale / parentScale.z);

        var ui = label.GetComponent<CollectionFeedbackUI>();
        var data = new SerializedObject(ui);
        data.FindProperty("initialMessage").stringValue = message;
        data.ApplyModifiedPropertiesWithoutUndo();
        return ui;
    }

    private static void CopyComponent(Component source, GameObject target)
    {
        if (source == null || target.GetComponent(source.GetType()) != null)
        {
            return;
        }

        ComponentUtility.CopyComponent(source);
        ComponentUtility.PasteComponentAsNew(target);
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

    private static Bounds WorldBounds(Transform target)
    {
        Renderer renderer = target.GetComponent<Renderer>();

        if (renderer != null)
        {
            return renderer.bounds;
        }

        Collider collider = target.GetComponent<Collider>();
        return collider != null ? collider.bounds : new Bounds(target.position, target.lossyScale);
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
