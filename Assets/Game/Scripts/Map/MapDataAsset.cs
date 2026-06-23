using System.Collections.Generic;
using UnityEngine;

public class MapDataAsset : ScriptableObject
{
    [Header("Map Info")]
    public int area;
    public string mapId;
    public string mapName;
    public int reqUserLevel;
    public int petIdSpawn;
    public int petLevelSpawn = 1;
    public int idGuadiant;
    public int levelGuadiant = 1;
    public int rewardPetId = -1; // Optional: pet id rewarded on completion (defaults to petIdSpawn)
    public int rewardGuardiantId = -1; // Optional: guardian id rewarded on completion
    public int reqWinsPet;

    [Header("Rewards")]
    public List<MapRewardData> rewards = new List<MapRewardData>();
}



