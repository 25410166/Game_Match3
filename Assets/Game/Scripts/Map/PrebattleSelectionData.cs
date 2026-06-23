using System.Collections.Generic;

public static class PrebattleSelectionData
{
    public static string MapId;
    public static int MapArea = -1;
    public static int PlayerPetId = -1;
    public static int PlayerPetLevel = 1;
    public static int EnemyPetId = -1;
    public static int EnemyPetLevel = 1;
    public static int EnemyGuardianId = 0;
    public static int EnemyGuardianLevel = 1;
    public static int GuardianId = -1;
    public static int GuardianLevel = 1;
    public static readonly List<PrebattleCardData> SelectedCards = new List<PrebattleCardData>();

    public static void Clear()
    {
        MapId = string.Empty;
        MapArea = -1;
        PlayerPetId = -1;
        PlayerPetLevel = 1;
        EnemyPetId = -1;
        EnemyPetLevel = 1;
        EnemyGuardianId = 0;
        EnemyGuardianLevel = 1;
        GuardianId = -1;
        GuardianLevel = 1;
        SelectedCards.Clear();
    }
}

public class PrebattleCardData
{
    public int cardId;
    public int cardLevel;
}
