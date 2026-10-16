using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

// Visual-only retargeting: interaction and gesture detection remain on the XR rig.
public sealed class M1CustomHandVisual : MonoBehaviour
{
    [Serializable]
    public struct BoneBinding
    {
        public Transform bone;
        public XRHandJointID joint;
        public XRHandJointID nextJoint;
        public Quaternion rotationOffset;
        public Vector3 positionOffset;
    }

    public Handedness handedness;
    public SkinnedMeshRenderer handRenderer;
    public BoneBinding[] bindings;
    private XRHandSubsystem subsystem;
    private readonly List<XRHandSubsystem> subsystems = new();
    private readonly Vector3[] positions = new Vector3[32];

    private void OnEnable() => SetVisible(false);
    private void Update()
    {
        if (subsystem != null && subsystem.running) return;
        Disconnect();
        SubsystemManager.GetSubsystems(subsystems);
        foreach (var candidate in subsystems)
        {
            if (!candidate.running) continue;
            subsystem = candidate;
            subsystem.updatedHands += OnHandsUpdated;
            break;
        }
    }

    private void OnDisable() => Disconnect();
    private void Disconnect()
    {
        if (subsystem != null) subsystem.updatedHands -= OnHandsUpdated;
        subsystem = null;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (handRenderer != null) handRenderer.enabled = visible;
    }

    private void OnHandsUpdated(XRHandSubsystem source, XRHandSubsystem.UpdateSuccessFlags flags,
        XRHandSubsystem.UpdateType updateType)
    {
        var hand = handedness == Handedness.Left ? source.leftHand : source.rightHand;
        var required = handedness == Handedness.Left
            ? XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints
            : XRHandSubsystem.UpdateSuccessFlags.RightHandJoints;
        if (!hand.isTracked || (flags & required) == 0) { SetVisible(false); return; }
        foreach (var binding in bindings)
        {
            if (!hand.GetJoint(binding.joint).TryGetPose(out var pose) ||
                !hand.GetJoint(binding.nextJoint).TryGetPose(out var next))
            { SetVisible(false); return; }
            positions[(int)binding.joint] = pose.position;
            positions[(int)binding.nextJoint] = next.position;
        }
        SetVisible(ApplyJointPositions(positions));
    }

    // The same geometric frames are used at import and runtime. This avoids
    // assuming the FBX bones use OpenXR's joint axes, and preserves skin bind poses.
    public static Vector3 PalmNormal(Vector3[] joints)
    {
        var wrist = joints[(int)XRHandJointID.Wrist];
        return Vector3.Cross(joints[(int)XRHandJointID.IndexProximal] - wrist,
            joints[(int)XRHandJointID.LittleProximal] - wrist).normalized;
    }

    public static Quaternion Frame(Vector3[] joints, XRHandJointID joint,
        XRHandJointID next, Vector3 normal)
    {
        return Quaternion.LookRotation(joints[(int)next] - joints[(int)joint], normal);
    }

    public bool ApplyJointPositions(Vector3[] joints)
    {
        var normal = PalmNormal(joints);
        if (normal.sqrMagnitude < 0.5f) return false;
        foreach (var b in bindings)
        {
            if ((joints[(int)b.nextJoint] - joints[(int)b.joint]).sqrMagnitude < 0.00000001f)
                return false;
        }
        // Bindings are parent-first so writing a parent never displaces a finished child.
        foreach (var b in bindings)
        {
            var frame = Frame(joints, b.joint, b.nextJoint, normal);
            b.bone.SetPositionAndRotation(transform.TransformPoint(joints[(int)b.joint] + frame * b.positionOffset),
                transform.rotation * frame * b.rotationOffset);
        }
        return true;
    }
}
