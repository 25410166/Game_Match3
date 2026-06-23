using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapDatabase", menuName = "Map/Map Database")]
public class MapDatabase : ScriptableObject
{
    public List<MapDataAsset> maps = new List<MapDataAsset>();

    public MapDataAsset GetMapById(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId) || maps == null)
            return null;

        for (int i = 0; i < maps.Count; i++)
        {
            MapDataAsset map = maps[i];
            if (map == null)
                continue;

            if (string.Equals(map.mapId, mapId, StringComparison.OrdinalIgnoreCase))
                return map;
        }

        return null;
    }
}

public enum MapRewardType
{
    GOLD,
    EXP,
    GEM,
    DIAMOND
}

[Serializable]
public class MapRewardData
{
    public string mapId;
    public MapRewardType rewardType;
    public string rewardId;
    public int gemElementId = -1;
    public int gemLevel = 1;
    public int amountMin;
    public int amountMax;
    public int weight;
}
