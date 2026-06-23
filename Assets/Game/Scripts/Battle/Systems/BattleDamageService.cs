using System;
using UnityEngine;

public sealed class BattleDamageActor
{
    public string Name;
    public Transform Root;
    public float CritRate;
    public float CritDamage;

    public string Element;

    public BattleDamageActor(string name, Transform root, float critRate = 0f, float critDamage = 0f, string element = "")
    {
        Name = name;
        Root = root;
        CritRate = critRate;
        CritDamage = critDamage;
        Element = element;
    }
}

public sealed class BattleDamageTarget
{
    public string Name;
    public Transform Root;
    public Func<int, int> ApplyDamage;
    public GameObject DamagePopupPrefab;
    public GameObject HitFxPrefab;
    public AudioSource AudioSource;
    public AudioClip HitSound;
    public string Weakness;


    public BattleDamageTarget(
        string name,
        Transform root,
        Func<int, int> applyDamage,
        GameObject damagePopupPrefab,
        GameObject hitFxPrefab,
        AudioSource audioSource = null,
        AudioClip hitSound = null,
        string weakness = "")
    {
        Name = name;
        Root = root;
        ApplyDamage = applyDamage;
        DamagePopupPrefab = damagePopupPrefab;
        HitFxPrefab = hitFxPrefab;
        AudioSource = audioSource;
        HitSound = hitSound;
        Weakness = weakness;
    }
}

public static class BattleDamageService
{
    public static BattleDamageActor BuildActor(string name, Transform root)
    {
        return new BattleDamageActor(name, root);
    }

    public static BattleDamageActor BuildActor(string name, Transform root, float critRate, float critDamage)
    {
        return new BattleDamageActor(name, root, critRate, critDamage);
    }

    public static BattleDamageActor BuildActor(string name, Transform root, float critRate, float critDamage, string element)
    {
        return new BattleDamageActor(name, root, critRate, critDamage, element);
    }

    public static BattleDamageTarget BuildTarget(
        string name,
        Transform root,
        Func<int, int> applyDamage,
        GameObject damagePopupPrefab,
        GameObject hitFxPrefab,
        AudioSource audioSource,
        AudioClip hitSound,
        string weakness)
    {
        return new BattleDamageTarget(name, root, applyDamage, damagePopupPrefab, hitFxPrefab, audioSource, hitSound, weakness);
    }

    public static int ApplyDamage(BattleDamageActor attacker, BattleDamageTarget target, int baseDamage, float multiplier)
    {
        int safeBaseDamage = Mathf.Max(1, baseDamage);
        float safeMultiplier = Mathf.Max(0f, multiplier);
        float critRate = attacker != null ? Mathf.Max(0f, attacker.CritRate) : 0f;
        float critDamage = attacker != null ? Mathf.Max(0f, attacker.CritDamage) : 0f;
        bool isCrit = critRate > 0f && UnityEngine.Random.value < Mathf.Clamp01(critRate * 0.01f);
        float critMultiplier = isCrit ? (1f + critDamage * 0.01f) : 1f;
        float weaknessMultiplier = ElementWeaknessSystem.GetWeaknessMultiplier(
            attacker != null ? attacker.Element : "",
            target != null ? target.Weakness : "");
        int preMitigationDamage = Mathf.Max(1, Mathf.RoundToInt(safeBaseDamage * safeMultiplier * weaknessMultiplier * critMultiplier));

        string attackerName = attacker != null && !string.IsNullOrWhiteSpace(attacker.Name) ? attacker.Name : "UnknownAttacker";
        string targetName = target != null && !string.IsNullOrWhiteSpace(target.Name) ? target.Name : "UnknownTarget";
        Debug.Log($"[BattleDamageService] {attackerName} -> {targetName} | baseDamage={safeBaseDamage}, multiplier={safeMultiplier:0.##}, weakness={weaknessMultiplier:0.##}, critRate={critRate:0.##}, critDamage={critDamage:0.##}, isCrit={isCrit}, preMitigation={preMitigationDamage}");

        if (target == null || target.ApplyDamage == null)
            return 0;

        int dealt = Mathf.Max(0, target.ApplyDamage(preMitigationDamage));
        Debug.Log($"[BattleDamageService] {attackerName} -> {targetName} | actualDealt={dealt}");
        if (dealt <= 0)
            return 0;

        if (target.Root != null && target.HitFxPrefab != null)
            BattleFxUtility.SpawnAutoDestroy(target.HitFxPrefab, target.Root.position, Quaternion.identity, 2f);

        DamagePopupFx.ShowDamage(target.DamagePopupPrefab, target.Root, dealt, isCrit);

        if (target.AudioSource != null && target.HitSound != null)
            target.AudioSource.PlayOneShot(target.HitSound);

        return dealt;
    }
}
