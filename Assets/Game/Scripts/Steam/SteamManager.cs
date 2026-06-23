using System;
using UnityEngine;

#if STEAMWORKS_NET && !DISABLESTEAMWORKS
using Steamworks;
#endif

public class SteamManager : MonoBehaviour
{
    private const string AchievementPrefix = "steam.achievement.";
    private const string BattleCountKey = "steam.stat.battles_completed";
    private const string ShopPurchaseCountKey = "steam.stat.shop_purchases";

    private static readonly string[] CoreAchievementKeys =
    {
        SteamAchievementKeys.TheJourneyBegins,
        SteamAchievementKeys.GrowingStronger,
        SteamAchievementKeys.SmartShopper,
        SteamAchievementKeys.BattleHardened,
        SteamAchievementKeys.RisingCompanion,
        SteamAchievementKeys.HeroAwakens,
        SteamAchievementKeys.PowerWithin,
        SteamAchievementKeys.PeakEvolution
    };

    private static SteamManager instance;
    public static SteamManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject(nameof(SteamManager));
                instance = go.AddComponent<SteamManager>();
            }

            return instance;
        }
    }

    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private bool debugLogs = true;

    private bool subscribedToPlayerManager;
    private bool steamInitialized;
    private bool steamStatsReady;

#if STEAMWORKS_NET && !DISABLESTEAMWORKS
    private Callback<UserStatsReceived_t> userStatsReceivedCallback;
    private Callback<UserStatsStored_t> userStatsStoredCallback;
    private Callback<UserAchievementStored_t> userAchievementStoredCallback;
#endif

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        InitializeSteam();
        BindPlayerManager();
        RefreshAchievementsFromPlayerData();
    }

    private void Update()
    {
        RunSteamCallbacks();
    }

    private void OnEnable()
    {
        BindPlayerManager();
    }

    private void OnDisable()
    {
        UnbindPlayerManager();
    }

    private void OnDestroy()
    {
        if (instance == this)
            ShutdownSteam();
    }

    private void OnApplicationQuit()
    {
        if (instance == this)
            ShutdownSteam();
    }

    public void ReportBattleCompleted()
    {
        int battlesCompleted = PlayerPrefs.GetInt(BattleCountKey, 0) + 1;
        PlayerPrefs.SetInt(BattleCountKey, battlesCompleted);
        PlayerPrefs.Save();

        if (debugLogs)
            Debug.Log($"[SteamManager] ReportBattleCompleted => {battlesCompleted}");

        if (battlesCompleted >= 1)
            UnlockAchievement(SteamAchievementKeys.TheJourneyBegins);
        if (battlesCompleted >= 10)
            UnlockAchievement(SteamAchievementKeys.BattleHardened);
    }

    public void ReportShopPurchase()
    {
        int purchaseCount = PlayerPrefs.GetInt(ShopPurchaseCountKey, 0) + 1;
        PlayerPrefs.SetInt(ShopPurchaseCountKey, purchaseCount);
        PlayerPrefs.Save();

        if (debugLogs)
            Debug.Log($"[SteamManager] ReportShopPurchase => {purchaseCount}");

        if (purchaseCount >= 1)
            UnlockAchievement(SteamAchievementKeys.SmartShopper);
    }

    public void ReportPetUpgraded(int petId, int newLevel)
    {
        if (debugLogs)
            Debug.Log($"[SteamManager] ReportPetUpgraded petId={petId} level={newLevel}");

        if (newLevel >= 2)
            UnlockAchievement(SteamAchievementKeys.GrowingStronger);
        if (newLevel >= 5)
            UnlockAchievement(SteamAchievementKeys.RisingCompanion);
        if (newLevel >= 8)
            UnlockAchievement(SteamAchievementKeys.PeakEvolution);
    }

    public void ReportGuardianUnlocked(int guardianId)
    {
        if (debugLogs)
            Debug.Log($"[SteamManager] ReportGuardianUnlocked guardianId={guardianId}");

        UnlockAchievement(SteamAchievementKeys.HeroAwakens);
    }

    public void ReportGuardianUpgraded(int guardianId, int newLevel)
    {
        if (debugLogs)
            Debug.Log($"[SteamManager] ReportGuardianUpgraded guardianId={guardianId} level={newLevel}");

        if (newLevel >= 2)
            UnlockAchievement(SteamAchievementKeys.PowerWithin);
    }

    public void RefreshAchievementsFromPlayerData()
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || playerManager.Data == null)
            return;

        PlayerSaveData data = playerManager.Data;
        int maxPetLevel = 0;
        if (data.ownedPets != null)
        {
            for (int i = 0; i < data.ownedPets.Count; i++)
            {
                OwnedPetData pet = data.ownedPets[i];
                if (pet == null)
                    continue;

                maxPetLevel = Mathf.Max(maxPetLevel, Mathf.Max(1, pet.petLevel));
            }
        }

        if (maxPetLevel >= 2)
            UnlockAchievement(SteamAchievementKeys.GrowingStronger);
        if (maxPetLevel >= 5)
            UnlockAchievement(SteamAchievementKeys.RisingCompanion);
        if (maxPetLevel >= 8)
            UnlockAchievement(SteamAchievementKeys.PeakEvolution);

        int ownedGuardianCount = 0;
        int maxGuardianLevel = 0;
        if (data.ownedGuardians != null)
        {
            for (int i = 0; i < data.ownedGuardians.Count; i++)
            {
                OwnedGuardianData guardian = data.ownedGuardians[i];
                if (guardian == null)
                    continue;

                ownedGuardianCount++;
                maxGuardianLevel = Mathf.Max(maxGuardianLevel, Mathf.Max(1, guardian.level));
            }
        }

        if (ownedGuardianCount >= 1)
            UnlockAchievement(SteamAchievementKeys.HeroAwakens);
        if (maxGuardianLevel >= 2)
            UnlockAchievement(SteamAchievementKeys.PowerWithin);

        int completedBattles = PlayerPrefs.GetInt(BattleCountKey, 0);
        if (completedBattles >= 1)
            UnlockAchievement(SteamAchievementKeys.TheJourneyBegins);
        if (completedBattles >= 10)
            UnlockAchievement(SteamAchievementKeys.BattleHardened);

        int shopPurchases = PlayerPrefs.GetInt(ShopPurchaseCountKey, 0);
        if (shopPurchases >= 1)
            UnlockAchievement(SteamAchievementKeys.SmartShopper);
    }

    public bool IsAchievementUnlocked(string achievementKey)
    {
        if (string.IsNullOrWhiteSpace(achievementKey))
            return false;

        if (TryGetSteamAchievementState(achievementKey, out bool steamUnlocked))
            return steamUnlocked;

        return PlayerPrefs.GetInt(AchievementPrefix + achievementKey, 0) == 1;
    }

    public bool UnlockAchievement(string achievementKey)
    {
        if (string.IsNullOrWhiteSpace(achievementKey) || IsAchievementUnlocked(achievementKey))
            return false;

        PlayerPrefs.SetInt(AchievementPrefix + achievementKey, 1);
        PlayerPrefs.Save();

        if (debugLogs)
            Debug.Log($"[SteamManager] Achievement unlocked => {achievementKey}");

        TryUnlockSteamAchievement(achievementKey);
        TryUnlockLegendAchievement();
        return true;
    }

    [ContextMenu("Steam/Reset All Achievements")]
    public void ResetAllAchievements()
    {
        for (int i = 0; i < CoreAchievementKeys.Length; i++)
        {
            TryClearSteamAchievement(CoreAchievementKeys[i]);
            PlayerPrefs.DeleteKey(AchievementPrefix + CoreAchievementKeys[i]);
        }

        TryClearSteamAchievement(SteamAchievementKeys.PixmoxLegend);
        PlayerPrefs.DeleteKey(AchievementPrefix + SteamAchievementKeys.PixmoxLegend);
        PlayerPrefs.DeleteKey(BattleCountKey);
        PlayerPrefs.DeleteKey(ShopPurchaseCountKey);
        PlayerPrefs.Save();

        TryResetSteamStats();

        if (debugLogs)
            Debug.Log("[SteamManager] ResetAllAchievements complete.");
    }

    private void TryUnlockLegendAchievement()
    {
        if (IsAchievementUnlocked(SteamAchievementKeys.PixmoxLegend))
            return;

        for (int i = 0; i < CoreAchievementKeys.Length; i++)
        {
            if (!IsAchievementUnlocked(CoreAchievementKeys[i]))
                return;
        }

        PlayerPrefs.SetInt(AchievementPrefix + SteamAchievementKeys.PixmoxLegend, 1);
        PlayerPrefs.Save();

        if (debugLogs)
            Debug.Log($"[SteamManager] Achievement unlocked => {SteamAchievementKeys.PixmoxLegend}");

        TryUnlockSteamAchievement(SteamAchievementKeys.PixmoxLegend);
    }

    private void TryUnlockSteamAchievement(string achievementKey)
    {
        if (!steamInitialized || !steamStatsReady)
        {
            if (debugLogs)
                Debug.Log($"[SteamManager] Steam not ready, keep local unlock => {achievementKey}");
            return;
        }

#if STEAMWORKS_NET && !DISABLESTEAMWORKS
        try
        {
            if (SteamUserStats.GetAchievement(achievementKey, out bool alreadyUnlocked) && alreadyUnlocked)
                return;

            bool setResult = SteamUserStats.SetAchievement(achievementKey);
            bool storeResult = SteamUserStats.StoreStats();

            if (debugLogs)
                Debug.Log($"[SteamManager] Steam unlock => {achievementKey}, set={setResult}, store={storeResult}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SteamManager] Steam unlock failed for {achievementKey}: {ex.Message}");
        }
#endif
    }

    private void InitializeSteam()
    {
#if STEAMWORKS_NET && !DISABLESTEAMWORKS
        try
        {
            string errorMessage;
            ESteamAPIInitResult initResult = SteamAPI.InitEx(out errorMessage);
            steamInitialized = initResult == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;

            if (!steamInitialized)
            {
                if (debugLogs)
                    Debug.LogWarning($"[SteamManager] Steam init failed: {initResult} - {errorMessage}");
                return;
            }

            userStatsReceivedCallback = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);
            userStatsStoredCallback = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
            userAchievementStoredCallback = Callback<UserAchievementStored_t>.Create(OnUserAchievementStored);

            steamStatsReady = true;

            if (debugLogs)
                Debug.Log("[SteamManager] Steam initialized and stats are ready.");
        }
        catch (Exception ex)
        {
            steamInitialized = false;
            steamStatsReady = false;
            Debug.LogWarning($"[SteamManager] Steam init exception: {ex.Message}");
        }
#else
        steamInitialized = false;
        steamStatsReady = false;

        if (debugLogs)
            Debug.Log("[SteamManager] Steamworks.NET not available, using local fallback only.");
#endif
    }

    private void RunSteamCallbacks()
    {
#if STEAMWORKS_NET && !DISABLESTEAMWORKS
        if (!steamInitialized)
            return;

        try
        {
            SteamAPI.RunCallbacks();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SteamManager] Steam callbacks failed: {ex.Message}");
        }
#endif
    }

    private void ShutdownSteam()
    {
#if STEAMWORKS_NET && !DISABLESTEAMWORKS
        if (!steamInitialized)
            return;

        try
        {
            SteamAPI.Shutdown();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SteamManager] Steam shutdown failed: {ex.Message}");
        }
        finally
        {
            steamInitialized = false;
            steamStatsReady = false;
        }
#endif
    }

    private bool TryGetSteamAchievementState(string achievementKey, out bool unlocked)
    {
        unlocked = false;

#if STEAMWORKS_NET && !DISABLESTEAMWORKS
        if (!steamInitialized || !steamStatsReady)
            return false;

        try
        {
            return SteamUserStats.GetAchievement(achievementKey, out unlocked);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SteamManager] Steam get achievement failed for {achievementKey}: {ex.Message}");
            return false;
        }
#else
        return false;
#endif
    }

    private void TryClearSteamAchievement(string achievementKey)
    {
#if STEAMWORKS_NET && !DISABLESTEAMWORKS
        if (!steamInitialized || !steamStatsReady || string.IsNullOrWhiteSpace(achievementKey))
            return;

        try
        {
            SteamUserStats.ClearAchievement(achievementKey);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SteamManager] Steam clear achievement failed for {achievementKey}: {ex.Message}");
        }
#endif
    }

    private void TryResetSteamStats()
    {
#if STEAMWORKS_NET && !DISABLESTEAMWORKS
        if (!steamInitialized || !steamStatsReady)
            return;

        try
        {
            bool resetResult = SteamUserStats.ResetAllStats(true);
            bool storeResult = SteamUserStats.StoreStats();

            if (debugLogs)
                Debug.Log($"[SteamManager] Steam reset stats => reset={resetResult}, store={storeResult}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SteamManager] Steam reset stats failed: {ex.Message}");
        }
#endif
    }

    private void BindPlayerManager()
    {
        if (subscribedToPlayerManager || PlayerManager.Instance == null)
            return;

        PlayerManager.Instance.OnPlayerDataChanged += HandlePlayerDataChanged;
        subscribedToPlayerManager = true;
    }

    private void UnbindPlayerManager()
    {
        if (!subscribedToPlayerManager || PlayerManager.Instance == null)
            return;

        PlayerManager.Instance.OnPlayerDataChanged -= HandlePlayerDataChanged;
        subscribedToPlayerManager = false;
    }

    private void HandlePlayerDataChanged()
    {
        RefreshAchievementsFromPlayerData();
    }

#if STEAMWORKS_NET && !DISABLESTEAMWORKS
    private void OnUserStatsReceived(UserStatsReceived_t callback)
    {
        if (!steamInitialized)
            return;

        steamStatsReady = callback.m_eResult == EResult.k_EResultOK;

        if (debugLogs)
            Debug.Log($"[SteamManager] UserStatsReceived => {callback.m_eResult}");

        if (steamStatsReady)
            RefreshAchievementsFromPlayerData();
    }

    private void OnUserStatsStored(UserStatsStored_t callback)
    {
        if (debugLogs)
            Debug.Log($"[SteamManager] UserStatsStored => {callback.m_eResult}");
    }

    private void OnUserAchievementStored(UserAchievementStored_t callback)
    {
        if (debugLogs)
            Debug.Log($"[SteamManager] UserAchievementStored => {callback.m_rgchAchievementName}");
    }
#endif
}

