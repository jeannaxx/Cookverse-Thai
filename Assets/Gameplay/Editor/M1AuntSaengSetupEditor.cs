using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// ป้าแสง (Meshy · Rig Mixamo · ~10K สามเหลี่ยม) → Prefab AuntSaeng + วางแทนป้าแสงเดิมใน GameplayTest_M1
// ไฟล์ใน Assets/Gameplay/Characters/AuntSaeng/
//   Models/AuntSaeng_Idle.fbx  (ตัวป้า + ท่า Idle · ใช้สร้าง Avatar Humanoid)
//   Models/AuntSaeng_Talk.fbx, AuntSaeng_Wave.fbx (ใช้ Avatar จาก Idle)
//   Textures/AuntSaeng_BaseColor · _Normal · _MetallicSmoothness (R=metallic, A=1-roughness รวมจากไฟล์ Meshy)
// เมนูนี้: ตั้ง Importer ทุกไฟล์ → วัสดุ URP Lit → Animator Controller (Idle วน · Wave · Talk · Agree ถ้ามีไฟล์)
//   → Prefab AuntSaeng.prefab (Prefab Variant ของ Idle.fbx) → ถ้าเปิด GameplayTest_M1 อยู่: วางแทนกล่อง NPC_GreetingPosition
//   (ซ่อนกล่องไว้ ไม่ลบ) ใต้ NPC_Mission_M1 · ไม่แตะ MissionInteractionZone · ต่อ Event Wave/Talk ให้
// รันซ้ำได้ (เขียนทับ Controller/วัสดุ/Prefab · ลบป้าแสงตัวเดิมในฉากแล้ววางใหม่)
public static class M1AuntSaengSetupEditor
{
    private const string Folder = "Assets/Gameplay/Characters/AuntSaeng";
    private const string IdlePath = Folder + "/Models/AuntSaeng_Idle.fbx";
    private const string TalkPath = Folder + "/Models/AuntSaeng_Talk.fbx";
    private const string WavePath = Folder + "/Models/AuntSaeng_Wave.fbx";
    private const string AgreePath = Folder + "/Models/AuntSaeng_Agree.fbx";   // ยังไม่มี · ถ้าเพิ่มไฟล์ภายหลังจะใช้อัตโนมัติ
    private const string BaseColorPath = Folder + "/Textures/AuntSaeng_BaseColor.png";
    private const string NormalPath = Folder + "/Textures/AuntSaeng_Normal.png";
    private const string MetallicPath = Folder + "/Textures/AuntSaeng_MetallicSmoothness.png";
    private const string MaterialPath = Folder + "/AuntSaeng.mat";
    private const string ControllerPath = Folder + "/Animation/AuntSaeng.controller";
    private const string PrefabPath = Folder + "/AuntSaeng.prefab";
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const int TextureMaxSize = 1024;      // พอสำหรับ NPC บนแว่น/WebXR (ต้นฉบับ 2048)
    private const float ExpectedHeight = 1.52f;   // ความสูงที่ Rig ใน Meshy

    [MenuItem("Tools/Cookverse Thai/M1/Setup Aunt Saeng (Prefab)")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("หยุด Play ก่อน");

        Avatar avatar = SetupModels();
        if (avatar == null) return;
        Material material = SetupMaterial();
        AnimatorController controller = SetupController();
        GameObject prefab = SetupPrefab(avatar, controller, material);
        if (prefab == null) return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path == ScenePath) PlaceInScene(scene, prefab);
        else Debug.Log($"[ป้าแสง] สร้าง Prefab แล้วที่ {PrefabPath} · เปิด GameplayTest_M1 แล้วรันอีกครั้งเพื่อวางในฉาก");
    }

    // ---------- Importer ----------
    private static Avatar SetupModels()
    {
        var idle = AssetImporter.GetAtPath(IdlePath) as ModelImporter;
        if (idle == null)
        {
            Debug.LogError($"[ป้าแสง] ไม่พบ {IdlePath}");
            return null;
        }

        ConfigureModel(idle, ModelImporterAvatarSetup.CreateFromThisModel, null, "AuntSaeng_Idle", loop: true);
        Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(IdlePath).OfType<Avatar>().FirstOrDefault();
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            Debug.LogError("[ป้าแสง] สร้าง Avatar Humanoid จาก Idle ไม่สำเร็จ · เปิด Idle.fbx > Rig > Configure ดูกระดูกที่ขาด");
            return null;
        }

        foreach ((string path, string clip) in new[] { (TalkPath, "AuntSaeng_Talk"), (WavePath, "AuntSaeng_Wave"), (AgreePath, "AuntSaeng_Agree") })
        {
            if (AssetImporter.GetAtPath(path) is ModelImporter mi)
            {
                ConfigureModel(mi, ModelImporterAvatarSetup.CopyFromOther, avatar, clip, loop: false);
            }
        }
        return avatar;
    }

    private static void ConfigureModel(ModelImporter mi, ModelImporterAvatarSetup setup, Avatar source, string clipName, bool loop)
    {
        mi.animationType = ModelImporterAnimationType.Human;
        mi.avatarSetup = setup;
        if (source != null) mi.sourceAvatar = source;
        mi.materialImportMode = ModelImporterMaterialImportMode.None;   // ใช้วัสดุ URP ที่สร้างเอง
        mi.importCameras = false;
        mi.importLights = false;
        mi.importBlendShapes = false;
        mi.importAnimation = true;

        ModelImporterClipAnimation[] clips = mi.defaultClipAnimations;
        foreach (ModelImporterClipAnimation c in clips)
        {
            c.name = clipName;
            c.loopTime = loop;
            // ยืนอยู่กับที่: ล็อกการหมุน/ความสูง/ตำแหน่งราก (ไม่ให้ตัวเลื่อนหรือหมุนเอง)
            c.lockRootRotation = true;
            c.lockRootHeightY = true;
            c.lockRootPositionXZ = true;
            c.keepOriginalOrientation = true;
            c.keepOriginalPositionY = true;
            c.keepOriginalPositionXZ = true;
        }
        mi.clipAnimations = clips;
        mi.SaveAndReimport();
    }

    // ---------- วัสดุ ----------
    private static Material SetupMaterial()
    {
        SetTexture(BaseColorPath, TextureImporterType.Default, true);
        SetTexture(NormalPath, TextureImporterType.NormalMap, false);
        SetTexture(MetallicPath, TextureImporterType.Default, false);

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(lit) { name = "AuntSaeng" };
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        else if (mat.shader != lit) mat.shader = lit;

        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
        mat.SetFloat("_BumpScale", 1f);
        mat.EnableKeyword("_NORMALMAP");
        mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicPath));
        mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_Smoothness", 1f);               // ตัวคูณ smoothness จาก alpha ของแผนที่
        mat.SetFloat("_SmoothnessTextureChannel", 0f); // อ่านจาก alpha ของ Metallic map
        mat.SetFloat("_Metallic", 1f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static void SetTexture(string path, TextureImporterType type, bool sRGB)
    {
        if (!(AssetImporter.GetAtPath(path) is TextureImporter ti)) return;
        ti.textureType = type;
        ti.sRGBTexture = sRGB;
        ti.maxTextureSize = TextureMaxSize;
        ti.mipmapEnabled = true;
        ti.textureCompression = TextureImporterCompression.Compressed;
        ti.SaveAndReimport();
    }

    // ---------- Animator ----------
    private static AnimatorController SetupController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        AnimatorState idle = sm.AddState("Idle", new Vector3(300, 0));
        idle.motion = Clip(IdlePath);
        sm.defaultState = idle;

        AnimatorState wave = AddOneShot(ctrl, sm, idle, "Wave", WavePath, new Vector3(550, -120));
        AnimatorState talk = AddOneShot(ctrl, sm, idle, "Talk", TalkPath, new Vector3(550, 0));
        AddOneShot(ctrl, sm, idle, "Agree", AgreePath, new Vector3(550, 120));

        // กำลังโบกมืออยู่แล้วมีคำสั่งพูด → พูดต่อหลังโบกเกือบจบ (ใส่ก่อน Wave→Idle ให้ถูกตรวจก่อน)
        if (wave != null && talk != null)
        {
            AnimatorStateTransition t = wave.AddTransition(talk);
            t.hasExitTime = true; t.exitTime = 0.85f; t.duration = 0.25f;
            t.AddCondition(AnimatorConditionMode.If, 0, AuntSaengAnimator.TalkTrigger);
            AnimatorStateTransition[] list = wave.transitions;
            wave.transitions = new[] { t }.Concat(list.Where(x => x != t)).ToArray();
        }

        AssetDatabase.SaveAssets();
        return ctrl;
    }

    private static AnimatorState AddOneShot(AnimatorController ctrl, AnimatorStateMachine sm, AnimatorState idle, string name, string fbx, Vector3 pos)
    {
        AnimationClip clip = Clip(fbx);
        if (clip == null) return null;
        ctrl.AddParameter(name, AnimatorControllerParameterType.Trigger);
        AnimatorState s = sm.AddState(name, pos);
        s.motion = clip;

        AnimatorStateTransition enter = idle.AddTransition(s);
        enter.hasExitTime = false; enter.duration = 0.25f;
        enter.AddCondition(AnimatorConditionMode.If, 0, name);

        AnimatorStateTransition back = s.AddTransition(idle);
        back.hasExitTime = true; back.exitTime = 0.95f; back.duration = 0.3f;
        return s;
    }

    private static AnimationClip Clip(string fbx) =>
        AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));

    // ---------- Prefab ----------
    private static GameObject SetupPrefab(Avatar avatar, AnimatorController controller, Material material)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(IdlePath);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(model);
        go.name = "AuntSaeng";
        try
        {
            Animator anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();   // Unity null ไม่ใช้ ?? กับ Component
            anim.avatar = avatar;
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // ขอบเขต Skinned Mesh จาก Meshy ไม่ตรง → ถ้า Cull Unity คิดว่ามองไม่เห็นแล้วหยุดท่า

            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                r.shadowCastingMode = ShadowCastingMode.On;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;   // คำนวณขอบเขตตามท่าจริง (ขอบเขตเดิมจาก Meshy ไม่ตรง)
            }

            // ความสูงจริงจาก Mesh (Meshy บางครั้งได้หน่วยซม.) → ปรับสเกลให้สูง ~1.52 ม.
            Bounds b = Bounds(go);
            float height = b.size.y;
            if (height > 0.01f && (height < ExpectedHeight * 0.7f || height > ExpectedHeight * 1.4f))
            {
                go.transform.localScale *= ExpectedHeight / height;
                b = Bounds(go);
                height = b.size.y;
            }

            CapsuleCollider cap = go.GetComponent<CapsuleCollider>();
            if (cap == null) cap = go.AddComponent<CapsuleCollider>();
            float s = go.transform.localScale.y;
            cap.height = height / s;
            cap.radius = 0.22f / s;
            cap.center = new Vector3(0f, height / s / 2f, 0f);

            if (go.GetComponent<AuntSaengAnimator>() == null) go.AddComponent<AuntSaengAnimator>();
            var so = new SerializedObject(go.GetComponent<AuntSaengAnimator>());
            so.FindProperty("animator").objectReferenceValue = anim;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            int tris = go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Sum(r => r.sharedMesh != null ? r.sharedMesh.triangles.Length / 3 : 0);
            Debug.Log($"[ป้าแสง] Prefab {PrefabPath} · สูง {height:0.00} ม. · {tris} สามเหลี่ยม · ท่า: " +
                      string.Join(", ", controller.parameters.Select(p => p.name).Prepend("Idle")));
            return saved;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    private static Bounds Bounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // ---------- วางในฉาก ----------
    private static void PlaceInScene(Scene scene, GameObject prefab)
    {
        Transform[] all = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform npc = all.FirstOrDefault(t => t.name == "NPC_Mission_M1");
        Transform marker = all.FirstOrDefault(t => t.name == "NPC_GreetingPosition");
        if (npc == null || marker == null)
        {
            Debug.LogError("[ป้าแสง] ไม่พบ NPC_Mission_M1 หรือ NPC_GreetingPosition ในฉาก · วาง Prefab เองได้");
            return;
        }

        foreach (Transform old in all.Where(t => t != null && t.name == "AuntSaeng" && PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) != null).ToList())
        {
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        // ซ่อนกล่องตำแหน่งเดิม (ไม่ลบ ใช้เป็นตัวบอกตำแหน่ง)
        var markerRenderer = marker.GetComponent<Renderer>();
        Vector3 markerCenter = markerRenderer != null ? markerRenderer.bounds.center : marker.position;
        float markerBottom = markerRenderer != null ? markerRenderer.bounds.min.y : marker.position.y;
        if (markerRenderer != null) { Undo.RecordObject(markerRenderer, "ซ่อนกล่องป้าแสงเดิม"); markerRenderer.enabled = false; }
        foreach (Collider c in marker.GetComponents<Collider>()) { Undo.RecordObject(c, "ปิด Collider กล่องป้าแสงเดิม"); c.enabled = false; }
        Physics.SyncTransforms();

        float ground = Physics.Raycast(new Vector3(markerCenter.x, markerBottom + 1.5f, markerCenter.z), Vector3.down, out RaycastHit hit, 3f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? hit.point.y : markerBottom;

        var aunt = (GameObject)PrefabUtility.InstantiatePrefab(prefab, npc);
        Undo.RegisterCreatedObjectUndo(aunt, "วางป้าแสง");
        aunt.name = "AuntSaeng";
        Vector3 pos = new Vector3(markerCenter.x, ground, markerCenter.z);

        // หันหน้าไปทางพื้นที่สนทนา (ผู้เล่นยืนตรงนั้นตอนรับภารกิจ)
        var zone = npc.GetComponentInChildren<MissionInteractionZone>(true);
        Vector3 look = zone != null ? zone.transform.position - pos : Vector3.forward;
        look.y = 0f;
        Quaternion rot = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look.normalized, Vector3.up) : Quaternion.identity;
        aunt.transform.SetPositionAndRotation(pos, rot);

        var animatorComp = aunt.GetComponent<AuntSaengAnimator>();
        var giver = npc.GetComponent<MissionGiver>();
        Wire(zone, "onPlayerEntered", animatorComp, animatorComp.PlayWave);
        Wire(giver, "onMissionRequested", animatorComp, animatorComp.PlayTalk);

        Debug.Log($"[ป้าแสง] วางแทนกล่อง NPC_GreetingPosition ที่ ({pos.x:0.00}, {pos.y:0.00}, {pos.z:0.00}) · ต่อ Event: เข้าพื้นที่ → Wave · ให้ภารกิจ → Talk");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ต่อ UnityEvent (private field) → เมธอดของป้าแสง · ลบตัวเดิมที่ชี้ AuntSaengAnimator ก่อน กันซ้ำ
    private static void Wire(UnityEngine.Object owner, string field, AuntSaengAnimator target, UnityAction action)
    {
        if (owner == null) { Debug.LogWarning($"[ป้าแสง] ไม่พบคอมโพเนนต์สำหรับ {field} · ข้าม"); return; }
        FieldInfo fi = owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (!(fi?.GetValue(owner) is UnityEvent ev)) { Debug.LogWarning($"[ป้าแสง] ไม่พบ UnityEvent {field} · ข้าม"); return; }
        Undo.RecordObject(owner, "ต่อ Event ป้าแสง");
        for (int i = ev.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            if (ev.GetPersistentTarget(i) == null || ev.GetPersistentTarget(i) is AuntSaengAnimator) UnityEventTools.RemovePersistentListener(ev, i);
        }
        UnityEventTools.AddPersistentListener(ev, action);
        EditorUtility.SetDirty(owner);
    }
}
