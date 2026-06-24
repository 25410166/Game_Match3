using UnityEngine;

public struct GemEffectMatchEntry
{
    public int GemType;
    public int MatchCount;
    public int ComboIndex;

    public GemEffectMatchEntry(int gemType, int matchCount, int comboIndex)
    {
        GemType = gemType;
        MatchCount = matchCount;
        ComboIndex = comboIndex;
    }
}

public static class GemEffectProcessor
{
    public const int ShieldItemId = 0;
    public const int RageItemId = 1;
    public const int HealItemId = 2;
    public const int ManaItemId = 3;
    public const int AttackItemId = 4;
    public const int DrainItemId = 5;

    public static void ProcessForPlayer(GemEffectMatchEntry entry, PlayerStats attackerStats, AIStats targetStats)
    {
        if (attackerStats == null || targetStats == null)
            return;

        float comboMultiplier = GetComboMultiplier(entry.ComboIndex);
        int matchCount = Mathf.Max(3, entry.MatchCount);

        switch (entry.GemType)
        {
            case RageItemId:
                ApplyRage(attackerStats, matchCount, comboMultiplier, "Player");
                break;

            case HealItemId:
                ApplyHeal(attackerStats, matchCount, comboMultiplier, "Player");
                break;

            case ManaItemId:
                ApplyMana(attackerStats, matchCount, comboMultiplier, "Player");
                break;

            case ShieldItemId:
                ApplyShield(attackerStats, matchCount, comboMultiplier, "Player");
                break;

            case AttackItemId:
                attackerStats.TriggerGemAttack(matchCount, comboMultiplier);
                Debug.Log($"[GemEffect] Player Attack | match={matchCount}, combo={comboMultiplier.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}");
                break;

            case DrainItemId:
                ApplyDrainForPlayer(attackerStats, targetStats, matchCount);
                break;
        }
    }

    public static void ProcessForAI(GemEffectMatchEntry entry, AIStats attackerStats, PlayerStats targetStats)
    {
        if (attackerStats == null || targetStats == null)
            return;

        float comboMultiplier = GetComboMultiplier(entry.ComboIndex);
        int matchCount = Mathf.Max(3, entry.MatchCount);

        switch (entry.GemType)
        {
            case RageItemId:
                ApplyRage(attackerStats, matchCount, comboMultiplier, "AI");
                break;

            case HealItemId:
                ApplyHeal(attackerStats, matchCount, comboMultiplier, "AI");
                break;

            case ManaItemId:
                ApplyMana(attackerStats, matchCount, comboMultiplier, "AI");
                break;

            case ShieldItemId:
                ApplyShield(attackerStats, matchCount, comboMultiplier, "AI");
                break;

            case AttackItemId:
                attackerStats.TriggerGemAttack(matchCount, comboMultiplier);
                Debug.Log($"[GemEffect] AI Attack | match={matchCount}, combo={comboMultiplier.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}");
                break;

            case DrainItemId:
                ApplyDrainForAI(attackerStats, targetStats, matchCount);
                break;
        }
    }

    private static float GetComboMultiplier(int comboIndex)
    {
        int safeCombo = Mathf.Max(1, comboIndex);
        return 1f + (safeCombo - 1) * 0.25f;
    }

    private static int GetResourceGainByMatchCount(int matchCount)
    {
        if (matchCount == 3) return 10;
        if (matchCount == 4) return 15;
        if (matchCount == 5) return 20;
        return 25;
    }

    private static float GetShieldPercentByMatchCount(int matchCount)
    {
        if (matchCount == 3) return 0.05f;
        if (matchCount == 4) return 0.07f;
        if (matchCount == 5) return 0.09f;
        return 0.12f;
    }

    private static void ApplyRage(PlayerStats attacker, int matchCount, float comboMultiplier, string actor)
    {
        int safeMatchCount = Mathf.Max(1, matchCount);
        string element = attacker != null ? attacker.element : string.Empty;
        int ragePerGem = ElementResourceGainConfig.GetRagePerGem(element);
        int rageGain = Mathf.Max(0, safeMatchCount * ragePerGem);
        attacker.GainRage(rageGain);
        AudioManager.Instance?.PlayBattleGainRageSound();
        Debug.Log($"[GemEffect] {actor} Rage | match={safeMatchCount}, element={element}, formula=gemCount*{ragePerGem}, gain={rageGain}");
    }

    private static void ApplyRage(AIStats attacker, int matchCount, float comboMultiplier, string actor)
    {
        int safeMatchCount = Mathf.Max(1, matchCount);
        string element = attacker != null ? attacker.element : string.Empty;
        int ragePerGem = ElementResourceGainConfig.GetRagePerGem(element);
        int rageGain = Mathf.Max(0, safeMatchCount * ragePerGem);
        attacker.GainRage(rageGain);
        AudioManager.Instance?.PlayBattleGainRageSound();
        Debug.Log($"[GemEffect] {actor} Rage | match={safeMatchCount}, element={element}, formula=gemCount*{ragePerGem}, gain={rageGain}");
    }

    private static void ApplyMana(PlayerStats attacker, int matchCount, float comboMultiplier, string actor)
    {
        int safeMatchCount = Mathf.Max(1, matchCount);
        string element = attacker != null ? attacker.element : string.Empty;
        int manaPerGem = ElementResourceGainConfig.GetManaPerGem(element);
        int manaGain = Mathf.Max(0, safeMatchCount * manaPerGem);
        attacker.GainMana(manaGain);
        AudioManager.Instance?.PlayBattleGainManaSound();
        Debug.Log($"[GemEffect] {actor} Mana | match={safeMatchCount}, element={element}, formula=gemCount*{manaPerGem}, gain={manaGain}");
    }

    private static void ApplyMana(AIStats attacker, int matchCount, float comboMultiplier, string actor)
    {
        int safeMatchCount = Mathf.Max(1, matchCount);
        string element = attacker != null ? attacker.element : string.Empty;
        int manaPerGem = ElementResourceGainConfig.GetManaPerGem(element);
        int manaGain = Mathf.Max(0, safeMatchCount * manaPerGem);
        attacker.GainMana(manaGain);
        AudioManager.Instance?.PlayBattleGainManaSound();
        Debug.Log($"[GemEffect] {actor} Mana | match={safeMatchCount}, element={element}, formula=gemCount*{manaPerGem}, gain={manaGain}");
    }

    private static void ApplyHeal(PlayerStats attacker, int matchCount, float comboMultiplier, string actor)
    {
        int safeMatchCount = Mathf.Max(1, matchCount);
        int healAmount = Mathf.Max(0, Mathf.RoundToInt(attacker.maxHP * 0.02f * safeMatchCount));
        attacker.Heal(healAmount);
        AudioManager.Instance?.PlayBattleGainHpSound();
        Debug.Log($"[GemEffect] {actor} Heal | match={safeMatchCount}, formula=2%*maxHP*gemCount, heal={healAmount}");
    }

    private static void ApplyHeal(AIStats attacker, int matchCount, float comboMultiplier, string actor)
    {
        int safeMatchCount = Mathf.Max(1, matchCount);
        int healAmount = Mathf.Max(0, Mathf.RoundToInt(attacker.maxHealth * 0.02f * safeMatchCount));
        attacker.Heal(healAmount);
        AudioManager.Instance?.PlayBattleGainHpSound();
        Debug.Log($"[GemEffect] {actor} Heal | match={safeMatchCount}, formula=2%*maxHP*gemCount, heal={healAmount}");
    }

    private static void ApplyShield(PlayerStats attacker, int matchCount, float comboMultiplier, string actor)
    {
        float shieldPercent = GetShieldPercentByMatchCount(matchCount);
        int shieldAmount = Mathf.Max(0, Mathf.RoundToInt(attacker.HP * shieldPercent * comboMultiplier));
        attacker.AddShield(shieldAmount, 2);
        AudioManager.Instance?.PlayBattleGainArmorSound();
        Debug.Log($"[GemEffect] {actor} Shield | match={matchCount}, combo={comboMultiplier:0.##}, shield={shieldAmount}");
    }

    private static void ApplyShield(AIStats attacker, int matchCount, float comboMultiplier, string actor)
    {
        float shieldPercent = GetShieldPercentByMatchCount(matchCount);
        int shieldAmount = Mathf.Max(0, Mathf.RoundToInt(attacker.Health * shieldPercent * comboMultiplier));
        attacker.AddShield(shieldAmount, 2);
        AudioManager.Instance?.PlayBattleGainArmorSound();
        Debug.Log($"[GemEffect] {actor} Shield | match={matchCount}, combo={comboMultiplier:0.##}, shield={shieldAmount}");
    }

    private static void ApplyDrainForPlayer(PlayerStats attacker, AIStats target, int matchCount)
    {
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
        AudioManager.Instance?.PlayBattleRainSound();

        Debug.Log($"[GemEffect] Player Drain | match={matchCount}, hp%={hpPercent * 100f:0.#}, mana%={manaPercent * 100f:0.#}, rage%={ragePercent * 100f:0.#}, hp={hpDone}, mana={manaDone}, rage={rageDone}");
    }

    private static void ApplyDrainForAI(AIStats attacker, PlayerStats target, int matchCount)
    {
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
        AudioManager.Instance?.PlayBattleRainSound();

        Debug.Log($"[GemEffect] AI Drain | match={matchCount}, hp%={hpPercent * 100f:0.#}, mana%={manaPercent * 100f:0.#}, rage%={ragePercent * 100f:0.#}, hp={hpDone}, mana={manaDone}, rage={rageDone}");
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
