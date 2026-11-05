using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// ลงสีบ้านเรือนไทยแบบ Blockout (ไม้สัก หลังคาดินเผา พื้นหญ้า เครื่องครัว) · เปลี่ยนเฉพาะวัสดุ ไม่แตะ Collider/สคริปต์/ตำแหน่ง
// - ใช้ได้ทั้ง GameplayTest_M1 และ MainVR (รันแยกทีละฉาก)
// - เปลี่ยนเฉพาะ Renderer ใต้ Enivronment/House_Blockout, Roof_Blockout, Ground_Blockout ที่ยังใช้วัสดุตั้งต้น
//   (วัสดุจาก Packages หรือ "Lit White") หรือวัสดุ House_* ของเครื่องมือนี้ (รันซ้ำได้) · วัสดุที่ตั้งเองไว้แล้ว (เตา ฝา ตู้เย็น ฯลฯ) ไม่แตะ
// - ไม่แตะกล่องจุดเกม (ชื่อมี Point / Zone / Spawn / Position) เพื่อให้ยังเห็นจุดวางของตามเดิม
// - วัสดุอยู่ที่ Assets/Gameplay/Art/House/House_*.mat (URP Lit สีล้วน ไม่มี texture → ไม่หนักบนเว็บ)
public static class M1HousePaintEditor
{
    private const string Folder = "Assets/Gameplay/Art/House";

    // ชื่อวัสดุ → สี
    private static readonly Dictionary<string, Color> Palette = new()
    {
        { "WallTeak", new Color(0.55f, 0.35f, 0.20f) },
        { "PostDark", new Color(0.32f, 0.20f, 0.11f) },
        { "FloorWood", new Color(0.66f, 0.48f, 0.30f) },
        { "StairWood", new Color(0.50f, 0.33f, 0.20f) },
        { "Concrete", new Color(0.62f, 0.60f, 0.56f) },
        { "RoofTile", new Color(0.62f, 0.27f, 0.17f) },
        { "Ceiling", new Color(0.72f, 0.56f, 0.38f) },
        { "Grass", new Color(0.38f, 0.50f, 0.27f) },
        { "CounterWood", new Color(0.62f, 0.45f, 0.28f) },
        { "LightWood", new Color(0.80f, 0.65f, 0.42f) },
        { "Bamboo", new Color(0.76f, 0.66f, 0.40f) },
        { "Rattan", new Color(0.72f, 0.56f, 0.32f) },
        { "DarkMetal", new Color(0.22f, 0.22f, 0.24f) },
        { "Steel", new Color(0.72f, 0.74f, 0.76f) },
        { "FridgeWhite", new Color(0.90f, 0.90f, 0.87f) },
        { "Porcelain", new Color(0.90f, 0.92f, 0.95f) },
        { "Stone", new Color(0.45f, 0.44f, 0.42f) },
        { "Mat", new Color(0.80f, 0.68f, 0.45f) },
        { "Cushion", new Color(0.70f, 0.22f, 0.18f) },
        { "Garlic", new Color(0.92f, 0.88f, 0.80f) },
        { "Shallot", new Color(0.62f, 0.25f, 0.30f) },
        { "Jar", new Color(0.85f, 0.82f, 0.70f) },
        { "Lid", new Color(0.70f, 0.20f, 0.15f) },
        { "Sauce", new Color(0.45f, 0.26f, 0.10f) },
        { "Oil", new Color(0.88f, 0.75f, 0.30f) },
    };

    private static readonly string[] Roots = { "House_Blockout", "Roof_Blockout", "Ground_Blockout" };
    private static readonly string[] MarkerWords = { "Point", "Zone", "Spawn", "Position" };

    // กฎเรียงตามลำดับ: (คำในเส้นทาง/ชื่อ, วัสดุ) · ตรวจชื่อวัตถุก่อน แล้วค่อยเส้นทาง
    private static string Pick(string name, string path)
    {
        bool N(params string[] k) => k.Any(x => name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
        bool P(params string[] k) => k.Any(x => path.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);

        if (N("Ground_Blockout")) return "Grass";
        if (N("Roof_Main")) return "RoofTile";
        if (N("Roof_Ceiling")) return "Ceiling";
        if (P("/Back_Walls")) return "WallTeak";
        if (P("/Foundation")) return "Concrete";
        if (P("/Front_Stairs")) return "StairWood";
        if (N("HouseFloor")) return "FloorWood";
        if (P("/Main_Posts", "/Railings")) return "PostDark";

        // ครัว
        if (N("Garlic_")) return "Garlic";
        if (N("Onion_")) return "Shallot";
        if (N("Basket")) return "Rattan";
        if (N("JarBody")) return "Jar";
        if (N("Lid", "Cap")) return "Lid";
        if (N("BottleBody", "BottleNeck")) return P("CookingOil") ? "Oil" : "Sauce";
        if (name.Trim() == "Fridge") return "FridgeWhite";
        if (N("Bamboo")) return "Bamboo";
        if (N("Hook_", "Pan_", "Wok_", "Pot_", "Ladle_", "Spatula_", "Knife")) return "DarkMetal";
        if (N("Sink", "Faucer", "Faucet")) return "Steel";
        if (N("CuttingBoard", "ServingTray")) return "LightWood";
        if (N("Mortar")) return "Stone";
        if (N("Plates", "ServingBowl")) return "Porcelain";
        if (N("Floor_Mat")) return "Mat";
        if (N("Cushion")) return "Cushion";
        if (N("Low_Table")) return "PostDark";
        if (N("Counter", "Couter", "Table", "Shelf", "ShelfBoard", "RackBar")) return "CounterWood";
        return null;
    }

    [MenuItem("Tools/Cookverse Thai/M1/Paint House (Thai Wood)")]
    public static void Paint()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("หยุด Play ก่อน");
        Scene scene = SceneManager.GetActiveScene();

        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Gameplay/Art", "House");
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        var mats = new Dictionary<string, Material>();
        foreach (var kv in Palette)
        {
            string path = $"{Folder}/House_{kv.Key}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(lit) { name = $"House_{kv.Key}" }; AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", kv.Value);
            m.SetFloat("_Smoothness", kv.Key is "Steel" or "Porcelain" or "FridgeWhite" or "DarkMetal" ? 0.55f : 0.15f);
            m.SetFloat("_Metallic", kv.Key is "Steel" or "DarkMetal" ? 0.6f : 0f);
            EditorUtility.SetDirty(m);
            mats[kv.Key] = m;
        }
        AssetDatabase.SaveAssets();

        Transform env = scene.GetRootGameObjects().Select(g => g.transform).FirstOrDefault(t => t.name.Trim() == "Enivronment");
        if (env == null) { Debug.LogError("[ลงสีบ้าน] ไม่พบ Enivronment ในฉากนี้"); return; }

        var counts = new Dictionary<string, int>();
        int skippedCustom = 0, skippedMarker = 0, unmatched = 0;
        var unmatchedNames = new List<string>();
        foreach (string rootName in Roots)
        {
            Transform root = env.Find(rootName);
            if (root == null) continue;
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                string name = r.name.Trim();
                string path = PathOf(r.transform, env);
                if (MarkerWords.Any(w => name.Contains(w))) { skippedMarker++; continue; }
                Material current = r.sharedMaterial;
                if (!IsReplaceable(current)) { skippedCustom++; continue; }
                string key = Pick(name, path);
                if (key == null) { unmatched++; if (unmatchedNames.Count < 12) unmatchedNames.Add(name); continue; }

                Undo.RecordObject(r, "ลงสีบ้าน");
                r.sharedMaterials = Enumerable.Repeat(mats[key], Mathf.Max(1, r.sharedMaterials.Length)).ToArray();
                counts[key] = counts.TryGetValue(key, out int c) ? c + 1 : 1;
            }
        }

        Debug.Log($"[ลงสีบ้าน] ฉาก {scene.name}: เปลี่ยน {counts.Values.Sum()} ชิ้น ({string.Join(", ", counts.Select(kv => $"{kv.Key} {kv.Value}"))}) · " +
                  $"ข้ามจุดเกม {skippedMarker} · ข้ามวัสดุที่ตั้งเองแล้ว {skippedCustom} · ไม่เข้ากฎ {unmatched}" +
                  (unmatchedNames.Count > 0 ? $" ({string.Join(", ", unmatchedNames)})" : ""));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static bool IsReplaceable(Material m)
    {
        if (m == null) return true;
        string path = AssetDatabase.GetAssetPath(m);
        return path.StartsWith("Packages/") || string.IsNullOrEmpty(path) || m.name == "Lit White" || path.StartsWith(Folder + "/House_");
    }

    private static string PathOf(Transform t, Transform stop)
    {
        var parts = new List<string>();
        for (Transform x = t; x != null && x != stop; x = x.parent) parts.Add(x.name.Trim());
        parts.Reverse();
        return "/" + string.Join("/", parts);
    }
}
