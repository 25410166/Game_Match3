using UnityEngine;

public static class MapLevelRequirementUIHelper
{
    public static bool CanAccessMap(MapDataAsset mapData)
    {
        if (mapData == null)
            return false;

        int requiredLevel = Mathf.Max(1, mapData.reqUserLevel);
        int playerLevel = PlayerManager.Instance != null && PlayerManager.Instance.Data != null
            ? Mathf.Max(1, PlayerManager.Instance.Data.level)
            : 1;

        return playerLevel >= requiredLevel;
    }

    public static string BuildWinsProgressText(MapDataAsset mapData, int wins)
    {
        if (mapData == null)
            return string.Empty;

        string requirementText = GetLevelRequirementText(mapData.reqUserLevel);
        string progressText = wins + "/" + Mathf.Max(0, mapData.reqWinsPet);
        return string.IsNullOrWhiteSpace(requirementText)
            ? progressText
            : requirementText + "\n" + progressText;
    }

    public static string GetLevelRequirementText(int requiredLevel)
    {
        int safeLevel = Mathf.Max(1, requiredLevel);
        string key = "levelrequire" + safeLevel;
        string fallback = "Require Level " + safeLevel;

        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
            return LocalizationManager.Instance.GetText(key, fallback);

        return fallback;
    }
}
