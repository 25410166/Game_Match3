using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GuardianDatabase", menuName = "Guardian/Guardian Database")]
public class GuardianDatabase : ScriptableObject
{
    public List<GuardianDataAsset> guardians = new List<GuardianDataAsset>();

    public GuardianDataAsset GetGuardianById(int guardianId)
    {
        if (guardians == null)
            return null;

        for (int i = 0; i < guardians.Count; i++)
        {
            GuardianDataAsset g = guardians[i];
            if (g != null && g.guardianId == guardianId)
                return g;
        }

        return null;
    }
}

[Serializable]
public class GuardianDataAsset
{
    [Header("Basic Info")]
    public int guardianId;
    public string guardianName;
    public GuardianElement element;

    [TextArea]
    public string description;

    [TextArea]
    public string story;

    [Header("Apply Triggers")]
    public bool applyOnBattleStart;
    public bool applyOnPlayerTurn = true;

    [Header("Levels")]
    public List<GuardianLevelData> levels = new List<GuardianLevelData>();

    [Header("Visual")]
    public GameObject guardianPrefab;
    public Sprite avatarIcon;
    public GameObject vfxPrefab;

    public GuardianLevelData GetLevelData(int level)
    {
        if (levels == null || levels.Count == 0)
            return new GuardianLevelData { level = 1 };

        int clamped = Mathf.Clamp(level, 1, levels.Count);
        GuardianLevelData data = levels[clamped - 1];
        if (data != null)
            return data;

        return new GuardianLevelData { level = clamped };
    }

    public int GetMaxLevel()
    {
        return levels != null && levels.Count > 0 ? levels.Count : 1;
    }

    public int GetPurchaseCost()
    {
        GuardianLevelData data = GetLevelData(1);
        return data != null ? Mathf.Max(0, data.diamondCost) : 0;
    }

    public int GetUpgradeCost(int currentLevel)
    {
        int nextLevel = Mathf.Max(1, currentLevel + 1);
        GuardianLevelData data = GetLevelData(nextLevel);
        if (data == null || data.level <= currentLevel)
            return 0;

        return Mathf.Max(0, data.diamondCost);
    }
}

[Serializable]
public class GuardianLevelData
{
    public int level = 1;
    public float value1;
    public float value2;
    public float value3;
    public int diamondCost;
}
