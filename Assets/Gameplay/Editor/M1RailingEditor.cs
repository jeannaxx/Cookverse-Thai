using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ราวระเบียงลูกกรงไม้แบบบ้านเรือนไทย แทนแท่งราวทึบ (Enivronment/House_Blockout/Railings/Rail_*)
// - ต่อแท่งราว: สร้าง Mesh ราวบน (มือจับ) + ราวรอง + ราวล่าง + ลูกกรงห่าง ~11 ซม. ตามความยาว/ความสูงของแท่งเดิม
// - ซ่อน MeshRenderer ของแท่งเดิม แต่คง Collider ไว้ → ผู้เล่นไม่ลอดช่องลูกกรง/ไม่ตกระเบียง (การชนเหมือนเดิมทุกอย่าง)
// - วาง Balustrade_<ชื่อแท่ง> เป็นพี่น้องในกลุ่ม Railings · ไม่มี Collider · Static
// - วัสดุ House_PostDark (จาก Paint House) · ~300 สามเหลี่ยมต่อแท่ง
// ใช้ได้ทั้ง GameplayTest_M1 และ MainVR · รันซ้ำได้ (ลบ Balustrade เดิมแล้วสร้างใหม่)
public static class M1RailingEditor
{
    private const string Folder = "Assets/Gameplay/Art/House";
    private const string MaterialPath = Folder + "/House_PostDark.mat";

    private const float TopRailH = 0.06f, TopRailD = 0.12f;      // มือจับด้านบน
    private const float SubRailH = 0.04f, SubRailD = 0.06f;      // ราวรองใต้มือจับ
    private const float SubRailGap = 0.10f;                       // ระยะจากมือจับลงมาถึงราวรอง
    private const float BottomRailH = 0.06f, BottomRailD = 0.08f, BottomLift = 0.05f;
    private const float BalusterW = 0.045f, BalusterSpacing = 0.11f;

    [MenuItem("Tools/Cookverse Thai/M1/Build Thai Railings")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("หยุด Play ก่อน");
        Scene scene = SceneManager.GetActiveScene();

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null) { Debug.LogError("[ราวระเบียง] ยังไม่มี House_PostDark · กด Paint House (Thai Wood) ก่อน"); return; }

        Transform railings = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name.Trim() == "Railings" && t.parent != null && t.parent.name.Trim() == "House_Blockout");
        if (railings == null) { Debug.LogError("[ราวระเบียง] ไม่พบ House_Blockout/Railings ในฉากนี้"); return; }

        foreach (Transform old in railings.Cast<Transform>().Where(t => t.name.StartsWith("Balustrade_")).ToList())
        {
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        int made = 0, tris = 0;
        foreach (Transform rail in railings.Cast<Transform>().Where(t => t.name.StartsWith("Rail_")).ToList())
        {
            var col = rail.GetComponent<Collider>();
            var ren = rail.GetComponent<MeshRenderer>();
            if (col == null && ren == null) continue;
            Bounds b = col != null && col.enabled ? col.bounds : ren.bounds;
            bool alongZ = b.size.z > b.size.x;
            float length = alongZ ? b.size.z : b.size.x;
            float height = b.size.y;

            Mesh mesh = SaveMesh(length, height);
            var go = new GameObject($"Balustrade_{rail.name.Trim()}");
            Undo.RegisterCreatedObjectUndo(go, "สร้างราวลูกกรง");
            go.transform.SetParent(railings, false);
            go.transform.SetPositionAndRotation(new Vector3(b.center.x, b.min.y, b.center.z), Quaternion.Euler(0f, alongZ ? 90f : 0f, 0f));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            go.isStatic = true;

            if (ren != null) { Undo.RecordObject(ren, "ซ่อนแท่งราวเดิม"); ren.enabled = false; }
            made++;
            tris += mesh.triangles.Length / 3;
        }

        Debug.Log($"[ราวระเบียง] ฉาก {scene.name}: สร้างราวลูกกรง {made} ช่วง · รวม {tris} สามเหลี่ยม · Collider แท่งเดิมยังกันตกเหมือนเดิม");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // จุดกำเนิด = กลางความยาว ที่ระดับพื้นของราว · แกนยาว = X · สูงขึ้นตาม Y
    private static Mesh SaveMesh(float length, float height)
    {
        string name = $"M1_Balustrade_{Mathf.RoundToInt(length * 100)}x{Mathf.RoundToInt(height * 100)}";
        string path = $"{Folder}/{name}.asset";
        var verts = new List<Vector3>();
        var normals = new List<Vector3>();
        var tris = new List<int>();

        float topY = height - TopRailH / 2f;
        Box(verts, normals, tris, new Vector3(0, topY, 0), new Vector3(length, TopRailH, TopRailD));
        float subY = height - TopRailH - SubRailGap - SubRailH / 2f;
        Box(verts, normals, tris, new Vector3(0, subY, 0), new Vector3(length, SubRailH, SubRailD));
        float bottomY = BottomLift + BottomRailH / 2f;
        Box(verts, normals, tris, new Vector3(0, bottomY, 0), new Vector3(length, BottomRailH, BottomRailD));

        // ลูกกรงระหว่างราวล่างถึงราวรอง · ช่วงสั้นระหว่างราวรองถึงมือจับใช้ลูกกรงสั้นถี่เท่ากัน
        float y0 = BottomLift + BottomRailH, y1 = subY - SubRailH / 2f;
        float y2 = subY + SubRailH / 2f, y3 = height - TopRailH;
        int count = Mathf.Max(2, Mathf.FloorToInt((length - BalusterW) / BalusterSpacing));
        float step = (length - BalusterW) / count;
        for (int i = 0; i <= count; i++)
        {
            float x = -length / 2f + BalusterW / 2f + i * step;
            Box(verts, normals, tris, new Vector3(x, (y0 + y1) / 2f, 0), new Vector3(BalusterW, y1 - y0, BalusterW));
            if (i % 2 == 0 && y3 - y2 > 0.02f)
                Box(verts, normals, tris, new Vector3(x, (y2 + y3) / 2f, 0), new Vector3(BalusterW * 0.8f, y3 - y2, BalusterW * 0.8f));
        }

        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool isNew = mesh == null;
        if (isNew) mesh = new Mesh { name = name };
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetNormals(normals);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
        if (isNew) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
        AssetDatabase.SaveAssets();
        return mesh;
    }

    // กล่อง 24 จุด (normal แยกต่อหน้า ให้ขอบคม)
    private static void Box(List<Vector3> v, List<Vector3> n, List<int> t, Vector3 c, Vector3 size)
    {
        Vector3 h = size / 2f;
        Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (Vector3 d in dirs)
        {
            Vector3 u = Mathf.Abs(d.y) > 0.5f ? Vector3.right : Vector3.up;
            Vector3 r = Vector3.Cross(u, d);
            Vector3 center = c + Vector3.Scale(d, h);
            Vector3 ru = Vector3.Scale(r, h), uu = Vector3.Scale(u, h);
            int o = v.Count;
            v.Add(center - ru - uu); v.Add(center - ru + uu); v.Add(center + ru + uu); v.Add(center + ru - uu);
            for (int k = 0; k < 4; k++) n.Add(d);
            // หน้าหันออกตาม d (Unity วนตามเข็มนาฬิกา)
            if (Vector3.Dot(Vector3.Cross(v[o + 1] - v[o], v[o + 2] - v[o]), d) > 0) { t.Add(o); t.Add(o + 1); t.Add(o + 2); t.Add(o); t.Add(o + 2); t.Add(o + 3); }
            else { t.Add(o); t.Add(o + 2); t.Add(o + 1); t.Add(o); t.Add(o + 3); t.Add(o + 2); }
        }
    }
}
