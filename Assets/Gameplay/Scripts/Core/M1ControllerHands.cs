using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

// มือตอนถือจอย: วางโมเดลมือ M1 กำรอบจอยทั้งสองข้าง · กดไก = นิ้วชี้งอ · กดกริป = นิ้วกลาง/นาง/ก้อยกำแน่น
// - ใช้โมเดลมือเดียวกับมือจับนิ้ว (M1CustomLeft/RightHand) แต่ปิดตัวขับข้อต่อ แล้วงอนิ้วเองจากค่าปุ่ม
// - มือเป็นลูกของ GameObject จอยที่ XRInputModalityManager สลับให้ → ตอนวางจอยแล้วใช้มือเปล่า มือชุดนี้หายเอง
// - ท่าวางมือคำนวณจากโครงกระดูก (ทิศนิ้ว + ฝั่งฝ่ามือ) · ปรับละเอียดระหว่าง Play ได้ที่ grip* ใน Inspector
public class M1ControllerHands : MonoBehaviour
{
    [Header("Hand Models (M1 custom hands)")]
    [SerializeField] private GameObject leftHandPrefab;
    [SerializeField] private GameObject rightHandPrefab;
    [Tooltip("ซ่อนโมเดลจอย เห็นแค่มือ (เส้นเลเซอร์และป้ายช่วยยังอยู่)")]
    [SerializeField] private bool hideControllerModel = true;

    [Header("ท่าจับ (พื้นที่ของจอยขวา · จอยซ้ายสะท้อนแกน X ให้เอง)")]
    [Tooltip("จุดกลางฝ่ามือเทียบกับจอย (เมตร) · +X = ด้านนอกของจอยขวา")]
    [SerializeField] private Vector3 gripPalmPosition = new(0.015f, -0.025f, -0.05f);
    [Tooltip("ทิศจากข้อมือไปโคนนิ้ว เทียบแนวหน้าจอย · ลบ = เงยขึ้นตามด้ามจอย (ข้อมืออยู่ล่าง-หลัง)")]
    [SerializeField] private float fingerPitch = -15f;
    [Tooltip("ความยาวมือจริง ข้อมือถึงปลายนิ้วกลาง (เมตร)")]
    [SerializeField] private float handLength = 0.15f;
    [SerializeField] private Vector3 extraRotation = Vector3.zero;

    [Header("องศาการงอนิ้ว (ต่อข้อ)")]
    [SerializeField] private float restCurl = 30f;     // กำจอยหลวม ๆ ตอนไม่กดอะไร
    [SerializeField] private float gripCurl = 62f;     // กดกริปสุด
    [SerializeField] private float indexRest = 12f;
    [SerializeField] private float indexPressed = 58f; // กดไกสุด
    [SerializeField] private float thumbCurl = 18f;
    [SerializeField, Min(1f)] private float curlSpeed = 14f;
    [Tooltip("ติ๊กถ้านิ้วงอไปทางหลังมือ (กลับทิศการงอ)")]
    [SerializeField] private bool flipCurl;

    private static readonly string[] Fingers = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

    private sealed class FingerBone
    {
        public Transform bone;
        public Quaternion rest;
        public Vector3 axis;   // แกนงอในพื้นที่ของกระดูกเอง
        public int finger;     // 0 thumb · 1 index · 2-4 อื่น
    }

    private sealed class Side
    {
        public bool left;
        public GameObject controller;
        public Transform root;
        public Quaternion handFrameLocal;   // (ทิศนิ้ว, ฝั่งฝ่ามือ) ในพื้นที่ root
        public Vector3 palmLocal;
        public readonly List<FingerBone> bones = new();
        public InputAction trigger, grip;
        public float triggerValue, gripValue;
    }

    private readonly Side[] sides = new Side[2];
    private XRInputModalityManager modality;

    public void Configure(GameObject left, GameObject right)
    {
        leftHandPrefab = left;
        rightHandPrefab = right;
    }

    // พักไว้ก่อน (ผู้ใช้ขอ 13 พ.ย.): กลับไปใช้โมเดลจอยแบบเดิมให้เล่นได้ · จะเปิดกลับ ตั้งเป็น true
    public static bool FeatureEnabled = false;

    private void Start()
    {
        if (!FeatureEnabled)
        {
            enabled = false;
            return;
        }

        modality = FindAnyObjectByType<XRInputModalityManager>();
        if (modality == null)
        {
            Debug.LogWarning("[มือถือจอย] ไม่พบ XRInputModalityManager จึงไม่สร้างมือ", this);
            enabled = false;
            return;
        }

        try
        {
            sides[0] = Build(true, modality.leftController, leftHandPrefab);
            sides[1] = Build(false, modality.rightController, rightHandPrefab);
        }
        catch (Exception e)
        {
            Debug.LogError($"[มือถือจอย] สร้างมือไม่สำเร็จ {e.GetType().Name}: {e.Message}", this);
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        foreach (Side s in sides)
        {
            if (s == null) continue;
            s.trigger?.Dispose();
            s.grip?.Dispose();
        }
    }

    private Side Build(bool left, GameObject controller, GameObject prefab)
    {
        if (controller == null || prefab == null) return null;
        string hand = left ? "LeftHand" : "RightHand";
        var side = new Side { left = left, controller = controller };

        if (hideControllerModel)
        {
            // เฉพาะตัวโมเดลจอย: ไม่ซ่อนเส้นเลเซอร์ (LineRenderer) ตัวหนังสือ/ป้าย (Canvas, TMP)
            foreach (Renderer r in controller.GetComponentsInChildren<Renderer>(true))
            {
                bool isModel = (r is MeshRenderer || r is SkinnedMeshRenderer) &&
                               r.GetComponentInParent<Canvas>(true) == null &&
                               r.GetComponent("TMP_Text") == null && r.GetComponent("TextMeshPro") == null;
                if (isModel) r.enabled = false;
            }
        }

        GameObject instance = Instantiate(prefab, controller.transform, false);
        instance.name = (left ? "Left" : "Right") + "ControllerHand";
        foreach (M1CustomHandVisual driver in instance.GetComponentsInChildren<M1CustomHandVisual>(true)) Destroy(driver);
        foreach (SkinnedMeshRenderer r in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            r.enabled = true;
            r.updateWhenOffscreen = true;
        }
        side.root = instance.transform;
        side.root.localPosition = Vector3.zero;
        side.root.localRotation = Quaternion.identity;

        Dictionary<string, Transform> byName = instance.GetComponentsInChildren<Transform>(true)
            .GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
        string prefix = "J_" + (left ? "Left" : "Right") + "_Hand";
        Transform Bone(string n) => byName.TryGetValue(prefix + n, out Transform t) ? t : null;

        Transform wrist = Bone(string.Empty), middle = Bone("Middle1"), index = Bone("Index1"), pinky = Bone("Pinky1");
        if (wrist == null || middle == null || index == null || pinky == null)
        {
            Debug.LogWarning($"[มือถือจอย] ไม่พบกระดูกมือ {prefix}* จึงข้าม", this);
            Destroy(instance);
            return null;
        }

        // ย่อ/ขยายให้มือยาวเท่ามือจริง (โมเดลเดิมอาจหน่วยต่างกัน)
        Transform tip = Bone("Middle4") ?? middle;
        float length = Vector3.Distance(wrist.position, tip.position);
        if (length > 0.001f) side.root.localScale *= handLength / length;

        // กรอบมือจากท่าเดิมของโมเดล: ทิศนิ้ว (ข้อมือ→โคนนิ้วกลาง) และทิศฝ่ามือ
        Vector3 fingerDir = (middle.position - wrist.position).normalized;
        Vector3 normal = Vector3.Cross(index.position - wrist.position, pinky.position - wrist.position).normalized;
        Vector3 palmSide = left ? normal : -normal;   // มือขวา: cross ชี้หลังมือ · มือซ้าย: ชี้ฝ่ามือ
        side.handFrameLocal = Quaternion.Inverse(side.root.rotation) * Quaternion.LookRotation(fingerDir, palmSide);
        side.palmLocal = side.root.InverseTransformPoint((wrist.position + middle.position) * 0.5f + palmSide * 0.015f);

        // แกนงอของแต่ละข้อ: หมุนแล้วปลายนิ้วเข้าหาฝ่ามือ (axis = d × palm)
        for (int f = 0; f < Fingers.Length; f++)
        {
            for (int n = 1; n <= 3; n++)
            {
                Transform b = Bone(Fingers[f] + n), next = Bone(Fingers[f] + (n + 1));
                if (b == null || next == null) continue;
                Vector3 d = (next.position - b.position).normalized;
                Vector3 axisWorld = Vector3.Cross(d, palmSide);
                if (axisWorld.sqrMagnitude < 1e-6f) continue;
                side.bones.Add(new FingerBone
                {
                    bone = b, rest = b.localRotation, finger = f,
                    axis = Quaternion.Inverse(b.rotation) * axisWorld.normalized
                });
            }
        }

        side.trigger = new InputAction(hand + "Trigger", InputActionType.Value);
        side.trigger.AddBinding($"<XRController>{{{hand}}}/{{Trigger}}");
        side.trigger.AddBinding($"<XRController>{{{hand}}}/trigger");
        side.grip = new InputAction(hand + "Grip", InputActionType.Value);
        side.grip.AddBinding($"<XRController>{{{hand}}}/{{Grip}}");
        side.grip.AddBinding($"<XRController>{{{hand}}}/grip");
        side.trigger.Enable();
        side.grip.Enable();

        PlaceHand(side);
        Debug.Log($"[มือถือจอย] สร้างมือ{(left ? "ซ้าย" : "ขวา")}บนจอยแล้ว (ข้อนิ้ว {side.bones.Count})", this);
        return side;
    }

    private void PlaceHand(Side s)
    {
        float mirror = s.left ? -1f : 1f;
        Vector3 forward = Quaternion.Euler(fingerPitch, 0f, 0f) * Vector3.forward;
        Vector3 palmFacing = new(-mirror, 0f, 0f);   // ฝ่ามือหันเข้าหาด้ามจอย
        Quaternion extra = Quaternion.Euler(extraRotation.x, extraRotation.y * mirror, extraRotation.z * mirror);
        Quaternion target = extra * Quaternion.LookRotation(forward, palmFacing);

        s.root.localRotation = target * Quaternion.Inverse(s.handFrameLocal);
        Vector3 palmTarget = new(gripPalmPosition.x * mirror, gripPalmPosition.y, gripPalmPosition.z);
        s.root.localPosition = palmTarget - s.root.localRotation * Vector3.Scale(s.palmLocal, s.root.localScale);
    }

    private void LateUpdate()
    {
        try { UpdateHands(); }
        catch (Exception e)
        {
            // อย่าให้มือพังแล้วลากทั้งเกมค้าง: ปิดมือถือจอยแล้วรายงาน
            Debug.LogError($"[มือถือจอย] ปิดตัวเองเพราะ {e.GetType().Name}: {e.Message}", this);
            enabled = false;
        }
    }

    private void UpdateHands()
    {
        foreach (Side s in sides)
        {
            if (s == null || s.root == null || s.controller == null || !s.controller.activeInHierarchy) continue;

            float k = 1f - Mathf.Exp(-curlSpeed * Time.deltaTime);
            s.triggerValue = Mathf.Lerp(s.triggerValue, Read(s.trigger), k);
            s.gripValue = Mathf.Lerp(s.gripValue, Read(s.grip), k);

            PlaceHand(s);   // ปรับค่าใน Inspector ระหว่าง Play เห็นผลทันที
            foreach (FingerBone fb in s.bones)
            {
                float angle = fb.finger switch
                {
                    0 => thumbCurl,
                    1 => Mathf.Lerp(indexRest, indexPressed, s.triggerValue),
                    _ => Mathf.Lerp(restCurl, gripCurl, s.gripValue)
                };
                fb.bone.localRotation = fb.rest * Quaternion.AngleAxis(flipCurl ? -angle : angle, fb.axis);
            }
        }
    }

    private static float Read(InputAction action)
    {
        try { return action != null ? Mathf.Clamp01(action.ReadValue<float>()) : 0f; }
        catch (InvalidOperationException) { return 0f; }
    }
}
