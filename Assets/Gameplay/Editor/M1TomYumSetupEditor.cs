using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ต้มยำกุ้ง (เมนู 2) ใน GameplayTest_M1 ด้วยคลิกเดียว · รันซ้ำได้
// - ไฟล์เมนู (Create Menu Assets) · Prefab กุ้ง/เห็ดฟาง วางในตู้เย็น (ชั้น = กุ้ง 5 · พื้นตู้ = เห็ด 4)
// - หม้อบนเตา + น้ำในหม้อ · ขวด น้ำ/น้ำพริกเผา/นมข้นจืด แทนที่ขวดน้ำมัน/ซอสหอย/น้ำตาล (น้ำปลาใช้ขวดเดิม)
// - ติด MenuOnly ให้ของเฉพาะเมนู + MenuSceneSwitcher เปิด/ปิดตามเมนู
// เปลี่ยนเมนูทดสอบ: Tools › Cookverse Thai › M1 › Use Menu: ...
public static class M1TomYumSetupEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string IngredientFolder = "Assets/Gameplay/Prefabs/Ingredients";
    private const string ChickenPrefabPath = IngredientFolder + "/Chicken.prefab";
    private const string PotPrefabPath = "Assets/XR-Web/Art/Kitchen/Prefabs/Pot.prefab";
    private const string FeedbackPrefabPath = "Assets/Gameplay/Prefabs/UI/CollectionFeedbackUI.prefab";
    private const string MenuFolder = "Assets/Gameplay/Data/Menus";
    private const string RootName = "TomYumCooking";
    private const float PotWidth = 0.30f;

    private static readonly Vector3 ShrimpSize = new(0.075f, 0.028f, 0.03f);
    private static readonly Vector3 MushroomSize = new(0.04f, 0.045f, 0.04f);

    private static readonly string[] KrapowOnly = { MenuCatalog.KrapowChickenId };
    private static readonly string[] TomYumOnly = { MenuCatalog.TomYumKungId };
    private static readonly string[] TomKhaOnly = { MenuCatalog.TomKhaGaiId };
    private static readonly string[] PotMenus = { MenuCatalog.TomYumKungId, MenuCatalog.TomKhaGaiId };

    [MenuItem("Tools/Cookverse Thai/M1/Setup Tom Yum (Pot + Shrimp + Bottles)")]
    public static void SetupMenuItem() => Setup();

    // ต้มยำ + ต้มข่า ใช้หม้อชุดเดียวกัน · ต้มข่าเพิ่มขวดกะทิ (วางที่ถ้วยผสมซอสของกะเพรา)
    public static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");

        M1MenuAssetsEditor.CreateMenuAssets();

        ObjectiveManager objective = Find<ObjectiveManager>(scene);
        PanContents pan = Find<PanContents>(scene);
        Transform panBase = FindT(scene, "Pan_Base");
        Transform gameplayObjects = FindT(scene, "GameplayObjects");
        Transform fridgeShelf = FindT(scene, "Fridge_Shelf");
        Transform fridgeBottom = FindT(scene, "Fridge_Bottom");
        Transform gameplayUI = FindT(scene, "GameplayUI");
        GameObject feedbackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FeedbackPrefabPath);

        if (objective == null || pan == null || panBase == null || gameplayObjects == null ||
            fridgeShelf == null || fridgeBottom == null || gameplayUI == null || feedbackPrefab == null)
        {
            Debug.LogError("[ต้มยำ] ไม่พบ ObjectiveManager / PanContents / Pan_Base / GameplayObjects / Fridge_Shelf / " +
                           "Fridge_Bottom / GameplayUI · รัน Setup Kitchen และ Setup Pan Cooking ก่อน");
            return;
        }

        // ---------- วัตถุดิบในตู้เย็น ----------
        GameObject shrimp = IngredientPrefab("Shrimp", MenuCatalog.ShrimpId, "กุ้ง", ShrimpSize,
            new Color(0.95f, 0.62f, 0.50f), "M1_Shrimp");
        GameObject mushroom = IngredientPrefab("StrawMushroom", MenuCatalog.StrawMushroomId, "เห็ดฟาง", MushroomSize,
            new Color(0.78f, 0.70f, 0.58f), "M1_StrawMushroom");
        if (shrimp == null || mushroom == null) return;

        Bounds shelf = fridgeShelf.GetComponent<Renderer>().bounds;
        Bounds bottom = fridgeBottom.GetComponent<Renderer>().bounds;
        PlaceRow(scene, shrimp, gameplayObjects, "Shrimp_Collectible_", MenuCatalog.ShrimpId, 5, ShrimpSize,
            shelf, shelf.min.z + 0.07f);
        PlaceRow(scene, mushroom, gameplayObjects, "StrawMushroom_Collectible_", MenuCatalog.StrawMushroomId, 4,
            MushroomSize, bottom, bottom.center.z);

        // ---------- หม้อ น้ำ ขวด ----------
        Transform station = pan.transform.parent;
        Transform panCooking = station.Find("PanCooking");
        Transform old = station.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        Transform oldLabel = gameplayUI.Find("TomYumShelfUI");
        if (oldLabel != null) Undo.DestroyObjectImmediate(oldLabel.gameObject);
        Transform oldKhaLabel = gameplayUI.Find("TomKhaShelfUI");
        if (oldKhaLabel != null) Undo.DestroyObjectImmediate(oldKhaLabel.gameObject);

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "สร้างของต้มยำ");
        root.transform.SetParent(station, false);

        Material template = panBase.GetComponent<Renderer>().sharedMaterial;
        Bounds baseBounds = panBase.GetComponent<Renderer>().bounds;
        float potHeight = BuildPot(root.transform, baseBounds);
        Transform liquid = BuildLiquid(root.transform, baseBounds, potHeight, template);
        var liquidVisual = root.AddComponent<PotLiquidVisual>();
        liquidVisual.Configure(pan, liquid);
        EditorUtility.SetDirty(liquidVisual);

        // ขวดต้มยำวางแทนที่ขวดกะเพรา (ปิดขวดเดิมชั่วคราวให้ Ray ลงถึงชั้น)
        var swaps = new (string oldName, string id, string nameThai, Color color)[]
        {
            ("Seasoning_cooking_oil", MenuCatalog.WaterId, "น้ำ", new Color(0.45f, 0.70f, 0.95f)),
            ("Seasoning_oyster_sauce", MenuCatalog.NamPrikPaoId, "น้ำพริกเผา", new Color(0.70f, 0.15f, 0.08f)),
            ("Seasoning_sugar", MenuCatalog.EvaporatedMilkId, "นมข้นจืด", new Color(0.98f, 0.95f, 0.85f)),
        };
        Vector3 labelPos = Vector3.zero;
        foreach (var swap in swaps)
        {
            Transform oldBottle = panCooking != null ? panCooking.Find(swap.oldName) : null;
            if (oldBottle == null)
            {
                Debug.LogWarning($"[ต้มยำ] ไม่พบ {swap.oldName} จึงข้ามขวด{swap.nameThai}");
                continue;
            }
            bool wasActive = oldBottle.gameObject.activeSelf;
            oldBottle.gameObject.SetActive(false);
            Physics.SyncTransforms();
            Vector3 slot = oldBottle.position + Vector3.up * 0.15f;
            M1PanSetupEditor.BuildBottle(root.transform, swap.id, swap.nameThai, slot, swap.color, pan, template);
            oldBottle.gameObject.SetActive(wasActive);
            labelPos = oldBottle.position;
        }

        // ขวดกะทิ (ต้มข่า) วางที่ถ้วยผสมซอสของกะเพรา (ถ้วยซ่อนในเมนูหม้อ)
        Transform bowl = panCooking != null ? panCooking.Find("Sauce_Bowl") : null;
        if (bowl != null)
        {
            bool bowlActive = bowl.gameObject.activeSelf;
            bowl.gameObject.SetActive(false);
            Physics.SyncTransforms();
            M1PanSetupEditor.BuildBottle(root.transform, MenuCatalog.CoconutMilkId, "กะทิ", bowl.position + Vector3.up * 0.15f,
                new Color(0.98f, 0.97f, 0.93f), pan, template);
            bowl.gameObject.SetActive(bowlActive);
        }
        else Debug.LogWarning("[ต้มยำ] ไม่พบ Sauce_Bowl จึงไม่ได้วางขวดกะทิ");

        Transform krapowLabel = gameplayUI.Find("SeasoningShelfUI");
        Vector3 tomYumLabelPos = krapowLabel != null ? krapowLabel.position : labelPos + Vector3.up * 0.45f;
        M1PanSetupEditor.CreateLabel(gameplayUI, feedbackPrefab, "TomYumShelfUI", tomYumLabelPos,
            "ชั้นเครื่องปรุง (ต้มยำ): ฟ้า=น้ำ · แดง=น้ำพริกเผา · ขาว=นมข้นจืด · ส้ม=น้ำปลา\n" +
            "เทน้ำลงหม้อ เปิดไฟจนเดือด · ปรุงรสหลังกุ้งสุก · ปิดไฟก่อนบีบมะนาว");
        Transform tomYumLabel = gameplayUI.Find("TomYumShelfUI");
        if (krapowLabel != null) tomYumLabel.rotation = krapowLabel.rotation;
        M1PanSetupEditor.CreateLabel(gameplayUI, feedbackPrefab, "TomKhaShelfUI", tomYumLabelPos,
            "ต้มข่าไก่: ขวดขาวบนโต๊ะเตรียม=กะทิ · ส้ม=น้ำปลา\n" +
            "เทกะทิลงหม้อ เปิดไฟจนเดือด · ใส่ไก่หลังสมุนไพร · ปิดไฟก่อนบีบมะนาว");
        Transform tomKhaLabel = gameplayUI.Find("TomKhaShelfUI");
        if (krapowLabel != null) tomKhaLabel.rotation = krapowLabel.rotation;

        // ---------- ของเฉพาะเมนู ----------
        Tag(root.transform, PotMenus, false);
        Tag(tomYumLabel, TomYumOnly, false);
        Tag(tomKhaLabel, TomKhaOnly, false);
        Tag(root.transform.Find("Seasoning_" + MenuCatalog.NamPrikPaoId), TomYumOnly, false);
        Tag(root.transform.Find("Seasoning_" + MenuCatalog.EvaporatedMilkId), TomYumOnly, false);
        Tag(root.transform.Find("Seasoning_" + MenuCatalog.WaterId), TomYumOnly, false);
        Tag(root.transform.Find("Seasoning_" + MenuCatalog.CoconutMilkId), TomKhaOnly, false);
        if (panCooking != null)
        {
            foreach (string n in new[] { "Seasoning_cooking_oil", "Seasoning_oyster_sauce", "Seasoning_sugar", "Sauce_Bowl" })
                Tag(panCooking.Find(n), KrapowOnly, false);
        }
        Tag(krapowLabel, KrapowOnly, false);
        Tag(gameplayUI.Find("SauceBowlUI"), KrapowOnly, false);
        // ภาพ Pan_Base/Pan_Rim/Pan_Handle ซ่อนถาวรโดย Fit Stove Station (ใช้โมเดล Wok แทน)
        Tag(FindT(scene, "Pot_OnCounter"), KrapowOnly, true);
        // ของบนเตาที่ Fit Stove สร้าง (กระทะโมเดล) คงไว้ตามเดิม   // หม้อบนเคาน์เตอร์ย้ายมาอยู่บนเตาตอนต้มยำ

        MenuSceneSwitcher switcher = objective.GetComponent<MenuSceneSwitcher>();
        if (switcher == null) switcher = Undo.AddComponent<MenuSceneSwitcher>(objective.gameObject);
        switcher.Configure(objective);
        EditorUtility.SetDirty(switcher);
        switcher.Apply();

        Save(scene);
        Debug.Log($"[ต้มยำ] เสร็จแล้ว: กุ้ง 5 (ชั้นตู้เย็น) · เห็ดฟาง 4 (พื้นตู้เย็น) · หม้อบนเตา · ขวดน้ำ/น้ำพริกเผา/นมข้นจืด · " +
                  $"เมนูตอนนี้ = {objective.Menu.nameThai} (เปลี่ยนด้วย Use Menu: ...)");
    }

    [MenuItem("Tools/Cookverse Thai/M1/Use Menu: Tom Yum Kung")]
    public static void UseTomYum() => UseMenu("Menu_TomYumKung");

    [MenuItem("Tools/Cookverse Thai/M1/Use Menu: Tom Kha Gai")]
    public static void UseTomKha() => UseMenu("Menu_TomKhaGai");

    [MenuItem("Tools/Cookverse Thai/M1/Use Menu: Krapow Chicken")]
    public static void UseKrapow() => UseMenu("Menu_KrapowChicken");

    private static void UseMenu(string assetName)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");

        var menu = AssetDatabase.LoadAssetAtPath<MenuDefinition>($"{MenuFolder}/{assetName}.asset");
        if (menu == null)
        {
            M1MenuAssetsEditor.CreateMenuAssets();
            menu = AssetDatabase.LoadAssetAtPath<MenuDefinition>($"{MenuFolder}/{assetName}.asset");
        }

        ObjectiveManager objective = Find<ObjectiveManager>(scene);
        var so = new SerializedObject(objective);
        so.FindProperty("menu").objectReferenceValue = menu;
        so.ApplyModifiedProperties();

        MenuSceneSwitcher switcher = objective.GetComponent<MenuSceneSwitcher>();
        if (switcher != null) switcher.Apply(menu.menuId);
        else Debug.LogWarning("[เมนู] ยังไม่มี MenuSceneSwitcher · รัน Setup Tom Yum ก่อน");

        Save(scene);
        Debug.Log($"[เมนู] ใช้เมนู {menu.nameThai} แล้ว กด Play เพื่อทดสอบ");
    }

    // ---------- ส่วนประกอบ ----------

    private static GameObject IngredientPrefab(string name, string id, string nameThai, Vector3 size, Color color, string matName)
    {
        string path = $"{IngredientFolder}/{name}.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        if (!AssetDatabase.CopyAsset(ChickenPrefabPath, path))
        {
            Debug.LogError($"[ต้มยำ] คัดลอก {ChickenPrefabPath} ไม่สำเร็จ");
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            root.name = name;
            root.transform.localScale = size;
            var identity = new SerializedObject(root.GetComponent<IngredientIdentity>());
            identity.FindProperty("ingredientId").stringValue = id;
            identity.FindProperty("legacyPrefabName").stringValue = name;
            identity.FindProperty("displayNameThai").stringValue = nameThai;
            identity.FindProperty("instanceId").stringValue = string.Empty;
            identity.FindProperty("cutSizeMultiplier").floatValue = 1f;
            identity.ApplyModifiedPropertiesWithoutUndo();

            var renderer = root.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                string matPath = $"{IngredientFolder}/{matName}.mat";
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    mat = new Material(renderer.sharedMaterial) { name = matName };
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                    AssetDatabase.CreateAsset(mat, matPath);
                }
                renderer.sharedMaterial = mat;
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"[ต้มยำ] สร้าง {path}");
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static void PlaceRow(Scene scene, GameObject prefab, Transform parent, string baseName, string id,
                                 int count, Vector3 size, Bounds surface, float z)
    {
        int existing = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<IngredientIdentity>(true))
            .Count(i => i.IngredientId == id);
        if (existing >= count)
        {
            Debug.Log($"[ต้มยำ] มี {id} ครบ {count} ชิ้นแล้ว จึงข้าม");
            return;
        }

        float usable = Mathf.Max(0.1f, surface.size.x - 0.12f);
        float step = Mathf.Min(size.x + 0.02f, usable / count);
        float startX = surface.center.x - step * (count - 1) / 2f;
        float y = surface.max.y + size.y / 2f + 0.005f;

        for (int n = existing; n < count; n++)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Undo.RegisterCreatedObjectUndo(instance, "เพิ่ม " + baseName);
            instance.name = baseName + (n + 1);
            instance.transform.SetPositionAndRotation(new Vector3(startX + step * n, y, z), Quaternion.identity);
            Vector3 ps = parent.lossyScale;
            instance.transform.localScale = new Vector3(size.x / ps.x, size.y / ps.y, size.z / ps.z);
            var identity = new SerializedObject(instance.GetComponent<IngredientIdentity>());
            identity.FindProperty("instanceId").stringValue = string.Empty;
            identity.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // หม้อจากโมเดล M5 (ภาพอย่างเดียว ไม่มี Collider) · ก้นหม้อวางบนฐานกระทะ · คืนความสูงหม้อ
    private static float BuildPot(Transform root, Bounds baseBounds)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PotPrefabPath);
        GameObject pot;
        if (prefab != null)
        {
            pot = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            PrefabUtility.UnpackPrefabInstance(pot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        else
        {
            pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.transform.SetParent(root, false);
        }
        pot.name = "Pot_OnStove";
        foreach (Collider c in pot.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
        foreach (Rigidbody r in pot.GetComponentsInChildren<Rigidbody>(true)) UnityEngine.Object.DestroyImmediate(r);

        pot.transform.rotation = Quaternion.identity;
        pot.transform.position = Vector3.zero;
        Bounds b = RendererBounds(pot);
        float width = Mathf.Max(b.size.x, b.size.z, 0.001f);
        pot.transform.localScale *= PotWidth / width;
        b = RendererBounds(pot);
        Vector3 target = new(baseBounds.center.x, baseBounds.min.y, baseBounds.center.z);
        pot.transform.position += target - new Vector3(b.center.x, b.min.y, b.center.z);
        return RendererBounds(pot).size.y;
    }

    private static Transform BuildLiquid(Transform root, Bounds baseBounds, float potHeight, Material template)
    {
        Material mat = M1PanSetupEditor.GetOrCreateMaterial("M1_PotLiquid", new Color(0.55f, 0.75f, 0.85f), template);
        GameObject liquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        liquid.name = "Pot_Liquid";
        UnityEngine.Object.DestroyImmediate(liquid.GetComponent<Collider>());
        liquid.transform.SetParent(root, false);
        liquid.transform.position = new Vector3(baseBounds.center.x, baseBounds.min.y + potHeight * 0.6f, baseBounds.center.z);
        Vector3 ps = root.lossyScale;
        float d = PotWidth * 0.82f;
        liquid.transform.localScale = new Vector3(d / ps.x, 0.004f / ps.y, d / ps.z);
        liquid.GetComponent<MeshRenderer>().sharedMaterial = mat;
        liquid.SetActive(false);
        return liquid.transform;
    }

    private static void Tag(Transform t, string[] ids, bool rendererOnly)
    {
        if (t == null) return;
        MenuOnly tag = t.GetComponent<MenuOnly>();
        if (tag == null) tag = Undo.AddComponent<MenuOnly>(t.gameObject);
        tag.Configure(ids, rendererOnly);
        EditorUtility.SetDirty(tag);
    }

    private static Bounds RendererBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.1f);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs.Skip(1)) b.Encapsulate(r.bounds);
        return b;
    }

    private static T Find<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).FirstOrDefault();

    private static Transform FindT(Scene scene, string name) =>
        scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name.Trim() == name);

    private static void Save(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
