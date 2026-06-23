public readonly struct SkillResult
{
    public readonly bool Executed;
    public readonly int TotalDamage;
    public readonly int HitCount;

    public SkillResult(bool executed, int totalDamage, int hitCount)
    {
        Executed = executed;
        TotalDamage = totalDamage;
        HitCount = hitCount;
    }

    public static SkillResult Failed => new SkillResult(false, 0, 0);
}
