using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ทำให้บันไดหน้าบ้านเดินขึ้นด้วยจอยได้ลื่น ๆ
// บันไดเดิมมี 3 ขั้น สูงรวม 1 ม. ในระยะราว 40 ซม. (ชันเกิน 45° ที่ระบบเดินรับได้)
// และแต่ละขั้นเป็นแผ่นบางลอย ทำให้ตัวผู้เล่นติดหรือจมลงไป
// เครื่องมือนี้:
// - วางทางลาดล่องหน (BoxCollider ไม่มีภาพ) จากพื้นดินหน้าบันไดขึ้นไปถึงพื้นบ้าน ชันไม่เกิน ~32°
// - ปิด Collider ของ Step_01–03 (ภาพบันไดยังอยู่เหมือนเดิม) เพื่อไม่ให้ขอบขั้นโผล่มาขวาง
// กดซ้ำได้ (ลบทางลาดเดิมแล้วสร้างใหม่) และ Undo ได้ (Cmd+Z)
public static class M1StairsRampEditor
{
    private const string RampName = "Stairs_WalkRamp";
    private const float ExtraRun = 0.8f;      // ยื่นทางลาดออกไปหน้าขั้นล่างสุด เพื่อให้ลาดน้อยลง
    private const float FloorOverlap = 0.25f; // ทางลาดซ้อนเข้าไปในพื้นบ้าน ไม่ให้มีร่อง
    private const float Thickness = 0.1f;

    [MenuItem("Tools/Cookverse Thai/M1/Fix Front Stairs (Walk Ramp)")]
    public static void Fix()
    {
        Scene scene = SceneManager.GetActiveScene();
        var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        Transform Find(string name) => all.FirstOrDefault(t => t.name.Trim() == name);

        Transform stairs = Find("Front_Stairs");
        Transform floor = Find("HouseFloor");
        Transform ground = Find("Ground_Blockout");
        var steps = new[] { Find("Step_01"), Find("Step_02"), Find("Step_03") }.Where(t => t != null).ToArray();
        if (stairs == null || floor == null || steps.Length == 0)
        {
            EditorUtility.DisplayDialog("Fix Front Stairs", "ไม่พบ Front_Stairs / HouseFloor / Step_0x ในฉากที่เปิดอยู่", "OK");
            return;
        }

        Bounds floorB = WorldBounds(floor);
        Bounds stepsB = WorldBounds(steps[0]);
        foreach (var s in steps.Skip(1)) stepsB.Encapsulate(WorldBounds(s));
        float groundTop = ground != null ? WorldBounds(ground).max.y : 0f;
        float floorTop = floorB.max.y;

        // หาว่าบันไดยื่นออกไปทางแกนไหนของพื้นบ้าน (x หรือ z, บวกหรือลบ)
        Vector3 offset = stepsB.center - floorB.center;
        bool alongZ = Mathf.Abs(offset.z) - floorB.extents.z > Mathf.Abs(offset.x) - floorB.extents.x;
        Vector3 axis = alongZ ? new Vector3(0, 0, Mathf.Sign(offset.z)) : new Vector3(Mathf.Sign(offset.x), 0, 0);

        float edge = Vector3.Dot(floorB.center, axis) + (alongZ ? floorB.extents.z : floorB.extents.x);
        float stepsOuter = Vector3.Dot(stepsB.center, axis) + (alongZ ? stepsB.extents.z : stepsB.extents.x);
        Vector3 across = stepsB.center - Vector3.Dot(stepsB.center, axis) * axis; // ตำแหน่งกลางบันไดตามแนวขวาง
        across.y = 0;
        float width = (alongZ ? stepsB.size.x : stepsB.size.z) + 0.1f;

        Vector3 bottom = across + axis * (stepsOuter + ExtraRun) + Vector3.up * groundTop;
        Vector3 top = across + axis * (edge - FloorOverlap) + Vector3.up * floorTop;
        Vector3 up = top - bottom;
        float length = up.magnitude;
        float angle = Mathf.Atan2(floorTop - groundTop, (bottom - top).With(y: 0).magnitude) * Mathf.Rad2Deg;

        Undo.SetCurrentGroupName("Fix Front Stairs");
        int group = Undo.GetCurrentGroup();

        var old = stairs.Find(RampName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

        var ramp = new GameObject(RampName);
        Undo.RegisterCreatedObjectUndo(ramp, "Create ramp");
        Undo.SetTransformParent(ramp.transform, stairs, "Parent ramp");
        ramp.transform.SetPositionAndRotation((bottom + top) * 0.5f, Quaternion.LookRotation(up.normalized, Vector3.up));
        ramp.transform.localScale = Vector3.one;
        ramp.layer = steps[0].gameObject.layer;
        var box = Undo.AddComponent<BoxCollider>(ramp);
        box.size = new Vector3(width, Thickness, length);
        box.center = new Vector3(0, -Thickness * 0.5f, 0); // ผิวบนของกล่องตรงกับแนวทางลาดพอดี

        int disabled = 0;
        foreach (var s in steps)
            foreach (var c in s.GetComponents<Collider>())
            {
                if (!c.enabled) continue;
                Undo.RecordObject(c, "Disable step collider");
                c.enabled = false;
                disabled++;
            }

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = ramp;
        Debug.Log($"[Fix Front Stairs] สร้าง {RampName} ยาว {length:0.00} ม. กว้าง {width:0.00} ม. ชัน {angle:0}° " +
                  $"(ดิน y={groundTop:0.00} → พื้นบ้าน y={floorTop:0.00}) และปิด Collider ของขั้นบันได {disabled} ชิ้น · อย่าลืม Save Scene (Cmd+S)");
    }

    // ใช้ Renderer ของวัตถุนั้นเอง (ไม่รวมลูก) เพราะ Ground_Blockout อาจมีของอื่นซ้อนอยู่ข้างใน
    private static Bounds WorldBounds(Transform t)
    {
        var own = t.GetComponent<Renderer>();
        if (own != null) return own.bounds;
        var rs = t.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var x in rs.Skip(1)) b.Encapsulate(x.bounds);
        return b;
    }

    private static Vector3 With(this Vector3 v, float? x = null, float? y = null, float? z = null)
        => new Vector3(x ?? v.x, y ?? v.y, z ?? v.z);
}
