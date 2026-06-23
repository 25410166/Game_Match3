public static class StatSystem
{
    public static int AddClamped(int current, int max, int amount)
    {
        if (amount <= 0 || max <= 0)
            return current;

        return UnityEngine.Mathf.Clamp(current + amount, 0, max);
    }

    public static int ApplyDamageWithArmor(ref int hp, int incomingDamage, int armor)
    {
        if (hp <= 0 || incomingDamage <= 0)
            return 0;

        int finalDamage = UnityEngine.Mathf.Max(1, incomingDamage - UnityEngine.Mathf.Max(0, armor));
        int hpBefore = hp;
        hp = UnityEngine.Mathf.Max(0, hp - finalDamage);
        return hpBefore - hp;
    }

    public static int ApplyDamageWithShieldAndArmor(ref int hp, ref int shield, int incomingDamage, int armor)
    {
        if (hp <= 0 || incomingDamage <= 0)
            return 0;

        int remaining = incomingDamage;
        if (shield > 0)
        {
            int absorbed = UnityEngine.Mathf.Min(shield, remaining);
            shield -= absorbed;
            remaining -= absorbed;
        }

        if (remaining <= 0)
            return 0;

        return ApplyDamageWithArmor(ref hp, remaining, armor);
    }
}
