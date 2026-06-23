using System.Collections.Generic;
using UnityEngine;

public static class UpgradeConfig
{
    public const float MIN_RATE = 0.05f;
    public const float MAX_RATE = 0.90f;

    public static float GetBaseRate(int petLevel, int gemLevel)
    {
        int delta = gemLevel - petLevel;

        if (delta <= -3) return 0.05f;
        if (delta == -2) return 0.10f;
        if (delta == -1) return 0.20f;
        if (delta == 0) return 0.30f;
        if (delta == 1) return 0.45f;
        if (delta == 2) return 0.60f;
        return 0.70f;
    }
}

public static class PetUpgradeService
{
    public static float CalculateSuccessRate(int petLevel, IReadOnlyList<int> gemLevels)
    {
        if (gemLevels == null || gemLevels.Count == 0)
            return 0f;

        List<int> valid = new List<int>();
        for (int i = 0; i < gemLevels.Count; i++)
        {
            int level = gemLevels[i];
            if (level >= 1 && level <= 5)
                valid.Add(level);
        }

        if (valid.Count == 0)
            return 0f;

        float failRate = 1f;
        for (int i = 0; i < valid.Count; i++)
        {
            float baseRate = UpgradeConfig.GetBaseRate(petLevel, valid[i]);
            failRate *= (1f - baseRate);
        }

        float successRate = 1f - failRate;

        if (valid.Count == 3)
            successRate += 0.05f;

        bool sameLevel = true;
        for (int i = 1; i < valid.Count; i++)
        {
            if (valid[i] != valid[0])
            {
                sameLevel = false;
                break;
            }
        }

        if (sameLevel)
            successRate += 0.05f;

        for (int i = 0; i < valid.Count; i++)
        {
            if (valid[i] - petLevel <= -2)
            {
                successRate *= 0.8f;
                break;
            }
        }

        return Mathf.Clamp(successRate, UpgradeConfig.MIN_RATE, UpgradeConfig.MAX_RATE);
    }

    public static bool TryUpgrade(int petLevel, IReadOnlyList<int> gemLevels, out float successRate, out float roll)
    {
        successRate = CalculateSuccessRate(petLevel, gemLevels);
        roll = Random.value;
        return roll <= successRate;
    }
}
