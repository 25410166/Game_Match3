using UnityEngine;

public enum SkillAttackType
{
    Melee = 0,
    Range = 1
}

public enum SkillRangeType
{
    Projectile = 0,
    DirectFX = 1,
    DirectImpact = 2,
    Audition = 3
}

public enum SkillType
{
    None = 0,
    Audition = 1
}

public enum SkillBoardEffectType
{
    None = 0,
    Row = 1,
    Column = 2,
    Swap = 3,
    Match = 4
}

[CreateAssetMenu(fileName = "SkillData", menuName = "Battle/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Identity")]
    public int skillId;
    public string skillName;
    public string desSkill;

    [Header("Delivery")]
    public SkillAttackType attackType = SkillAttackType.Range;
    public SkillRangeType rangeType = SkillRangeType.DirectFX;
    public SkillType typeSkill = SkillType.None;

    [Header("Damage")]
    [Min(1)] public int hitCount = 1;
    [Min(0f)] public float hitDelay = 0f;
    [Min(0f)] public float damageMultiplier = 1f;

    [Header("Audition")]
    [Min(1)] public int auditionSequenceLengthMin = 6;
    [Min(1)] public int auditionSequenceLengthMax = 8;
    [Min(0.1f)] public float auditionRoundDuration = 5f;
    [Range(0f, 1f)] public float auditionPerfectZoneStart = 0.78f;
    [Range(0f, 1f)] public float auditionPerfectZoneEnd = 0.88f;
    [Min(0f)] public float auditionPerfectMultiplier = 3f;
    [Min(0f)] public float auditionGreatMultiplier = 2f;
    [Min(0f)] public float auditionMissMultiplier = 1f;

    [Header("Status Effect")]
    public StatusEffectType effect = StatusEffectType.None;
    [Min(0)] public int roundEffect = 0;

    [Header("Plays")]
    [Min(1)] public int animationPlayCount = 1;
    [Min(1)] public int fxPlayCount = 1;

    [Header("Visual")]
    public Sprite skillSprite;
    public GameObject fxPrefab;
    public GameObject projectilePrefab;

    [Header("Cost")]
    [Min(0)] public int manaCost;
    [Min(0)] public int rageCost;
    [Range(0f, 100f)] public float hpCostPercent;

    [Header("Board")]
    public int[] gemTypesAffected;
    public SkillBoardEffectType boardEffectType = SkillBoardEffectType.None;

    [Header("Timing")]
    [Min(0f)] public float animationDuration = 1.2f;
}
