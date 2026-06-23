using UnityEngine;

public static class BoardEffectSystem
{
    public const int ShieldItemId = 0;
    public const int RageItemId = 1;
    public const int HealItemId = 2;
    public const int ManaItemId = 3;
    public const int AttackItemId = 4;
    public const int DrainItemId = 5;

    public const int DefaultGainValue = 10;

    public static bool IsAttackItem(int itemId)
    {
        return itemId == AttackItemId;
    }

    public static void ApplyStatEffect(PlayerStats target, int itemId, int value = DefaultGainValue)
    {
        if (target == null || value <= 0)
            return;

        switch (itemId)
        {
            case ShieldItemId:
                target.AddShield(value, 2);
                break;
            case RageItemId:
                target.GainRage(Mathf.Max(0, value) * ElementResourceGainConfig.GetRagePerGem(target.element));
                break;
            case HealItemId:
                ApplyHealByGemCount(target, value);
                break;
            case ManaItemId:
                target.GainMana(Mathf.Max(0, value) * ElementResourceGainConfig.GetManaPerGem(target.element));
                break;
            case DrainItemId:
                Debug.LogWarning("[BoardEffectSystem] Drain requires attacker and target. Use ApplyDrainByMatch(attacker, target, matchCount).");
                break;
        }
    }

    public static void ApplyStatEffect(AIStats target, int itemId, int value = DefaultGainValue)
    {
        if (target == null || value <= 0)
            return;

        switch (itemId)
        {
            case ShieldItemId:
                target.AddShield(value, 2);
                break;
            case RageItemId:
                target.GainRage(Mathf.Max(0, value) * ElementResourceGainConfig.GetRagePerGem(target.element));
                break;
            case HealItemId:
                ApplyHealByGemCount(target, value);
                break;
            case ManaItemId:
                target.GainMana(Mathf.Max(0, value) * ElementResourceGainConfig.GetManaPerGem(target.element));
                break;
            case DrainItemId:
                Debug.LogWarning("[BoardEffectSystem] Drain requires attacker and target. Use ApplyDrainByMatch(attacker, target, matchCount).");
                break;
        }
    }

    private static void ApplyHealByGemCount(PlayerStats target, int gemCount)
    {
        int safeGemCount = Mathf.Max(1, gemCount);
        int healAmount = Mathf.Max(0, Mathf.RoundToInt(target.maxHP * 0.02f * safeGemCount));
        target.Heal(healAmount);
    }

    private static void ApplyHealByGemCount(AIStats target, int gemCount)
    {
        int safeGemCount = Mathf.Max(1, gemCount);
        int healAmount = Mathf.Max(0, Mathf.RoundToInt(target.maxHealth * 0.02f * safeGemCount));
        target.Heal(healAmount);
    }

    public static void ApplyDrainByMatch(PlayerStats attacker, AIStats target, int matchCount)
    {
        if (attacker == null || target == null)
            return;

        GetDrainPercents(matchCount, out float hpPercent, out float manaPercent, out float ragePercent);

        int hpRequest = Mathf.Max(0, Mathf.RoundToInt(target.maxHealth * hpPercent));
        int manaRequest = Mathf.Max(0, Mathf.RoundToInt(target.maxMana * manaPercent));
        int rageRequest = Mathf.Max(0, Mathf.RoundToInt(target.maxRage * ragePercent));

        int hpDone = target.TakeRawDamage(hpRequest);
        int manaDone = target.ReduceMana(manaRequest);
        int rageDone = target.ReduceRage(rageRequest);

        attacker.Heal(hpDone);
        attacker.GainMana(manaDone);
        attacker.GainRage(rageDone);
    }

    public static void ApplyDrainByMatch(AIStats attacker, PlayerStats target, int matchCount)
    {
        if (attacker == null || target == null)
            return;

        GetDrainPercents(matchCount, out float hpPercent, out float manaPercent, out float ragePercent);

        int hpRequest = Mathf.Max(0, Mathf.RoundToInt(target.maxHP * hpPercent));
        int manaRequest = Mathf.Max(0, Mathf.RoundToInt(target.maxMana * manaPercent));
        int rageRequest = Mathf.Max(0, Mathf.RoundToInt(target.maxRage * ragePercent));

        int hpDone = target.TakeRawDamage(hpRequest);
        int manaDone = target.ReduceMana(manaRequest);
        int rageDone = target.ReduceRage(rageRequest);

        attacker.Heal(hpDone);
        attacker.GainMana(manaDone);
        attacker.GainRage(rageDone);
    }

    private static void GetDrainPercents(int matchCount, out float hpPercent, out float manaPercent, out float ragePercent)
    {
        hpPercent = 0f;
        manaPercent = 0f;
        ragePercent = 0f;

        int safeMatchCount = Mathf.Max(1, matchCount);
        if (safeMatchCount == 3)
        {
            hpPercent = 0.10f;
            return;
        }

        if (safeMatchCount == 4)
        {
            manaPercent = 0.30f;
            return;
        }

        if (safeMatchCount == 5)
        {
            ragePercent = 0.50f;
            return;
        }

        if (safeMatchCount > 5)
        {
            float percent = Mathf.Min(0.30f, 0.05f * safeMatchCount);
            hpPercent = percent;
            manaPercent = percent;
            ragePercent = percent;
        }
    }
}
