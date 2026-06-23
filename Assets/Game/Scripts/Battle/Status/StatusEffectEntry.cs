using System;

[Serializable]
public class StatusEffectEntry
{
    public StatusEffectType type;
    public int remainingTurns;
    public float value;
    public bool useDirectValue;

    public StatusEffectEntry(StatusEffectType type, int remainingTurns, float value)
        : this(type, remainingTurns, value, false)
    {
    }

    public StatusEffectEntry(StatusEffectType type, int remainingTurns, float value, bool useDirectValue)
    {
        this.type = type;
        this.remainingTurns = remainingTurns;
        this.value = value;
        this.useDirectValue = useDirectValue;
    }
}
