using UnityEngine;

public enum AuditionResultType
{
    Perfect = 0,
    Great = 1,
    Miss = 2
}

public readonly struct AuditionMiniGameResult
{
    public static readonly AuditionMiniGameResult DefaultGreat = new AuditionMiniGameResult(AuditionResultType.Great, 2f, 0, 0);
    public static readonly AuditionMiniGameResult DefaultMiss = new AuditionMiniGameResult(AuditionResultType.Miss, 1f, 0, 0);

    public AuditionResultType ResultType { get; }
    public float DamageMultiplier { get; }
    public int SuccessfulInputs { get; }
    public int SequenceLength { get; }

    public AuditionMiniGameResult(AuditionResultType resultType, float damageMultiplier, int successfulInputs, int sequenceLength)
    {
        ResultType = resultType;
        DamageMultiplier = Mathf.Max(0f, damageMultiplier);
        SuccessfulInputs = Mathf.Max(0, successfulInputs);
        SequenceLength = Mathf.Max(0, sequenceLength);
    }
}
