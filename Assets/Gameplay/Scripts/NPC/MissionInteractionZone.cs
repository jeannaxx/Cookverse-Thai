
//Tool / Using call tool 
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

//บังคับให้ Object มี Box Collider
//public = Script อื่นสามารถเข้าถึงได้
//class = กำลังสร้างชุดคำสั่งหนึ่งชุด
//MissionInteractionZone = ชื่อ Class
//MonoBehaviour = ทำให้ Script นี้ติดกับ GameObject ใน Unity ได้
[RequireComponent(typeof(BoxCollider))]
public class MissionInteractionZone : MonoBehaviour
{
    //ตัวแปรอ้างอิง Object อื่น Header สร้างหัวข้อใน Inspector
    //SerializeField ทำให้ตัวแปรที่เป็น private ปรากฏใน Inspector เพื่อให้เราลาก Object มาใส่ได้
    //private หมายถึงใช้ภายใน Script นี้เป็นหลัก
    [Header("Required References")]
    [SerializeField] private Transform playerRoot;
    [SerializeField] private MissionGiver missionGiver;

    [Header("Mission Settings")]
    [SerializeField] private bool giveMissionOnEnter = true;

    [Header("Player State")]
    [SerializeField] private bool playerInside;

    [Header("Zone Events")]
    [SerializeField] private UnityEvent onPlayerEntered;
    [SerializeField] private UnityEvent onPlayerExited;

    private readonly HashSet<Collider> playerCollidersInside = new();

    public bool PlayerInside => playerInside;

    private void Reset()
    {
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayerCollider(other))
        {
            return;
        }

        if (!playerCollidersInside.Add(other))
        {
            return;
        }

        if (playerCollidersInside.Count == 1)
        {
            SetPlayerInside(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!playerCollidersInside.Remove(other))
        {
            return;
        }

        if (playerCollidersInside.Count == 0)
        {
            SetPlayerInside(false);
        }
    }

    private bool IsPlayerCollider(Collider other)
    {
        if (playerRoot == null)
        {
            return false;
        }

        return other.transform == playerRoot ||
               other.transform.IsChildOf(playerRoot);
    }

    public void RequestMission()
    {
        if (!playerInside)
        {
            Debug.LogWarning(
                "[พื้นที่ NPC] ผู้เล่นยังไม่ได้อยู่ในพื้นที่สนทนากับป้าแสง",
                this);
            return;
        }

        if (missionGiver == null)
        {
            Debug.LogError(
                "[พื้นที่ NPC] ยังไม่ได้เชื่อมผู้ให้ภารกิจ",
                this);
            return;
        }

        missionGiver.GiveMission();
    }

    private void SetPlayerInside(bool isInside)
    {
        if (playerInside == isInside)
        {
            return;
        }

        playerInside = isInside;

        if (playerInside)
        {
            Debug.Log(
                "[พื้นที่ NPC] ผู้เล่นเข้าสู่พื้นที่สนทนากับป้าแสง",
                this);

            onPlayerEntered?.Invoke();

            if (giveMissionOnEnter)
            {
                RequestMission();
            }

            return;
        }

        Debug.Log(
            "[พื้นที่ NPC] ผู้เล่นออกจากพื้นที่สนทนากับป้าแสง",
            this);

        onPlayerExited?.Invoke();
    }

    [ContextMenu("Test/Simulate Player Enter")]
    private void TestPlayerEnter()
    {
        SetPlayerInside(true);
    }

    [ContextMenu("Test/Simulate Player Exit")]
    private void TestPlayerExit()
    {
        SetPlayerInside(false);
    }
}
