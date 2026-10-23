using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// ใช้บล็อกที่จัดไว้ในสวนเป็นวัตถุดิบที่หยิบได้ใน GameplayTest_M1
// - กะเพรา: ต้นกะเพราบล็อก 6 ต้นบนแปลง หยิบได้ทุกต้น (เด็ดต้นไหนก็นับ 1 กำ)
// - พริก: บล็อกสีแดง Chili_Box บนแปลงพริก + เพิ่มอีก 2 ก้อนแบบเดียวกัน รวม 3 ก้อน
// - ลบของเดิมที่วางผิด: Krapow_Bunch_Collectible และ Chili_Collectible 3 แผ่น
// ค่าการหยิบคัดลอกจาก Krapow.prefab / Chili.prefab ให้เหมือนวัตถุดิบอื่น
// รันซ้ำได้: ของที่ทำแล้วจะข้าม
public static class M1GardenItemsEditor
{
    private const string ScenePath = "Assets/Scenes/GameplayTest_M1.unity";
    private const string IngredientFolder = "Assets/Gameplay/Prefabs/Ingredients";
    private const string KrapowPrefabPath = IngredientFolder + "/Krapow.prefab";
    private const string ChiliPrefabPath = IngredientFolder + "/Chili.prefab";
    private const string ChiliBlockName = "Chili_Box";
    private const int ChiliCount = 3;

    // ระยะห่างของบล็อกพริกบนแปลง (ตามแนวยาวของแปลง)
    private const float ChiliSpacing = 0.32f;

    [MenuItem("Tools/Cookverse Thai/M1/Fix Krapow and Chili Items")]
    public static void Fix()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (EditorApplication.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("เปิด GameplayTest_M1 และหยุด Play ก่อน");
        }

        GameObject krapowTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(KrapowPrefabPath);
        GameObject chiliTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(ChiliPrefabPath);
        Transform karpao = FindInScene(scene, "Karpao", "Garden_Blockout");
        Transform chiliBlock = FindInScene(scene, ChiliBlockName, "Garden_Blockout");

        if (krapowTemplate == null || chiliTemplate == null ||
            karpao == null || chiliBlock == null)
        {
            Debug.LogError(
                "[ปรับวัตถุดิบ] ไม่พบ Krapow.prefab, Chili.prefab, กลุ่มต้นกะเพรา Karpao " +
                "หรือบล็อก Chili_Box จึงยกเลิก");
            return;
        }

        int removed = RemoveOldCollectibles(scene);
        int plants = MakeKrapowPlantsGrabbable(karpao, krapowTemplate);
        int chilies = MakeChiliBlocks(chiliBlock, chiliTemplate);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            $"[ปรับวัตถุดิบ] เสร็จแล้ว: ลบของเดิม {removed} ชิ้น · " +
            $"ต้นกะเพราหยิบได้ {plants} ต้น · บล็อกพริกสีแดง {chilies} ก้อน");
    }

    // ลบเฉพาะกะเพรา/พริกที่อยู่ใต้ GameplayObjects (ของเดิมที่วางผิด)
    private static int RemoveOldCollectibles(Scene scene)
    {
        IngredientIdentity[] old = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<IngredientIdentity>(true))
            .Where(i =>
                (i.IngredientId == ObjectiveManager.KrapowLeafIngredientId ||
                 i.IngredientId == ObjectiveManager.ChiliIngredientId) &&
                i.transform.parent != null &&
                i.transform.parent.name.Trim() == "GameplayObjects")
            .ToArray();

        foreach (IngredientIdentity item in old)
        {
            Undo.DestroyObjectImmediate(item.gameObject);
        }

        return old.Length;
    }

    private static int MakeKrapowPlantsGrabbable(Transform karpao, GameObject template)
    {
        int count = 0;

        foreach (Transform plant in karpao.Cast<Transform>().ToArray())
        {
            if (plant.GetComponent<IngredientIdentity>() == null)
            {
                Undo.RegisterFullObjectHierarchyUndo(plant.gameObject, "ทำต้นกะเพราให้หยิบได้");

                // จุดหมุนเดิมอยู่ใกล้ 0,0,0 ห่างจากตัวต้นหลายเมตร ย้ายมากลางต้นก่อน
                CenterPivotOnChildren(plant);
                AddIngredientComponents(plant.gameObject, template);
            }

            count++;
        }

        return count;
    }

    private static int MakeChiliBlocks(Transform first, GameObject template)
    {
        if (first.GetComponent<IngredientIdentity>() == null)
        {
            Undo.RegisterFullObjectHierarchyUndo(first.gameObject, "ทำบล็อกพริกให้หยิบได้");
            AddIngredientComponents(first.gameObject, template);
        }

        int count = 1;

        // เพิ่มบล็อกพริกแบบเดียวกันอีก 2 ก้อน วางเรียงตามแนวยาวของแปลง
        for (int n = 1; n < ChiliCount; n++)
        {
            string name = $"{ChiliBlockName}_{n + 1}";

            if (first.parent.Find(name) == null)
            {
                GameObject copy = UnityEngine.Object.Instantiate(first.gameObject, first.parent);
                Undo.RegisterCreatedObjectUndo(copy, "เพิ่มบล็อกพริก");
                copy.name = name;
                float side = n == 1 ? -1f : 1f;
                copy.transform.position = first.position + new Vector3(0f, 0f, side * ChiliSpacing);
                ResetIdentity(copy);
            }

            count++;
        }

        return count;
    }

    private static void AddIngredientComponents(GameObject target, GameObject template)
    {
        Component[] components =
        {
            template.GetComponent<Rigidbody>(),
            template.GetComponent<XRGrabInteractable>(),
            template.GetComponent<IngredientIdentity>()
        };

        foreach (Component source in components)
        {
            if (source != null && target.GetComponent(source.GetType()) == null)
            {
                ComponentUtility.CopyComponent(source);
                ComponentUtility.PasteComponentAsNew(target);
            }
        }

        // ใบของต้นที่อยู่ติดกันซ้อนกัน ถ้าเป็น Rigidbody ปกติ (ไม่มีแรงโน้มถ่วงและแรงหน่วง)
        // จะผลักกันกระเด็นตอนเริ่มเล่น จึงให้อยู่นิ่งจนกว่าจะถูกหยิบ
        // XRGrabInteractable คืนค่านี้หลังปล่อยมือ ของจึงลอยค้างตรงที่วาง
        target.GetComponent<Rigidbody>().isKinematic = true;
        ResetIdentity(target);
    }

    // แต่ละชิ้นต้องเริ่มที่ "ยังไม่เก็บ" และสร้าง Instance ID ของตัวเองตอนเล่น
    private static void ResetIdentity(GameObject target)
    {
        var identity = new SerializedObject(target.GetComponent<IngredientIdentity>());
        identity.FindProperty("instanceId").stringValue = string.Empty;
        identity.FindProperty("currentState").enumValueIndex = (int)IngredientState.Raw;
        identity.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CenterPivotOnChildren(Transform root)
    {
        Transform[] children = root.Cast<Transform>().ToArray();

        if (children.Length == 0)
        {
            return;
        }

        Vector3[] worldPositions = children.Select(c => c.position).ToArray();
        Vector3 center = Vector3.zero;

        foreach (Vector3 position in worldPositions)
        {
            center += position;
        }

        root.position = center / worldPositions.Length;

        for (int i = 0; i < children.Length; i++)
        {
            children[i].position = worldPositions[i];
        }
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
