using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class M1RemainingGameplaySetupEditor
{
    private const string ScenePath =
        "Assets/Scenes/GameplayTest_M1.unity";
    private const string KrapowPrefabPath =
        "Assets/Gameplay/Prefabs/Ingredients/Krapow.prefab";
    private const string ChiliPrefabPath =
        "Assets/Gameplay/Prefabs/Ingredients/Chili.prefab";
    private const string CollectionZonePrefabPath =
        "Assets/Gameplay/Prefabs/Stations/CollectionZone_Krapow.prefab";
    private const string FeedbackPrefabPath =
        "Assets/Gameplay/Prefabs/UI/CollectionFeedbackUI.prefab";
    private const string ChecklistPrefabPath =
        "Assets/Gameplay/Prefabs/UI/MissionChecklistUI.prefab";
    private const string ZoneMaterialPath =
        "Assets/Gameplay/Prefabs/Stations/M1_CollectionZone_Green.mat";

    [MenuItem("Tools/Cookverse Thai/M1/Apply Remaining Gameplay Setup")]
    public static void ApplyRemainingGameplaySetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError(
                "[ตั้งค่า M1] กรุณาออกจาก Play Mode ก่อนตั้งค่าระบบ");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(
            ScenePath,
            OpenSceneMode.Single);

        GameObject krapowPlant = FindInScene(scene, "Karpao");
        GameObject chiliPlant = FindInScene(scene, "Chili");
        GameObject vegetablePrepPoint =
            FindInScene(scene, "VegetablePrepPoint");
        GameObject gameplayObjects =
            FindInScene(scene, "GameplayObjects");
        GameObject gameplayUI = FindInScene(scene, "GameplayUI");
        ObjectiveManager objectiveManager =
            FindComponentInScene<ObjectiveManager>(scene);
        SessionManager sessionManager =
            FindComponentInScene<SessionManager>(scene);
        Camera playerCamera = FindComponentInScene<Camera>(scene);

        if (krapowPlant == null || chiliPlant == null ||
            vegetablePrepPoint == null ||
            gameplayObjects == null ||
            gameplayUI == null || objectiveManager == null ||
            sessionManager == null || playerCamera == null)
        {
            Debug.LogError(
                "[ตั้งค่า M1] ไม่พบ Object หลักใน GameplayTest_M1 " +
                "จึงยกเลิกการตั้งค่า");
            return;
        }

        RemoveCollectibleComponentsFromPlant(krapowPlant);
        GameObject krapowBunch = CreateOrConfigureKrapowBunch(
            scene,
            krapowPlant,
            gameplayObjects.transform);
        GameObject chili = CreateOrConfigureChili(
            scene,
            chiliPlant,
            gameplayObjects.transform);

        Bounds prepPointBounds = GetWorldBounds(vegetablePrepPoint);
        CollectionFeedbackUI feedbackUI = CreateOrConfigureFeedbackUI(
            scene,
            gameplayUI.transform,
            prepPointBounds);
        MissionChecklistUI checklistUI = CreateOrConfigureChecklistUI(
            scene,
            gameplayUI.transform,
            objectiveManager,
            playerCamera.transform);
        IngredientCollector collector = CreateOrConfigureCollectionZone(
            scene,
            gameplayObjects.transform,
            prepPointBounds);

        collector.Configure(objectiveManager, feedbackUI);
        EditorUtility.SetDirty(collector);

        ResetSavedSceneState(
            objectiveManager,
            sessionManager,
            krapowBunch.GetComponent<IngredientIdentity>(),
            chili.GetComponent<IngredientIdentity>());

        SaveGameplayPrefabs(
            krapowBunch,
            chili,
            collector.gameObject,
            feedbackUI.gameObject,
            checklistUI.gameObject);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = krapowBunch;
        Debug.Log(
            "[ตั้งค่า M1] เพิ่มภารกิจผัดกะเพราแล้ว: " +
            "ใบกะเพรา พริก และรายการตรวจสอบวัตถุดิบพร้อมใช้งาน");
    }

    private static void RemoveCollectibleComponentsFromPlant(
        GameObject krapowPlant)
    {
        XRGrabInteractable grab =
            krapowPlant.GetComponent<XRGrabInteractable>();
        IngredientIdentity identity =
            krapowPlant.GetComponent<IngredientIdentity>();
        Rigidbody body = krapowPlant.GetComponent<Rigidbody>();

        if (grab != null)
        {
            Undo.DestroyObjectImmediate(grab);
        }

        if (identity != null)
        {
            Undo.DestroyObjectImmediate(identity);
        }

        if (body != null)
        {
            Undo.DestroyObjectImmediate(body);
        }

        EditorUtility.SetDirty(krapowPlant);
    }

    private static GameObject CreateOrConfigureKrapowBunch(
        Scene scene,
        GameObject krapowPlant,
        Transform parent)
    {
        const string bunchName = "Krapow_Bunch_Collectible";
        GameObject bunch = FindInScene(scene, bunchName);

        if (bunch == null)
        {
            Transform sourceStem = null;

            foreach (Transform child in krapowPlant.transform)
            {
                if (child.name == "Krapao_Block")
                {
                    sourceStem = child;
                    break;
                }
            }

            if (sourceStem == null)
            {
                throw new MissingReferenceException(
                    "ไม่พบ Krapao_Block สำหรับสร้างกำกะเพรา");
            }

            bunch = Object.Instantiate(sourceStem.gameObject);
            bunch.name = bunchName;
            Undo.RegisterCreatedObjectUndo(bunch, "Create Krapow Bunch");
            Undo.SetTransformParent(
                bunch.transform,
                parent,
                "Parent Krapow Bunch");

            Bounds plantBounds = GetWorldBounds(krapowPlant);
            bunch.transform.position = new Vector3(
                plantBounds.center.x,
                plantBounds.center.y,
                plantBounds.max.z + 0.12f);
            bunch.transform.rotation = sourceStem.rotation;
            bunch.transform.localScale = sourceStem.lossyScale * 0.85f;
        }

        Rigidbody body = bunch.GetComponent<Rigidbody>();

        if (body == null)
        {
            body = Undo.AddComponent<Rigidbody>(bunch);
        }

        body.mass = 0.12f;
        body.useGravity = false;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        XRGrabInteractable grab = bunch.GetComponent<XRGrabInteractable>();

        if (grab == null)
        {
            grab = Undo.AddComponent<XRGrabInteractable>(bunch);
        }

        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.useDynamicAttach = true;

        IngredientIdentity identity = bunch.GetComponent<IngredientIdentity>();

        if (identity == null)
        {
            identity = Undo.AddComponent<IngredientIdentity>(bunch);
        }

        SerializedObject identityData = new SerializedObject(identity);
        identityData.FindProperty("ingredientId").stringValue =
            IngredientIdentity.KrapowLeafId;
        identityData.FindProperty("legacyPrefabName").stringValue = "Krapow";
        identityData.FindProperty("displayNameThai").stringValue =
            "กำกะเพรา";
        identityData.FindProperty("instanceId").stringValue = string.Empty;
        identityData.FindProperty("currentState").enumValueIndex =
            (int)IngredientState.Raw;
        identityData.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(bunch);
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(grab);
        EditorUtility.SetDirty(identity);
        return bunch;
    }

    private static GameObject CreateOrConfigureChili(
        Scene scene,
        GameObject chiliPlant,
        Transform parent)
    {
        const string chiliName = "Chili_Collectible";
        GameObject chili = FindInScene(scene, chiliName);

        if (chili == null)
        {
            chili = Object.Instantiate(chiliPlant);
            chili.name = chiliName;
            Undo.RegisterCreatedObjectUndo(chili, "Create Chili Collectible");
            Undo.SetTransformParent(
                chili.transform,
                parent,
                "Parent Chili Collectible");

            Bounds sourceBounds = GetWorldBounds(chiliPlant);
            chili.transform.position = new Vector3(
                sourceBounds.center.x,
                sourceBounds.max.y + 0.12f,
                sourceBounds.max.z + 0.12f);
            chili.transform.rotation = chiliPlant.transform.rotation;
            chili.transform.localScale =
                chiliPlant.transform.lossyScale * 0.55f;
        }

        Rigidbody body = chili.GetComponent<Rigidbody>();

        if (body == null)
        {
            body = Undo.AddComponent<Rigidbody>(chili);
        }

        body.mass = 0.08f;
        body.useGravity = false;
        body.isKinematic = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        XRGrabInteractable grab = chili.GetComponent<XRGrabInteractable>();

        if (grab == null)
        {
            grab = Undo.AddComponent<XRGrabInteractable>(chili);
        }

        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.useDynamicAttach = true;

        IngredientIdentity identity = chili.GetComponent<IngredientIdentity>();

        if (identity == null)
        {
            identity = Undo.AddComponent<IngredientIdentity>(chili);
        }

        SerializedObject identityData = new SerializedObject(identity);
        identityData.FindProperty("ingredientId").stringValue =
            IngredientIdentity.ChiliId;
        identityData.FindProperty("legacyPrefabName").stringValue = "Chili";
        identityData.FindProperty("displayNameThai").stringValue = "พริก";
        identityData.FindProperty("instanceId").stringValue = string.Empty;
        identityData.FindProperty("currentState").enumValueIndex =
            (int)IngredientState.Raw;
        identityData.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(chili);
        EditorUtility.SetDirty(body);
        EditorUtility.SetDirty(grab);
        EditorUtility.SetDirty(identity);
        return chili;
    }

    private static IngredientCollector CreateOrConfigureCollectionZone(
        Scene scene,
        Transform parent,
        Bounds prepPointBounds)
    {
        GameObject zone = FindInScene(scene, "CollectionZone_Krapow");

        if (zone == null)
        {
            zone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zone.name = "CollectionZone_Krapow";
            Undo.RegisterCreatedObjectUndo(
                zone,
                "Create Krapow Collection Zone");
            Undo.SetTransformParent(
                zone.transform,
                parent,
                "Parent Krapow Collection Zone");
        }

        Vector3 zonePosition = prepPointBounds.center;
        zonePosition.y = prepPointBounds.max.y + 0.035f;

        zone.transform.position = zonePosition;
        zone.transform.rotation = Quaternion.identity;
        zone.transform.localScale = new Vector3(0.62f, 0.08f, 0.48f);

        BoxCollider collider = zone.GetComponent<BoxCollider>();

        if (collider == null)
        {
            collider = Undo.AddComponent<BoxCollider>(zone);
        }

        collider.isTrigger = true;

        Rigidbody body = zone.GetComponent<Rigidbody>();

        if (body == null)
        {
            body = Undo.AddComponent<Rigidbody>(zone);
        }

        body.useGravity = false;
        body.isKinematic = true;

        IngredientCollector collector =
            zone.GetComponent<IngredientCollector>();

        if (collector == null)
        {
            collector = Undo.AddComponent<IngredientCollector>(zone);
        }

        MeshRenderer renderer = zone.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            renderer.sharedMaterial = GetOrCreateZoneMaterial();
        }

        EditorUtility.SetDirty(zone);
        return collector;
    }

    private static CollectionFeedbackUI CreateOrConfigureFeedbackUI(
        Scene scene,
        Transform parent,
        Bounds prepPointBounds)
    {
        GameObject feedbackObject =
            FindInScene(scene, "CollectionFeedbackUI");

        if (feedbackObject == null)
        {
            feedbackObject = new GameObject(
                "CollectionFeedbackUI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(CollectionFeedbackUI));

            Undo.RegisterCreatedObjectUndo(
                feedbackObject,
                "Create Collection Feedback UI");
            Undo.SetTransformParent(
                feedbackObject.transform,
                parent,
                "Parent Collection Feedback UI");
        }

        Canvas canvas = feedbackObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        RectTransform canvasRect =
            feedbackObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(600f, 120f);
        canvasRect.position = prepPointBounds.center + Vector3.up * 0.65f;
        canvasRect.localScale = Vector3.one * 0.003f;

        Transform textTransform = feedbackObject.transform.Find("FeedbackText");
        GameObject textObject;

        if (textTransform == null)
        {
            textObject = new GameObject(
                "FeedbackText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(Outline));

            Undo.RegisterCreatedObjectUndo(
                textObject,
                "Create Collection Feedback Text");
            Undo.SetTransformParent(
                textObject.transform,
                feedbackObject.transform,
                "Parent Collection Feedback Text");
        }
        else
        {
            textObject = textTransform.gameObject;
        }

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.localScale = Vector3.one;
        textRect.localRotation = Quaternion.identity;

        Text text = textObject.GetComponent<Text>();
        text.text = "นำใบกะเพราและพริกมาวางบนโต๊ะเตรียมวัตถุดิบ";
        text.fontSize = 34;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        CollectionFeedbackUI feedbackUI =
            feedbackObject.GetComponent<CollectionFeedbackUI>();
        feedbackUI.Configure(text);

        EditorUtility.SetDirty(feedbackObject);
        EditorUtility.SetDirty(feedbackUI);
        return feedbackUI;
    }

    private static MissionChecklistUI CreateOrConfigureChecklistUI(
        Scene scene,
        Transform parent,
        ObjectiveManager objectiveManager,
        Transform playerCamera)
    {
        GameObject checklistObject =
            FindInScene(scene, "MissionChecklistUI");

        if (checklistObject == null)
        {
            checklistObject = new GameObject(
                "MissionChecklistUI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(MissionChecklistUI));

            Undo.RegisterCreatedObjectUndo(
                checklistObject,
                "Create Mission Checklist UI");
            Undo.SetTransformParent(
                checklistObject.transform,
                parent,
                "Parent Mission Checklist UI");
        }

        Canvas canvas = checklistObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        canvas.sortingOrder = 25;

        RectTransform canvasRect =
            checklistObject.GetComponent<RectTransform>();
        canvasRect.localScale = Vector3.one;
        canvasRect.localRotation = Quaternion.identity;

        CanvasScaler scaler = checklistObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        Transform panelTransform = checklistObject.transform.Find(
            "ChecklistPanel");
        GameObject panelObject;

        if (panelTransform == null)
        {
            panelObject = new GameObject(
                "ChecklistPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Undo.RegisterCreatedObjectUndo(
                panelObject,
                "Create Checklist Panel");
            Undo.SetTransformParent(
                panelObject.transform,
                checklistObject.transform,
                "Parent Checklist Panel");
        }
        else
        {
            panelObject = panelTransform.gameObject;
        }

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.one;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = Vector2.one;
        panelRect.sizeDelta = new Vector2(520f, 320f);
        panelRect.anchoredPosition3D = new Vector3(-28f, -28f, 0f);
        panelRect.localScale = Vector3.one;
        panelRect.localRotation = Quaternion.identity;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.04f, 0.07f, 0.05f, 0.88f);
        panelImage.raycastTarget = false;

        Transform textTransform = panelObject.transform.Find("ChecklistText");
        GameObject textObject;

        if (textTransform == null)
        {
            textObject = new GameObject(
                "ChecklistText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            Undo.RegisterCreatedObjectUndo(
                textObject,
                "Create Checklist Text");
            Undo.SetTransformParent(
                textObject.transform,
                panelObject.transform,
                "Parent Checklist Text");
        }
        else
        {
            textObject = textTransform.gameObject;
        }

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 20f);
        textRect.offsetMax = new Vector2(-24f, -20f);
        textRect.localScale = Vector3.one;
        textRect.localRotation = Quaternion.identity;

        Text text = textObject.GetComponent<Text>();
        text.fontSize = 30;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        MissionChecklistUI checklistUI =
            checklistObject.GetComponent<MissionChecklistUI>();
        checklistUI.Configure(objectiveManager, text, playerCamera);

        EditorUtility.SetDirty(checklistObject);
        EditorUtility.SetDirty(checklistUI);
        return checklistUI;
    }

    private static void ResetSavedSceneState(
        ObjectiveManager objectiveManager,
        SessionManager sessionManager,
        IngredientIdentity krapow,
        IngredientIdentity chili)
    {
        SerializedObject objectiveData =
            new SerializedObject(objectiveManager);
        objectiveData.FindProperty("currentStepId").stringValue =
            ObjectiveManager.AcceptMissionStepId;
        objectiveData.FindProperty("missionAccepted").boolValue = false;
        objectiveData.FindProperty("collectedKrapowCount").intValue = 0;
        objectiveData.FindProperty("collectedChiliCount").intValue = 0;
        objectiveData.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject sessionData = new SerializedObject(sessionManager);
        sessionData.FindProperty("sessionId").stringValue = string.Empty;
        sessionData.ApplyModifiedPropertiesWithoutUndo();

        IngredientIdentity[] ingredients = { krapow, chili };

        foreach (IngredientIdentity ingredient in ingredients)
        {
            if (ingredient == null)
            {
                continue;
            }

            SerializedObject ingredientData =
                new SerializedObject(ingredient);
            ingredientData.FindProperty("instanceId").stringValue =
                string.Empty;
            ingredientData.FindProperty("currentState").enumValueIndex =
                (int)IngredientState.Raw;
            ingredientData.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void SaveGameplayPrefabs(
        GameObject krapow,
        GameObject chili,
        GameObject collectionZone,
        GameObject feedbackUI,
        GameObject checklistUI)
    {
        SaveCleanPrefabCopy(krapow, KrapowPrefabPath, "Krapow");
        SaveCleanPrefabCopy(chili, ChiliPrefabPath, "Chili");
        SaveCleanPrefabCopy(
            collectionZone,
            CollectionZonePrefabPath,
            "CollectionZone_Krapow",
            true);
        SaveCleanPrefabCopy(
            feedbackUI,
            FeedbackPrefabPath,
            "CollectionFeedbackUI");
        SaveCleanPrefabCopy(
            checklistUI,
            ChecklistPrefabPath,
            "MissionChecklistUI",
            false,
            true);
    }

    private static void SaveCleanPrefabCopy(
        GameObject source,
        string path,
        string prefabName,
        bool clearCollectorReferences = false,
        bool clearChecklistReferences = false)
    {
        GameObject copy = Object.Instantiate(source);
        copy.name = prefabName;
        copy.transform.SetParent(null);

        if (clearCollectorReferences)
        {
            IngredientCollector collector =
                copy.GetComponent<IngredientCollector>();
            collector?.Configure(null, null);
        }

        if (clearChecklistReferences)
        {
            MissionChecklistUI checklist =
                copy.GetComponent<MissionChecklistUI>();
            Text text = copy.GetComponentInChildren<Text>(true);
            checklist?.Configure(null, text, null);
        }

        PrefabUtility.SaveAsPrefabAsset(copy, path);
        Object.DestroyImmediate(copy);
    }

    private static Material GetOrCreateZoneMaterial()
    {
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(ZoneMaterialPath);

        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        material = new Material(shader)
        {
            name = "M1_CollectionZone_Green",
            color = new Color(0.15f, 0.85f, 0.3f, 1f)
        };

        AssetDatabase.CreateAsset(material, ZoneMaterialPath);
        return material;
    }

    private static Bounds GetWorldBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return new Bounds(root.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;

        for (int index = 1; index < renderers.Length; index++)
        {
            bounds.Encapsulate(renderers[index].bounds);
        }

        return bounds;
    }

    private static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName)
                {
                    return candidate.gameObject;
                }
            }
        }

        return null;
    }

    private static T FindComponentInScene<T>(Scene scene)
        where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);

            if (component != null)
            {
                return component;
            }
        }

        return null;
    }
}
