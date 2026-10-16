using System.IO;
using System.Text;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Hands;

public static class M1CustomHandsEditor
{
    private const string Folder = "Assets/Gameplay/Art/Hands";

    [MenuItem("Tools/Cookverse Thai/M1/Apply Custom Hands")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("หยุด Play ก่อนตั้งค่ามือ");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/GameplayTest_M1.unity" || scene.isDirty)
            throw new InvalidOperationException("เปิดและบันทึก GameplayTest_M1 ก่อนตั้งค่ามือ");
        var visualizer = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MonoBehaviour>(true))
            .First(c => c != null && c.GetType().Name == "HandVisualizer");
        var materialPath = Folder + "/M1Hands.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/T_VRHands_Base_color.png"));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.25f);
        EditorUtility.SetDirty(material);
        var left = Build(Handedness.Left, material);
        var right = Build(Handedness.Right, material);
        // Keep the original visualizer and gesture rig functioning, but hide their meshes.
        var serialized = new SerializedObject(visualizer);
        serialized.FindProperty("m_DrawMeshes").boolValue = false;
        serialized.ApplyModifiedProperties();
        foreach (var prefab in new[] { left, right })
        {
            var existing = visualizer.transform.parent.Find(prefab.name);
            if (existing == null)
                PrefabUtility.InstantiatePrefab(prefab, visualizer.transform.parent);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(visualizer);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[มือ M1] เชื่อมโมเดลมือซ้าย–ขวาแล้ว ตรวจโครงกระดูกและท่าเริ่มต้นผ่าน ต้องทดสอบการขยับด้วย Simulator หรือแว่นต่อ");
    }

    private static GameObject Build(Handedness side, Material material)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Hands VR.fbx");
        var instance = UnityEngine.Object.Instantiate(model);
        instance.name = "M1Custom" + side + "Hand";
        try
        {
            foreach (var animator in instance.GetComponentsInChildren<Animator>()) UnityEngine.Object.DestroyImmediate(animator);
            var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
            var mesh = UnityEngine.Object.Instantiate(renderer.sharedMesh);
            mesh.name = instance.name + "Mesh";
            var weights = mesh.boneWeights;
            var bones = renderer.bones;
            var prefix = "J_" + side;
            bool Belongs(int vertex)
            {
                var w = weights[vertex];
                float Sum(bool wanted)
                {
                    float result = 0;
                    int[] ids = { w.boneIndex0, w.boneIndex1, w.boneIndex2, w.boneIndex3 };
                    float[] values = { w.weight0, w.weight1, w.weight2, w.weight3 };
                    for (int i = 0; i < 4; i++)
                        if (bones[ids[i]].name.StartsWith(prefix) == wanted) result += values[i];
                    return result;
                }
                return Sum(true) > Sum(false);
            }
            var triangles = new List<int>();
            var source = mesh.triangles;
            for (int i = 0; i < source.Length; i += 3)
            {
                bool a = Belongs(source[i]), b = Belongs(source[i + 1]), c = Belongs(source[i + 2]);
                if (a != b || b != c) throw new InvalidOperationException("พบหน้าตัดที่เชื่อมมือสองข้าง ต้องตรวจโมเดลก่อน");
                if (a) triangles.AddRange(new[] { source[i], source[i + 1], source[i + 2] });
            }
            if (triangles.Count == 0) throw new InvalidOperationException("ไม่พบผิวโมเดลมือ " + side);
            mesh.subMeshCount = 1;
            mesh.SetTriangles(triangles, 0);
            var meshPath = Folder + "/" + mesh.name + ".asset";
            var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (savedMesh == null) { AssetDatabase.CreateAsset(mesh, meshPath); savedMesh = mesh; }
            else { EditorUtility.CopySerialized(mesh, savedMesh); UnityEngine.Object.DestroyImmediate(mesh); }
            renderer.sharedMesh = savedMesh;
            renderer.sharedMaterial = material;
            renderer.updateWhenOffscreen = true;
            var driver = instance.AddComponent<M1CustomHandVisual>();
            driver.handedness = side;
            driver.handRenderer = renderer;
            var transforms = instance.GetComponentsInChildren<Transform>().ToDictionary(t => t.name);
            var refs = new List<(Transform bone, XRHandJointID joint, XRHandJointID next)>();
            refs.Add((transforms[prefix], XRHandJointID.Wrist, XRHandJointID.MiddleProximal));
            refs.Add((transforms[prefix + "_Hand"], XRHandJointID.Wrist, XRHandJointID.MiddleProximal));
            var fingers = new[] { "Thumb", "Index", "Middle", "Ring", "Pinky" };
            var joints = new[] {
                new[] { XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip },
                new[] { XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip },
                new[] { XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip },
                new[] { XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip },
                new[] { XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip }
            };
            var positions = new Vector3[32];
            positions[(int)XRHandJointID.Wrist] = transforms[prefix + "_Hand"].position;
            for (int f = 0; f < fingers.Length; f++)
                for (int n = 0; n < 4; n++)
                {
                    var bone = transforms[prefix + "_Hand" + fingers[f] + (n + 1)];
                    var joint = joints[f][n];
                    positions[(int)joint] = bone.position;
                    refs.Add((bone, joint, joints[f][n == 3 ? 2 : n + 1]));
                }
            var normal = M1CustomHandVisual.PalmNormal(positions);
            driver.bindings = refs.Select(r => {
                var frame = M1CustomHandVisual.Frame(positions, r.joint, r.next, normal);
                return new M1CustomHandVisual.BoneBinding {
                    bone = r.bone, joint = r.joint, nextJoint = r.next,
                    rotationOffset = Quaternion.Inverse(frame) * r.bone.rotation,
                    positionOffset = Quaternion.Inverse(frame) * (r.bone.position - positions[(int)r.joint])
                };
            }).ToArray();
            var originalPositions = refs.Select(r => r.bone.position).ToArray();
            var originalRotations = refs.Select(r => r.bone.rotation).ToArray();
            if (!driver.ApplyJointPositions(positions)) throw new InvalidOperationException("ท่าอ้างอิงมือไม่สมบูรณ์");
            for (int i = 0; i < refs.Count; i++)
                if (Vector3.Distance(refs[i].bone.position, originalPositions[i]) > 0.0001f ||
                    Quaternion.Angle(refs[i].bone.rotation, originalRotations[i]) > 0.1f)
                    throw new InvalidOperationException("การตรวจท่าอ้างอิงไม่ผ่าน: " + refs[i].bone.name);
            renderer.enabled = false;
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, Folder + "/" + instance.name + ".prefab");
            Debug.Log($"[มือ M1] {side}: กระดูก {refs.Count} จุด ผิว {triangles.Count / 3} สามเหลี่ยม ตรวจท่าอ้างอิงผ่าน");
            return prefab;
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }

    [MenuItem("Tools/Cookverse Thai/M1/Inspect Custom Hands")]
    public static void Inspect()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gameplay/Art/Hands/Hands VR.fbx");
        var report = new StringBuilder();
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
            report.AppendLine($"{AnimationUtility.CalculateTransformPath(t, model.transform)} | pos={t.position.ToString("F5")} rot={t.rotation.eulerAngles} scale={t.lossyScale}");
        foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            report.AppendLine($"MESH {r.name} vertices={r.sharedMesh.vertexCount} bounds={r.bounds} root={r.rootBone?.name}");
            foreach (var b in r.bones) report.AppendLine("BONE " + b.name);
            foreach (var m in r.sharedMaterials) report.AppendLine("MATERIAL " + m?.name);
        }
        File.WriteAllText("/private/tmp/m1-custom-hands.txt", report.ToString());
        Debug.Log("[มือ M1] ตรวจโครงสร้างโมเดลมือแล้ว");
    }
}
