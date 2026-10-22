using System;
using System.Collections.Generic;
using UnityEngine;

// วัตถุดิบที่ขั้นภารกิจหนึ่งต้องการ: ชนิดอะไร กี่ชิ้น และต้องอยู่ในสถานะไหน
[Serializable]
public class IngredientRequirement
{
    public string ingredientId;

    [Tooltip("ชื่อ Prefab เดิมที่ยังรองรับระหว่างเปลี่ยนผ่าน เช่น Krapow")]
    public string legacyName;

    public string displayNameThai;
    public string unitThai;

    [Min(1)]
    public int requiredCount = 1;

    [Tooltip("สถานะที่วัตถุดิบต้องไปถึงจึงนับ เช่น Collected, Washed, Cut")]
    public IngredientState targetState = IngredientState.Collected;

    [Tooltip("จำนวนที่ทำได้แล้วในรอบนี้ ระบบตั้งค่าเอง")]
    public int currentCount;

    public bool IsComplete => currentCount >= requiredCount;

    public IngredientRequirement()
    {
    }

    public IngredientRequirement(
        string ingredientId,
        string legacyName,
        string displayNameThai,
        string unitThai,
        int requiredCount,
        IngredientState targetState = IngredientState.Collected)
    {
        this.ingredientId = ingredientId;
        this.legacyName = legacyName;
        this.displayNameThai = displayNameThai;
        this.unitThai = unitThai;
        this.requiredCount = requiredCount;
        this.targetState = targetState;
    }

    public bool Matches(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return string.Equals(
                   id,
                   ingredientId,
                   StringComparison.OrdinalIgnoreCase) ||
               (!string.IsNullOrEmpty(legacyName) &&
                string.Equals(
                    id,
                    legacyName,
                    StringComparison.OrdinalIgnoreCase));
    }
}

// ขั้นภารกิจหนึ่งขั้น ถ้าไม่มีรายการวัตถุดิบ ขั้นนั้นผ่านด้วยการกระทำอื่น
// เช่น accept_mission ผ่านเมื่อรับภารกิจจากป้าแสง
[Serializable]
public class MissionStepDefinition
{
    public string stepId;
    public string titleThai;
    public List<IngredientRequirement> requirements = new();

    public bool HasRequirements =>
        requirements != null && requirements.Count > 0;

    public bool IsComplete =>
        HasRequirements && requirements.TrueForAll(r => r.IsComplete);

    public MissionStepDefinition()
    {
    }

    public MissionStepDefinition(
        string stepId,
        string titleThai,
        params IngredientRequirement[] requirements)
    {
        this.stepId = stepId;
        this.titleThai = titleThai;
        this.requirements = new List<IngredientRequirement>(requirements);
    }

    public bool HasRequirementsFor(IngredientState state)
    {
        return HasRequirements &&
               requirements.Exists(r => r.targetState == state);
    }

    public IngredientRequirement FindRequirement(
        string ingredientId,
        IngredientState state)
    {
        if (!HasRequirements)
        {
            return null;
        }

        return requirements.Find(
            r => r.targetState == state && r.Matches(ingredientId));
    }

    public string DescribeRequirements(IngredientState state)
    {
        if (!HasRequirements)
        {
            return string.Empty;
        }

        var names = new List<string>();

        foreach (IngredientRequirement requirement in requirements)
        {
            if (requirement.targetState == state)
            {
                names.Add(requirement.displayNameThai);
            }
        }

        return string.Join("และ", names);
    }
}
