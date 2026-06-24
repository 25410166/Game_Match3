using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    public event Action OnPlayerDataChanged;

    [Header("Save Settings")]
    [SerializeField] private string saveFileName = "player_data.json";
    [SerializeField] private bool autoLoadOnAwake = true;
    [SerializeField] private bool autoSaveOnPauseOrQuit = true;

    [Header("EXP Require by Level (Level 1 -> 10)")]
    [SerializeField] private List<int> expRequireByLevel = new List<int>
    {
        100, 200, 400, 800, 1600, 3200, 6400, 12800, 25600, 51200
    };

    [Header("Player Data")]
    [SerializeField] private PlayerSaveData playerData = PlayerSaveData.CreateDefault();

    [Header("Monitor (Read Only)")]
    [SerializeField] private string monitorPlayerName;
    [SerializeField] private int monitorGold;
    [SerializeField] private int monitorDiamond;
    [SerializeField] private int monitorCurrentExp;

    [Header("Debug / Test")]
    [SerializeField] private bool testMode = false;
    [SerializeField] private bool demo = false;
    [SerializeField] private string demoDiscordUrl = "https://discord.gg/";
    [SerializeField] private string demoSteamUrl = "https://store.steampowered.com/";
    [SerializeField] private KeyCode testRewardHotkey = KeyCode.F;

    // Flag to prevent infinite loop during pet reward claiming
    private bool isSilentNotification = false;
    private bool pendingDemoCtaPopup = false;
    private bool demoCtaUnlocked = false;

    public PlayerSaveData Data => playerData;
    public bool DemoEnabled => demo;
    public string DemoDiscordUrl => demoDiscordUrl;
    public string DemoSteamUrl => demoSteamUrl;
    public bool IsDemoCtaUnlocked => demoCtaUnlocked;

    public bool HasConfirmedPlayerName => playerData != null && playerData.hasConfirmedPlayerName && !string.IsNullOrWhiteSpace(playerData.playerName);
    public bool HasSelectedStarterPet => playerData != null && playerData.hasSelectedStarterPet && playerData.starterPetId >= 0;

    private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
    private int MaxLevel => expRequireByLevel != null && expRequireByLevel.Count > 0 ? expRequireByLevel.Count : 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        if (autoLoadOnAwake)
        {
            LoadData();
            LoadSettings();
            ApplyTestModeIfEnabled();
        }
        else
        {
            EnsureDataValid();
            RefreshMonitorFields();
        }
    }

    private void OnValidate()
    {
        EnsureDataValid();
        RefreshMonitorFields();
    }

    private void Update()
    {
        if (!testMode || !Input.GetKeyDown(testRewardHotkey))
            return;

        GrantTestModeRewards();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoSaveOnPauseOrQuit)
        {
            SaveData();
            SaveSettings();
        }
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnPauseOrQuit)
        {
            SaveData();
            SaveSettings();
        }
    }

    public int GetExpRequireForLevel(int level)
    {
        if (expRequireByLevel == null || expRequireByLevel.Count == 0)
            return int.MaxValue;

        int index = Mathf.Clamp(level - 1, 0, expRequireByLevel.Count - 1);
        return expRequireByLevel[index];
    }

    public int GetMaxLevel() => MaxLevel;

    public string GetPlayerNameForDisplay()
    {
        return HasConfirmedPlayerName ? playerData.playerName : "";
    }

    public static bool IsValidPlayerName(string name, int maxLength = 24)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        string trimmed = name.Trim();
        if (trimmed.Length == 0 || trimmed.Length > maxLength) return false;

        for (int i = 0; i < trimmed.Length; i++)
        {
            char c = trimmed[i];
            if (!char.IsLetterOrDigit(c) && c != ' ')
                return false;
        }
        return true;
    }

    public bool TryConfirmPlayerName(string newName)
    {
        if (!IsValidPlayerName(newName)) return false;

        playerData.playerName = newName.Trim();
        playerData.hasConfirmedPlayerName = true;
        NotifyDataChanged();
        return true;
    }

    public bool ConfirmStarterPetSelection(int petId, string petName, int petLevel = 1)
    {
        if (petId < 0) return false;

        var existing = playerData.ownedPets.Find(x => x.petId == petId);
        if (existing == null)
        {
            playerData.ownedPets.Add(new OwnedPetData
            {
                petId = petId,
                petName = petName,
                petLevel = Mathf.Max(1, petLevel)
            });
        }
        else
        {
            existing.petName = string.IsNullOrEmpty(petName) ? existing.petName : petName;
            int nextLevel = Mathf.Max(1, petLevel);
            existing.petLevel = Mathf.Max(existing.petLevel, nextLevel);
        }

        playerData.hasSelectedStarterPet = true;
        playerData.starterPetId = petId;
        NotifyDataChanged();
        return true;
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        playerData.currentExp += amount;

        while (playerData.level < MaxLevel)
        {
            int require = GetExpRequireForLevel(playerData.level);
            if (playerData.currentExp < require) break;

            playerData.currentExp -= require;
            playerData.level++;
            EnqueuePendingLevelReward(playerData.level);
        }

        NotifyDataChanged();
    }

    public bool HasClaimedLevelReward(int level)
    {
        return playerData != null
            && playerData.claimedLevelRewardLevels != null
            && playerData.claimedLevelRewardLevels.Contains(level);
    }

    public bool HasPendingLevelReward(int level)
    {
        return playerData != null
            && playerData.pendingLevelRewardLevels != null
            && playerData.pendingLevelRewardLevels.Contains(level);
    }

    public int GetNextPendingLevelReward()
    {
        if (playerData == null || playerData.pendingLevelRewardLevels == null || playerData.pendingLevelRewardLevels.Count == 0)
            return -1;

        playerData.pendingLevelRewardLevels.Sort();
        return playerData.pendingLevelRewardLevels[0];
    }

    public bool TryClaimLevelReward(int level)
    {
        if (playerData == null || level < 2 || level > 10)
            return false;

        if (playerData.claimedLevelRewardLevels == null)
            playerData.claimedLevelRewardLevels = new List<int>();
        if (playerData.pendingLevelRewardLevels == null)
            playerData.pendingLevelRewardLevels = new List<int>();

        if (playerData.claimedLevelRewardLevels.Contains(level))
            return false;

        playerData.claimedLevelRewardLevels.Add(level);
        playerData.pendingLevelRewardLevels.Remove(level);
        NotifyDataChanged();
        return true;
    }

    private void EnqueuePendingLevelReward(int level)
    {
        if (playerData == null || level < 2 || level > 10)
            return;

        if (playerData.claimedLevelRewardLevels == null)
            playerData.claimedLevelRewardLevels = new List<int>();
        if (playerData.pendingLevelRewardLevels == null)
            playerData.pendingLevelRewardLevels = new List<int>();

        if (playerData.claimedLevelRewardLevels.Contains(level) || playerData.pendingLevelRewardLevels.Contains(level))
            return;

        playerData.pendingLevelRewardLevels.Add(level);
    }

    public void AddGold(int amount)
    {
        playerData.gold = Mathf.Max(0, playerData.gold + amount);
        NotifyDataChanged();
    }

    public void AddDiamond(int amount)
    {
        playerData.diamond = Mathf.Max(0, playerData.diamond + amount);
        NotifyDataChanged();
    }

    public void SetPlayerName(string newName)
    {
        if (!IsValidPlayerName(newName)) return;
        playerData.playerName = newName.Trim();
        NotifyDataChanged();
    }

    public void AddOrUpdateOwnedPet(int petId, string petName, int petLevel)
    {
        if (petId < 0) return;

        var existing = playerData.ownedPets.Find(x => x.petId == petId);
        if (existing == null)
        {
            playerData.ownedPets.Add(new OwnedPetData
            {
                petId = petId,
                petName = petName,
                petLevel = Mathf.Max(1, petLevel)
            });
            NotifyDataChanged();
            return;
        }

        existing.petName = string.IsNullOrEmpty(petName) ? existing.petName : petName;
        int nextLevel = Mathf.Max(1, petLevel);
        existing.petLevel = Mathf.Max(existing.petLevel, nextLevel);
        NotifyDataChanged();
    }

    public void AddOrUpdateOwnedCard(int cardId, int cardLevel, int quantity = 1)
    {
        if (cardId < 0 || quantity <= 0) return;

        var existing = playerData.ownedCards.Find(x => x.cardId == cardId && x.cardLevel == cardLevel);
        if (existing == null)
        {
            playerData.ownedCards.Add(new OwnedCardData
            {
                cardId = cardId,
                cardLevel = Mathf.Clamp(cardLevel, 1, 3),
                quantity = quantity
            });
            NotifyDataChanged();
            return;
        }

        existing.quantity += quantity;
        NotifyDataChanged();
    }

    public bool ConsumeOwnedCard(int cardId, int cardLevel, int amount = 1)
    {
        if (cardId < 0 || amount <= 0) return false;
        if (playerData == null || playerData.ownedCards == null) return false;

        for (int i = 0; i < playerData.ownedCards.Count; i++)
        {
            var c = playerData.ownedCards[i];
            if (c == null) continue;
            if (c.cardId == cardId && c.cardLevel == cardLevel)
            {
                c.quantity = Mathf.Max(0, c.quantity - amount);
                if (c.quantity <= 0)
                    playerData.ownedCards.RemoveAt(i);
                NotifyDataChanged();
                return true;
            }
        }

        return false;
    }

    public int GetMapWinCount(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId) || playerData == null || playerData.mapWins == null)
            return 0;

        for (int i = 0; i < playerData.mapWins.Count; i++)
        {
            MapWinData map = playerData.mapWins[i];
            if (map == null)
                continue;

            if (string.Equals(map.mapId, mapId, StringComparison.OrdinalIgnoreCase))
                return Mathf.Max(0, map.wins);
        }

        return 0;
    }

    public void AddMapWin(string mapId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(mapId) || amount <= 0)
            return;

        if (playerData.mapWins == null)
            playerData.mapWins = new List<MapWinData>();

        MapWinData data = playerData.mapWins.Find(x => x != null && string.Equals(x.mapId, mapId, StringComparison.OrdinalIgnoreCase));
        if (data == null)
        {
            data = new MapWinData
            {
                mapId = mapId,
                wins = amount
            };
            playerData.mapWins.Add(data);
        }
        else
        {
            data.wins = Mathf.Max(0, data.wins + amount);
        }

        NotifyDataChanged();
    }

    public void QueueDemoCtaPopup(string mapId, int petId)
    {
        if (!demo)
            return;

        if (!string.Equals(mapId, "110", StringComparison.OrdinalIgnoreCase))
            return;

        if (petId != 91)
            return;

        pendingDemoCtaPopup = true;
        demoCtaUnlocked = true;
    }

    public bool HasPendingDemoCtaPopup()
    {
        return pendingDemoCtaPopup;
    }

    public bool TryConsumePendingDemoCtaPopup()
    {
        if (!pendingDemoCtaPopup)
            return false;

        pendingDemoCtaPopup = false;
        return true;
    }

    public bool HasClaimedMapPetReward(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId) || playerData == null || playerData.claimedMapPetRewards == null)
            return false;

        for (int i = 0; i < playerData.claimedMapPetRewards.Count; i++)
        {
            string claimed = playerData.claimedMapPetRewards[i];
            if (string.Equals(claimed, mapId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public bool TryClaimMapPetReward(string mapId, int petId, string petName, int petLevel = 1)
    {
        if (string.IsNullOrWhiteSpace(mapId) || petId < 0)
            return false;

        if (HasClaimedMapPetReward(mapId))
            return false;

        // Enable silent mode to prevent infinite loop from refresh event
        isSilentNotification = true;
        try
        {
            AddOrUpdateOwnedPet(petId, petName, petLevel);

            if (playerData.claimedMapPetRewards == null)
                playerData.claimedMapPetRewards = new List<string>();

            playerData.claimedMapPetRewards.Add(mapId);
            NotifyDataChanged();
            return true;
        }
        finally
        {
            isSilentNotification = false;
        }
    }

    public bool HasClaimedMapGuardianReward(string mapId)
    {
        if (string.IsNullOrWhiteSpace(mapId) || playerData == null || playerData.claimedMapGuardianRewards == null)
            return false;

        for (int i = 0; i < playerData.claimedMapGuardianRewards.Count; i++)
        {
            string claimed = playerData.claimedMapGuardianRewards[i];
            if (string.Equals(claimed, mapId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public bool TryClaimMapGuardianReward(string mapId, int guardianId, int guardianLevel = 1)
    {
        if (string.IsNullOrWhiteSpace(mapId) || guardianId < 0 || playerData == null)
            return false;

        if (HasClaimedMapGuardianReward(mapId))
            return false;

        if (playerData.ownedGuardians == null)
            playerData.ownedGuardians = new List<OwnedGuardianData>();

        OwnedGuardianData owned = GetOwnedGuardian(guardianId);
        if (owned == null)
        {
            playerData.ownedGuardians.Add(new OwnedGuardianData
            {
                guardianId = guardianId,
                level = Mathf.Max(1, guardianLevel)
            });
        }
        else
        {
            owned.level = Mathf.Max(Mathf.Max(1, owned.level), guardianLevel);
        }

        if (playerData.equippedGuardianId < 0)
            playerData.equippedGuardianId = guardianId;

        if (playerData.claimedMapGuardianRewards == null)
            playerData.claimedMapGuardianRewards = new List<string>();

        playerData.claimedMapGuardianRewards.Add(mapId);
        NotifyDataChanged();
        SteamManager.Instance.ReportGuardianUnlocked(guardianId);
        return true;
    }
    public OwnedPetData GetOwnedPet(int petId)
    {
        if (playerData == null || playerData.ownedPets == null)
            return null;

        return playerData.ownedPets.Find(x => x.petId == petId);
    }

    public int GetLastSelectedPetId()
    {
        return playerData != null ? playerData.lastSelectedPetId : -1;
    }

    public bool TrySetLastSelectedPetId(int petId)
    {
        if (petId < 0)
            return false;

        if (GetOwnedPet(petId) == null)
            return false;

        playerData.lastSelectedPetId = petId;
        NotifyDataChanged();
        return true;
    }

    public bool TrySetOwnedPetLevel(int petId, int petLevel)
    {
        if (petId < 0)
            return false;

        OwnedPetData pet = GetOwnedPet(petId);
        if (pet == null)
            return false;

        pet.petLevel = Mathf.Max(1, petLevel);
        NotifyDataChanged();
        return true;
    }

    public int GetEquippedGuardianId()
    {
        return playerData != null ? playerData.equippedGuardianId : -1;
    }

    public OwnedGuardianData GetOwnedGuardian(int guardianId)
    {
        if (playerData == null || playerData.ownedGuardians == null)
            return null;

        return playerData.ownedGuardians.Find(x => x != null && x.guardianId == guardianId);
    }

    public int GetGuardianLevel(int guardianId)
    {
        OwnedGuardianData owned = GetOwnedGuardian(guardianId);
        return owned != null ? Mathf.Max(1, owned.level) : 1;
    }

    public bool IsGuardianOwned(int guardianId)
    {
        return GetOwnedGuardian(guardianId) != null;
    }

    public bool TryEquipGuardian(int guardianId)
    {
        if (guardianId < 0)
            return false;

        if (!IsGuardianOwned(guardianId))
            return false;

        playerData.equippedGuardianId = guardianId;
        NotifyDataChanged();
        return true;
    }

    public bool TryPurchaseGuardian(int guardianId, int diamondCost)
    {
        // Guardian purchase is disabled; guardians are earned from special maps.
        return false;
    }

    public bool TryUpgradeGuardian(int guardianId, int diamondCost, int maxLevel = 10)
    {
        if (guardianId < 0)
            return false;

        OwnedGuardianData owned = GetOwnedGuardian(guardianId);
        if (owned == null)
            return false;

        int safeMaxLevel = Mathf.Max(1, maxLevel);
        if (owned.level >= safeMaxLevel)
            return false;

        int cost = Mathf.Max(0, diamondCost);
        if (playerData == null || playerData.diamond < cost)
            return false;

        playerData.diamond = Mathf.Max(0, playerData.diamond - cost);
        owned.level = Mathf.Min(safeMaxLevel, owned.level + 1);
        NotifyDataChanged();
        SteamManager.Instance.ReportGuardianUpgraded(guardianId, owned.level);
        return true;
    }

    public int GetOwnedGemQuantity(int elementId, int gemLevel)
    {
        if (playerData == null || playerData.ownedGems == null)
            return 0;

        int total = 0;
        for (int i = 0; i < playerData.ownedGems.Count; i++)
        {
            OwnedGemData gem = playerData.ownedGems[i];
            if (gem == null)
                continue;

            if (gem.elementId == elementId && gem.gemLevel == gemLevel)
                total += Mathf.Max(0, gem.quantity);
        }

        return total;
    }

    public bool ConsumeOwnedGems(int elementId, int[] gemLevels)
    {
        if (elementId < 0 || gemLevels == null || gemLevels.Length == 0)
            return false;

        if (playerData == null || playerData.ownedGems == null)
            return false;

        int[] requiredByLevel = new int[6];
        for (int i = 0; i < gemLevels.Length; i++)
        {
            int level = gemLevels[i];
            if (level < 1 || level > 5)
                return false;
            requiredByLevel[level]++;
        }

        for (int level = 1; level <= 5; level++)
        {
            if (requiredByLevel[level] <= 0)
                continue;

            if (GetOwnedGemQuantity(elementId, level) < requiredByLevel[level])
                return false;
        }

        for (int level = 1; level <= 5; level++)
        {
            int remain = requiredByLevel[level];
            if (remain <= 0)
                continue;

            for (int i = 0; i < playerData.ownedGems.Count && remain > 0; i++)
            {
                OwnedGemData gem = playerData.ownedGems[i];
                if (gem == null)
                    continue;

                if (gem.elementId != elementId || gem.gemLevel != level || gem.quantity <= 0)
                    continue;

                int use = Mathf.Min(gem.quantity, remain);
                gem.quantity -= use;
                remain -= use;
            }
        }

        playerData.ownedGems.RemoveAll(x => x == null || x.quantity <= 0);
        NotifyDataChanged();
        return true;
    }

    public void AddOrUpdateOwnedGem(int elementId, int gemLevel, int quantity = 1)
    {
        if (elementId < 0 || quantity <= 0) return;

        var existing = playerData.ownedGems.Find(x => x.elementId == elementId && x.gemLevel == gemLevel);
        if (existing == null)
        {
            playerData.ownedGems.Add(new OwnedGemData
            {
                elementId = elementId,
                gemLevel = Mathf.Clamp(gemLevel, 1, 5),
                quantity = quantity
            });
            NotifyDataChanged();
            return;
        }

        existing.quantity += quantity;
        NotifyDataChanged();
    }

    [ContextMenu("Player/Refresh Monitor")]
    public void RefreshMonitorFields()
    {
        if (playerData == null)
        {
            monitorPlayerName = string.Empty;
            monitorGold = 0;
            monitorDiamond = 0;
            monitorCurrentExp = 0;
            return;
        }

        monitorPlayerName = playerData.playerName;
        monitorGold = playerData.gold;
        monitorDiamond = playerData.diamond;
        monitorCurrentExp = playerData.currentExp;
    }

    [ContextMenu("Player/Reset New User Data")]
    public void ResetToNewUser()
    {
        playerData = PlayerSaveData.CreateDefault();
        NotifyDataChanged();
    }

    [ContextMenu("Debug/Apply Test Mode (Level 10 + 1M Gold)")]
    private void ApplyTestModeIfEnabled()
    {
        if (!testMode)
            return;

        GrantTestModeRewards();
    }

    private void GrantTestModeRewards()
    {
        if (playerData == null)
            playerData = PlayerSaveData.CreateDefault();

        playerData.currentExp += 51200;
        playerData.gold += 100000;
        playerData.diamond += 100000;

        while (playerData.level < MaxLevel)
        {
            int require = GetExpRequireForLevel(playerData.level);
            if (playerData.currentExp < require) break;

            playerData.currentExp -= require;
            playerData.level++;
        }

        NotifyDataChanged();
        Debug.Log($"[Test Mode] Added: +51200 EXP, +100000 Gold, +100000 Diamond | Current Level: {playerData.level}, Gold: {playerData.gold}, Diamond: {playerData.diamond}, EXP: {playerData.currentExp}");
    }

    [ContextMenu("Player/Save Data")]
    public void SaveData()
    {
        try
        {
            EnsureDataValid();
            string json = JsonUtility.ToJson(playerData, true);
            File.WriteAllText(SavePath, json);
            RefreshMonitorFields();
            Debug.Log("[PlayerManager] Save success: " + SavePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[PlayerManager] Save failed: " + ex.Message);
        }
    }

    [ContextMenu("Player/Load Data")]
    public void LoadData()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                playerData = PlayerSaveData.CreateDefault();
                NotifyDataChanged();
                return;
            }

            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                playerData = PlayerSaveData.CreateDefault();
                NotifyDataChanged();
                return;
            }

            playerData = JsonUtility.FromJson<PlayerSaveData>(json);
            EnsureDataValid();
            NotifyDataChanged();
            Debug.Log("[PlayerManager] Load success: " + SavePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[PlayerManager] Load failed: " + ex.Message);
            playerData = PlayerSaveData.CreateDefault();
            NotifyDataChanged();
        }
    }

    [ContextMenu("Settings/Save Settings")]
    public void SaveSettings()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings();
        }
    }

    [ContextMenu("Settings/Load Settings")]
    public void LoadSettings()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ReloadSettings();
        }
    }

    private void EnsureDataValid()
    {
        if (playerData == null)
            playerData = PlayerSaveData.CreateDefault();

        playerData.level = Mathf.Clamp(playerData.level, 1, MaxLevel);
        playerData.currentExp = Mathf.Max(0, playerData.currentExp);
        playerData.gold = Mathf.Max(0, playerData.gold);
        playerData.diamond = Mathf.Max(0, playerData.diamond);

        if (playerData.ownedPets == null) playerData.ownedPets = new List<OwnedPetData>();
        if (playerData.ownedCards == null) playerData.ownedCards = new List<OwnedCardData>();
        if (playerData.ownedGems == null) playerData.ownedGems = new List<OwnedGemData>();
        if (playerData.ownedGuardians == null) playerData.ownedGuardians = new List<OwnedGuardianData>();
        if (playerData.mapWins == null) playerData.mapWins = new List<MapWinData>();
        if (playerData.claimedMapPetRewards == null) playerData.claimedMapPetRewards = new List<string>();
        if (playerData.claimedMapGuardianRewards == null) playerData.claimedMapGuardianRewards = new List<string>();

        if (playerData.lastSelectedPetId >= 0)
        {
            bool hasPet = playerData.ownedPets.Exists(p => p != null && p.petId == playerData.lastSelectedPetId);
            if (!hasPet)
                playerData.lastSelectedPetId = -1;
        }

        string normalized = string.IsNullOrWhiteSpace(playerData.playerName) ? string.Empty : playerData.playerName.Trim();
        if (!playerData.hasConfirmedPlayerName)
        {
            if (IsValidPlayerName(normalized) && !string.Equals(normalized, "Player", StringComparison.OrdinalIgnoreCase))
            {
                playerData.playerName = normalized;
                playerData.hasConfirmedPlayerName = true;
            }
            else
            {
                playerData.playerName = string.Empty;
            }
        }
        else
        {
            if (!IsValidPlayerName(normalized))
            {
                playerData.playerName = string.Empty;
                playerData.hasConfirmedPlayerName = false;
            }
            else
            {
                playerData.playerName = normalized;
            }
        }

        if (!playerData.hasSelectedStarterPet)
        {
            if (playerData.ownedPets.Count > 0)
            {
                playerData.hasSelectedStarterPet = true;
                if (playerData.starterPetId < 0)
                    playerData.starterPetId = playerData.ownedPets[0].petId;
            }
        }
        else if (playerData.starterPetId < 0)
        {
            playerData.hasSelectedStarterPet = false;
        }

        if (playerData.ownedGuardians.Count == 0)
        {
            playerData.equippedGuardianId = -1;
        }
        else
        {
            bool hasEquipped = playerData.ownedGuardians.Exists(g => g != null && g.guardianId == playerData.equippedGuardianId);
            if (!hasEquipped)
                playerData.equippedGuardianId = playerData.ownedGuardians[0].guardianId;
        }

        for (int i = 0; i < playerData.ownedGuardians.Count; i++)
        {
            OwnedGuardianData g = playerData.ownedGuardians[i];
            if (g == null)
                continue;

            g.level = Mathf.Max(1, g.level);
        }
    }

    private void NotifyDataChanged()
    {
        EnsureDataValid();
        RefreshMonitorFields();
        // Skip event notification if silent mode (to prevent infinite loop during reward claiming)
        if (!isSilentNotification && OnPlayerDataChanged != null)
            OnPlayerDataChanged.Invoke();
    }
}

[Serializable]
public class PlayerSaveData
{
    public string playerName = string.Empty;
    public bool hasConfirmedPlayerName = false;
    public bool hasSelectedStarterPet = false;
    public bool tutorial1Triggered = false;
    public bool tutorial1Completed = false;
    public bool tutorial1Skipped = false;
    public bool tutorial2Triggered = false;
    public bool tutorial2Completed = false;
    public bool tutorial2Skipped = false;
    public bool tutorial3Triggered = false;
    public bool tutorial3Completed = false;
    public bool tutorial3Skipped = false;
    public bool tutorial3FirstUpgradeGuaranteedUsed = false;
    public int starterPetId = -1;
    public int lastSelectedPetId = -1;
    public int level = 1;
    public int currentExp = 0;
    public int gold = 0;
    public int diamond = 0;
    public int equippedGuardianId = -1;

    public List<OwnedPetData> ownedPets = new List<OwnedPetData>();
    public List<OwnedCardData> ownedCards = new List<OwnedCardData>();
    public List<OwnedGemData> ownedGems = new List<OwnedGemData>();
    public List<OwnedGuardianData> ownedGuardians = new List<OwnedGuardianData>();
    public List<MapWinData> mapWins = new List<MapWinData>();
    public List<string> claimedMapPetRewards = new List<string>();
    public List<string> claimedMapGuardianRewards = new List<string>();
    public List<int> claimedLevelRewardLevels = new List<int>();
    public List<int> pendingLevelRewardLevels = new List<int>();

    public static PlayerSaveData CreateDefault()
    {
        return new PlayerSaveData
        {
            playerName = string.Empty,
            hasConfirmedPlayerName = false,
            hasSelectedStarterPet = false,
            tutorial1Triggered = false,
            tutorial1Completed = false,
            tutorial1Skipped = false,
            tutorial2Triggered = false,
            tutorial2Completed = false,
            tutorial2Skipped = false,
            tutorial3Triggered = false,
            tutorial3Completed = false,
            tutorial3Skipped = false,
            tutorial3FirstUpgradeGuaranteedUsed = false,
            starterPetId = -1,
            lastSelectedPetId = -1,
            level = 1,
            currentExp = 0,
            gold = 0,
            diamond = 0,
            equippedGuardianId = -1,
            ownedPets = new List<OwnedPetData>(),
            ownedCards = new List<OwnedCardData>(),
            ownedGems = new List<OwnedGemData>(),
            ownedGuardians = new List<OwnedGuardianData>(),
            mapWins = new List<MapWinData>(),
            claimedMapPetRewards = new List<string>(),
            claimedMapGuardianRewards = new List<string>(),
            claimedLevelRewardLevels = new List<int>(),
            pendingLevelRewardLevels = new List<int>()
        };
    }
}

[Serializable]
public class MapWinData
{
    public string mapId;
    public int wins;
}

[Serializable]
public class OwnedPetData
{
    public int petId;
    public string petName;
    public int petLevel = 1;
}

[Serializable]
public class OwnedCardData
{
    public int cardId;
    public int cardLevel = 1;
    public int quantity = 1;
}

[Serializable]
public class OwnedGemData
{
    public int elementId;
    public int gemLevel = 1;
    public int quantity = 1;
}


[Serializable]
public class OwnedGuardianData
{
    public int guardianId;
    public int level = 1;
}







