using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Serializable]
    public class BattleRuntimeData
    {
        public int playerPetId = -1;
        public int playerLevel = 1;
        public int enemyPetId = -1;
        public int enemyLevel = 1;

        public bool HasValidData => playerPetId >= 0 && enemyPetId >= 0;
    }

    [Serializable]
    public class MapEnemyData
    {
        public int petId = -1;
        public int level = 1;
        public int guardianId = 0;
        public int guardianLevel = 1;
    }

    public enum Turn { Player, AI }

    [Header("Settings")]
    public Turn currentTurn = Turn.Player;
    public float turnTransitionTime = 10f;

    [Header("State")]
    private bool isProcessing = false; // Lock for animations/effects
    private bool isPlayerInteracting = false; // Legacy turn-timer lock
    private bool isBoardActionInProgress = false; // Player is resolving/attempting a board swap
    private bool isSkillActionInProgress = false; // Player is executing a skill/card action
    private bool gameEnded = false;
    private bool battleResultHandled = false;
    private bool isTutorialTimerPaused = false;
    private int turnIndex = 0;
    public int CurrentTurnIndex => turnIndex;

    [Header("References")]
    public PlayerStats player;
    public AIStats ai;
    public CardManager cardManager;
    public GuardianManager guardianManager;
    [SerializeField] private BattleResultPopupUI battleResultPopup;

    [Header("Battle Runtime Data")]
    public BattleRuntimeData battleData = new BattleRuntimeData();
    public MapEnemyData currentEnemy = new MapEnemyData();

    private Coroutine turnTimerCoroutine;
    private Coroutine startTurnCoroutine;

    private static readonly HashSet<string> SpecialGuardianRewardMapIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "149", "150", "151", "152", "153", "154", "155" };

    private struct WinRewardGrantResult
    {
        public int gold;
        public int exp;
        public int diamond;
        public List<BattleResultPopupUI.GemRewardViewData> gemRewards;
        public bool hasPetReward;
        public int petId;
        public int petLevel;
        public string petName;
        public bool hasGuardianReward;
        public int guardianId;
        public int guardianLevel;
        public string guardianName;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (player == null) player = FindObjectOfType<PlayerStats>();
        if (ai == null) ai = FindObjectOfType<AIStats>();
        if (cardManager == null) cardManager = FindObjectOfType<CardManager>();
        if (guardianManager == null) guardianManager = FindObjectOfType<GuardianManager>();
        if (battleResultPopup == null) battleResultPopup = FindObjectOfType<BattleResultPopupUI>(true);

        gameEnded = false;
        battleResultHandled = false;

        Debug.Log($"[GameManager] Setup | player={(player != null ? "OK" : "NULL")} | ai={(ai != null ? "OK" : "NULL")} | cardManager={(cardManager != null ? "OK" : "NULL")}");
    }

    private void Start()
    {
        if (player != null) player.Init();
        if (ai != null) ai.Init();

        if (guardianManager != null)
        {
            guardianManager.RefreshCurrentGuardianFromPlayer();
            guardianManager.ResetBattleState();
        }

        if (battleResultPopup != null)
            battleResultPopup.HideAll();

        StartTurn();
    }

    public void StartPlayerInteraction()
    {
        if (currentTurn != Turn.Player || isProcessing || gameEnded || isSkillActionInProgress)
            return;

        isPlayerInteracting = true;
        isBoardActionInProgress = true;
        StopTurnTimerInternal();

        if (UIManager.Instance != null)
            UIManager.Instance.CheckSkillRequirements();
    }

    public void OnCardActionStart()
    {
        if (currentTurn != Turn.Player || isProcessing || gameEnded || isBoardActionInProgress)
            return;

        isPlayerInteracting = true;
        isSkillActionInProgress = true;
        StopTurnTimerInternal();

        if (UIManager.Instance != null)
            UIManager.Instance.CheckSkillRequirements();
    }

    public void CancelPlayerBoardInteraction()
    {
        if (currentTurn != Turn.Player || gameEnded)
            return;

        isPlayerInteracting = false;
        isBoardActionInProgress = false;

        if (!isProcessing && !isTutorialTimerPaused)
        {
            if (turnTimerCoroutine != null)
                StopCoroutine(turnTimerCoroutine);

            turnTimerCoroutine = StartCoroutine(TurnTimer());
        }

        if (UIManager.Instance != null)
            UIManager.Instance.CheckSkillRequirements();
    }

    private void StartTurn()
    {
        if (gameEnded) return;

        if (startTurnCoroutine != null)
            StopCoroutine(startTurnCoroutine);

        startTurnCoroutine = StartCoroutine(StartTurnRoutine());
    }

    private IEnumerator StartTurnRoutine()
    {
        if (gameEnded)
            yield break;

        isProcessing = true;
        isPlayerInteracting = false;
        isBoardActionInProgress = false;
        isSkillActionInProgress = false;
        if (Board.Instance != null)
            Board.Instance.gameObject.SetActive(false);

        turnIndex++;

        bool skipTurn = false;
        if (currentTurn == Turn.Player)
        {
            if (player != null)
                player.ConsumeImmortalRoundOnOwnerTurnStart();
            if (ai != null)
                ai.ActivatePendingImmortal();
            if (guardianManager != null && turnIndex >= 3)
                yield return StartCoroutine(guardianManager.PlayPlayerGuardianEffectRoutine(player, ai, turnIndex == 3));

            if (player != null)
                player.TickStatusEffects();

            if (guardianManager != null)
                guardianManager.TickBurnVfxForTurn(true);

            skipTurn = player != null && player.IsStunnedThisTurn;
        }
        else
        {
            if (ai != null)
                ai.ConsumeImmortalRoundOnOwnerTurnStart();
            if (player != null)
                player.ActivatePendingImmortal();
            if (guardianManager != null && turnIndex >= 4)
                yield return StartCoroutine(guardianManager.PlayAiGuardianEffectRoutine(player, ai, turnIndex == 4));

            if (ai != null)
                ai.TickStatusEffects();

            if (guardianManager != null)
                guardianManager.TickBurnVfxForTurn(false);

            skipTurn = ai != null && ai.IsStunnedThisTurn;
        }

        if (skipTurn)
        {
            Debug.Log("[GameManager] Skip turn due to stun.");
            isProcessing = false;
            EndTurn(null);
            yield break;
        }

        Debug.Log($"[GameManager] StartTurn | currentTurn={currentTurn} | cardManager={(cardManager != null ? "OK" : "NULL")}");
        isProcessing = false;
        isPlayerInteracting = false;
        isBoardActionInProgress = false;
        isSkillActionInProgress = false;
        if (Board.Instance != null)
            Board.Instance.gameObject.SetActive(true);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnTurnChanged();
        }
        if (cardManager != null)
        {
            cardManager.RefreshCardInteractables();
        }
        else
        {
            Debug.LogWarning("[GameManager] StartTurn: cardManager is NULL, card UI will not refresh.");
        }
        if (!isTutorialTimerPaused)
        {
            if (turnTimerCoroutine != null) StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = StartCoroutine(TurnTimer());
        }
    }


    public void PauseTurnTimerForTutorial()
    {
        if (gameEnded)
            return;

        isTutorialTimerPaused = true;
        StopTurnTimerInternal();
    }

    public void ResumeTurnTimerAfterTutorial()
    {
        if (gameEnded)
            return;

        isTutorialTimerPaused = false;

        if (currentTurn == Turn.Player && !isProcessing && turnTimerCoroutine == null)
            turnTimerCoroutine = StartCoroutine(TurnTimer());
    }
    private void StopTurnTimerInternal()
    {
        if (turnTimerCoroutine != null)
        {
            StopCoroutine(turnTimerCoroutine);
            turnTimerCoroutine = null;
            if (UIManager.Instance != null) UIManager.Instance.StopTurnTimer();
        }
    }

    public void EndTurn(List<int> destroyedIds = null, List<GemEffectMatchEntry> effectEntries = null, bool switchTurn = true)
    {
        if (isProcessing || gameEnded) return;

        Turn actingTurn = currentTurn;
        StopTurnTimerInternal();
        StartCoroutine(EndTurnRoutine(destroyedIds, effectEntries, actingTurn, switchTurn));
    }

    private IEnumerator EndTurnRoutine(List<int> destroyedIds, List<GemEffectMatchEntry> effectEntries, Turn actingTurn, bool switchTurn)
    {
        isProcessing = true;
        isPlayerInteracting = false;
        isBoardActionInProgress = false;
        isSkillActionInProgress = false;
        if (cardManager != null)
            cardManager.RefreshCardInteractables();

        if (destroyedIds != null && destroyedIds.Count > 0)
        {
            if (Board.Instance != null) Board.Instance.gameObject.SetActive(false);
            Dictionary<int, List<GemEffectMatchEntry>> buckets = BuildGemEffectBuckets(destroyedIds, effectEntries);

            if (UIManager.Instance != null)
            {
                yield return StartCoroutine(UIManager.Instance.PlayDestroyedItemsSequence(
                    destroyedIds,
                    (gemType, count) => ApplyEffectsForGemStep(actingTurn, gemType, count, buckets)));
            }
            else
            {
                List<int> orderedGemTypes = new List<int>(buckets.Keys);
                orderedGemTypes.Sort();
                for (int i = 0; i < orderedGemTypes.Count; i++)
                {
                    int gemType = orderedGemTypes[i];
                    yield return StartCoroutine(ApplyEffectsForGemStep(actingTurn, gemType, 0, buckets));
                }
            }

            if (Board.Instance != null) Board.Instance.gameObject.SetActive(true);
        }

        if (CheckGameEnd())
        {
            isProcessing = false;
            yield break;
        }

        TickShieldDurations();

        if (switchTurn)
        {
            currentTurn = (actingTurn == Turn.Player) ? Turn.AI : Turn.Player;

            Debug.Log($"[GameManager] Turn switched | actingTurn={actingTurn} -> currentTurn={currentTurn}");

            if (currentTurn == Turn.Player && cardManager != null)
            {
                Debug.Log("[GameManager] Refreshing card interactables for player turn after switch");
                cardManager.RefreshCardInteractables();
            }
        }

        isProcessing = false;
        StartTurn();
    }

    private Dictionary<int, List<GemEffectMatchEntry>> BuildGemEffectBuckets(List<int> destroyedIds, List<GemEffectMatchEntry> effectEntries)
    {
        Dictionary<int, List<GemEffectMatchEntry>> buckets = new Dictionary<int, List<GemEffectMatchEntry>>();

        if (effectEntries != null && effectEntries.Count > 0)
        {
            for (int i = 0; i < effectEntries.Count; i++)
            {
                GemEffectMatchEntry entry = effectEntries[i];
                if (!buckets.TryGetValue(entry.GemType, out List<GemEffectMatchEntry> list))
                {
                    list = new List<GemEffectMatchEntry>();
                    buckets[entry.GemType] = list;
                }

                list.Add(entry);
            }

            foreach (var kv in buckets)
                kv.Value.Sort((a, b) => a.ComboIndex.CompareTo(b.ComboIndex));

            return buckets;
        }

        Dictionary<int, int> counts = new Dictionary<int, int>();
        if (destroyedIds != null)
        {
            for (int i = 0; i < destroyedIds.Count; i++)
            {
                int id = destroyedIds[i];
                if (counts.ContainsKey(id)) counts[id]++;
                else counts[id] = 1;
            }
        }

        foreach (var kv in counts)
        {
            buckets[kv.Key] = new List<GemEffectMatchEntry>
            {
                new GemEffectMatchEntry(kv.Key, Mathf.Max(3, kv.Value), 1)
            };
        }

        return buckets;
    }

    private IEnumerator ApplyEffectsForGemStep(
        Turn actingTurn,
        int gemType,
        int fallbackCount,
        Dictionary<int, List<GemEffectMatchEntry>> buckets)
    {
        if (buckets == null)
            yield break;

        if (!buckets.TryGetValue(gemType, out List<GemEffectMatchEntry> entries) || entries == null || entries.Count == 0)
        {
            if (fallbackCount <= 0)
                yield break;

            entries = new List<GemEffectMatchEntry>
            {
                new GemEffectMatchEntry(gemType, Mathf.Max(3, fallbackCount), 1)
            };
        }

        bool attackTriggered = false;

        for (int i = 0; i < entries.Count; i++)
        {
            GemEffectMatchEntry entry = entries[i];

            if (entry.GemType == GemEffectProcessor.AttackItemId)
            {
                if (attackTriggered)
                {
                    Debug.Log("[GameManager] Skip duplicated attack trigger for same gem step.");
                    continue;
                }

                attackTriggered = true;
            }

            if (actingTurn == Turn.Player && player != null && ai != null)
            {
                GemEffectProcessor.ProcessForPlayer(entry, player, ai);
                if (entry.GemType == GemEffectProcessor.AttackItemId)
                    yield return new WaitUntil(() => !player.isAttacking);
            }
            else if (actingTurn == Turn.AI && ai != null && player != null)
            {
                GemEffectProcessor.ProcessForAI(entry, ai, player);
                if (entry.GemType == GemEffectProcessor.AttackItemId)
                    yield return new WaitUntil(() => !ai.isAttacking);
            }
        }
    }

    private void TickShieldDurations()
    {
        if (player != null)
            player.TickShieldDuration();

        if (ai != null)
            ai.TickShieldDuration();
    }

    public void SetBattleData(int playerPetId, int playerLevel, int enemyPetId, int enemyLevel)
    {
        SetBattleData(playerPetId, playerLevel, enemyPetId, enemyLevel, PrebattleSelectionData.EnemyGuardianId, PrebattleSelectionData.EnemyGuardianLevel);
    }

    public void SetBattleData(int playerPetId, int playerLevel, int enemyPetId, int enemyLevel, int enemyGuardianId, int enemyGuardianLevel)
    {
        battleData.playerPetId = playerPetId;
        battleData.playerLevel = Mathf.Max(1, playerLevel);
        battleData.enemyPetId = enemyPetId;
        battleData.enemyLevel = Mathf.Max(1, enemyLevel);

        currentEnemy.petId = enemyPetId;
        currentEnemy.level = Mathf.Max(1, enemyLevel);
        currentEnemy.guardianId = Mathf.Max(0, enemyGuardianId);
        currentEnemy.guardianLevel = Mathf.Max(1, enemyGuardianLevel);
    }

    private IEnumerator TurnTimer()
    {
        yield return null;

        if (currentTurn == Turn.AI && !gameEnded)
        {
            StartCoroutine(AIMove());
        }

        Turn turnAtStart = currentTurn;

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowTurnTimer(turnTransitionTime, currentTurn));
        }
        else
        {
            yield return new WaitForSeconds(turnTransitionTime);
        }

        if (!isProcessing && !gameEnded && !isPlayerInteracting && currentTurn == turnAtStart)
        {
            EndTurn(null);
        }
    }

    private IEnumerator AIMove()
    {
        if (gameEnded) yield break;

        yield return new WaitForSeconds(1f);
        AIController aiController = FindObjectOfType<AIController>();

        if (aiController != null)
        {
            yield return StartCoroutine(aiController.MakeMove());
        }
        else
        {
            EndTurn(null);
        }
    }

    public void ResumePlayerTurnAfterInvalidSwap()
    {
        if (currentTurn != Turn.Player || isProcessing || gameEnded)
            return;

        isPlayerInteracting = false;
        isBoardActionInProgress = false;
        if (turnTimerCoroutine != null)
            StopCoroutine(turnTimerCoroutine);

        if (!isTutorialTimerPaused)
            turnTimerCoroutine = StartCoroutine(TurnTimer());

        if (UIManager.Instance != null)
            UIManager.Instance.CheckSkillRequirements();
    }

    public bool CanPlayerMove()
    {
        return CanPlayerBoardInteract();
    }

    public bool CanPlayerBoardInteract()
    {
        return currentTurn == Turn.Player && !isProcessing && !gameEnded && !isSkillActionInProgress;
    }

    public bool CanPlayerUseSkill()
    {
        return currentTurn == Turn.Player && !isProcessing && !gameEnded && !isBoardActionInProgress && !isSkillActionInProgress;
    }

    public bool ProcessImmediateBattleResultIfNeeded()
    {
        return CheckGameEnd();
    }

    private bool CheckGameEnd()
    {
        if (player != null && player.HP <= 0)
        {
            gameEnded = true;
            HandleBattleResult(false);
            return true;
        }

        if (ai != null && ai.Health <= 0)
        {
            gameEnded = true;
            HandleBattleResult(true);
            return true;
        }

        return false;
    }

    private void HandleBattleResult(bool playerWon)
    {
        if (battleResultHandled) return;
        battleResultHandled = true;

        if (guardianManager != null)
            guardianManager.DeactivateGuardian();

        StopTurnTimerInternal();
        if (cardManager != null)
            cardManager.RefreshCardInteractables();

        string mapId = PrebattleSelectionData.MapId;
        SteamManager.Instance.ReportBattleCompleted();

        if (playerWon)
        {
            WinRewardGrantResult rewardResult = ApplyWinRewards(mapId);
            ShowWinPopup(rewardResult);
            return;
        }

        ShowFailPopup();
    }

    private void ShowFailPopup()
    {
        if (battleResultPopup == null)
            battleResultPopup = FindObjectOfType<BattleResultPopupUI>(true);

        if (battleResultPopup == null)
        {
            ReloadBattleScene();
            return;
        }

        battleResultPopup.ShowFail(ReloadBattleScene, LoadHomeScene);
    }

    private void ShowWinPopup(WinRewardGrantResult rewardResult)
    {
        if (battleResultPopup == null)
            battleResultPopup = FindObjectOfType<BattleResultPopupUI>(true);

        if (battleResultPopup == null)
        {
            LoadHomeScene();
            return;
        }

        BattleResultPopupUI.WinResultViewData viewData = new BattleResultPopupUI.WinResultViewData
        {
            gold = rewardResult.gold,
            exp = rewardResult.exp,
            diamond = rewardResult.diamond,
            gemRewards = rewardResult.gemRewards ?? new List<BattleResultPopupUI.GemRewardViewData>(),
            hasPetReward = rewardResult.hasPetReward,
            petId = rewardResult.petId,
            petLevel = rewardResult.petLevel,
            petName = rewardResult.petName,
            hasGuardianReward = rewardResult.hasGuardianReward,
            guardianId = rewardResult.guardianId,
            guardianLevel = rewardResult.guardianLevel,
            guardianName = rewardResult.guardianName
        };

        Debug.Log($"[GameManager] ShowWinPopup: gemRewards count = {viewData.gemRewards.Count}");
        for (int i = 0; i < viewData.gemRewards.Count; i++)
        {
            var gem = viewData.gemRewards[i];
            Debug.Log($"[GameManager] GemReward {i}: name='{gem.displayName}', amount={gem.amount}, sprite={gem.gemSprite?.name ?? "NULL"}");
        }

        battleResultPopup.ShowWin(viewData, LoadHomeScene);
    }
    private WinRewardGrantResult ApplyWinRewards(string mapId)
    {
        WinRewardGrantResult result = new WinRewardGrantResult
        {
            gemRewards = new List<BattleResultPopupUI.GemRewardViewData>(),
            petId = -1,
            petLevel = 1,
            petName = string.Empty,
            guardianId = -1,
            guardianLevel = 1,
            guardianName = string.Empty
        };

        if (PlayerManager.Instance == null)
            return result;

        if (!string.IsNullOrWhiteSpace(mapId))
            PlayerManager.Instance.AddMapWin(mapId, 1);

        if (GameDataManager.Instance == null || string.IsNullOrWhiteSpace(mapId))
        {
            PlayerManager.Instance.SaveData();
            return result;
        }

        MapDataAsset map = GameDataManager.Instance.GetMapData(mapId);
        if (map == null)
        {
            PlayerManager.Instance.SaveData();
            return result;
        }

        ApplyMapRewards(map, ref result);

        bool isSpecialGuardianMap = SpecialGuardianRewardMapIds.Contains(map.mapId);
        if (isSpecialGuardianMap)
        {
            TryClaimWinGuardianReward(map, ref result);
        }
        else
        {
            TryClaimWinPetReward(map, ref result);
            TryClaimWinGuardianReward(map, ref result);
        }

        PlayerManager.Instance.SaveData();
        return result;
    }

    private void ApplyMapRewards(MapDataAsset map, ref WinRewardGrantResult result)
    {
        if (map == null || map.rewards == null || PlayerManager.Instance == null)
            return;

        for (int i = 0; i < map.rewards.Count; i++)
        {
            MapRewardData reward = map.rewards[i];
            if (reward == null)
                continue;

            int amount = ResolveRewardAmount(reward);
            if (amount <= 0)
                continue;

            switch (reward.rewardType)
            {
                case MapRewardType.GOLD:
                    PlayerManager.Instance.AddGold(amount);
                    result.gold += amount;
                    break;

                case MapRewardType.EXP:
                    PlayerManager.Instance.AddExp(amount);
                    result.exp += amount;
                    break;

                case MapRewardType.DIAMOND:
                    PlayerManager.Instance.AddDiamond(amount);
                    result.diamond += amount;
                    break;

                case MapRewardType.GEM:
                    int elementId = reward.gemElementId;
                    int gemLevel = Mathf.Clamp(reward.gemLevel, 1, 5);
                    Debug.Log($"[GameManager] Processing GEM reward: elementId={elementId}, gemLevel={gemLevel}");
                    
                    if (elementId < 0)
                    {
                        Debug.LogError($"[GameManager] Invalid elementId {elementId}, skipping gem reward");
                        break;
                    }

                    PlayerManager.Instance.AddOrUpdateOwnedGem(elementId, gemLevel, amount);
                    Debug.Log($"[GameManager] Added gem to player inventory: element {elementId} Lv{gemLevel} x{amount}");
                    
                    Sprite gemSprite = GetGemSprite(elementId, gemLevel);
                    Debug.Log($"[GameManager] GetGemSprite returned: {gemSprite?.name ?? "NULL"}");
                    
                    result.gemRewards.Add(new BattleResultPopupUI.GemRewardViewData
                    {
                        displayName = BuildGemDisplayName(elementId, gemLevel),
                        amount = amount,
                        gemSprite = gemSprite
                    });
                    Debug.Log($"[GameManager] Added to gemRewards list: {BuildGemDisplayName(elementId, gemLevel)} x{amount}");
                    break;
            }
        }
    }

    private void TryClaimWinPetReward(MapDataAsset map, ref WinRewardGrantResult result)
    {
        if (map == null || PlayerManager.Instance == null)
            return;

        int currentWins = PlayerManager.Instance.GetMapWinCount(map.mapId);
        bool canReceivePet = currentWins >= Mathf.Max(0, map.reqWinsPet) && !PlayerManager.Instance.HasClaimedMapPetReward(map.mapId);
        if (!canReceivePet)
            return;

        int rewardPetId = map.rewardPetId >= 0 ? map.rewardPetId : map.petIdSpawn;
        if (rewardPetId < 0)
            return;

        string petName = rewardPetId.ToString();
        if (GameDataManager.Instance != null &&
            GameDataManager.Instance.TryGetPetStatSnapshot(rewardPetId, 1, out GameDataManager.PetStatSnapshot snapshot) &&
            !string.IsNullOrWhiteSpace(snapshot.petName))
        {
            petName = snapshot.petName;
        }

        int petLevel = 1;
        bool claimed = PlayerManager.Instance.TryClaimMapPetReward(map.mapId, rewardPetId, petName, petLevel);
        if (!claimed)
            return;

        result.hasPetReward = true;
        result.petId = rewardPetId;
        result.petLevel = petLevel;
        result.petName = petName;

        PlayerManager.Instance.QueueDemoCtaPopup(map.mapId, rewardPetId);
    }

    private void TryClaimWinGuardianReward(MapDataAsset map, ref WinRewardGrantResult result)
    {
        if (map == null || PlayerManager.Instance == null)
            return;

        if (!SpecialGuardianRewardMapIds.Contains(map.mapId))
            return;

        int guardianId = map.rewardGuardiantId >= 0 ? map.rewardGuardiantId : map.idGuadiant;
        if (guardianId < 0)
            return;

        int currentWins = PlayerManager.Instance.GetMapWinCount(map.mapId);
        if (currentWins < 1)
            return;

        int guardianLevel = Mathf.Max(1, map.levelGuadiant);
        bool claimed = PlayerManager.Instance.TryClaimMapGuardianReward(map.mapId, guardianId, guardianLevel);
        if (!claimed)
            return;

        result.hasGuardianReward = true;
        result.guardianId = guardianId;
        result.guardianLevel = guardianLevel;
        result.guardianName = guardianId.ToString();

        if (GameDataManager.Instance != null && GameDataManager.Instance.GuardianDatabase != null)
        {
            GuardianDataAsset guardianData = GameDataManager.Instance.GuardianDatabase.GetGuardianById(guardianId);
            if (guardianData != null && !string.IsNullOrWhiteSpace(guardianData.guardianName))
                result.guardianName = guardianData.guardianName;
        }
    }
    private static int ResolveRewardAmount(MapRewardData reward)
    {
        if (reward == null)
            return 0;

        int dropChancePercent = Mathf.Clamp(reward.weight, 0, 100);
        if (dropChancePercent <= 0)
            return 0;

        float roll = UnityEngine.Random.Range(0f, 100f);
        if (roll >= dropChancePercent)
            return 0;

        int min = Mathf.Max(0, reward.amountMin);
        int max = Mathf.Max(min, reward.amountMax);

        if (max <= 0)
            return 0;

        return UnityEngine.Random.Range(min, max + 1);
    }

    private static string BuildGemDisplayName(int elementId, int gemLevel)
    {
        // Get element name t? GemCollection
        string elementName = GetGemElementName(elementId);
        if (string.IsNullOrEmpty(elementName))
            elementName = "Gem";

        // Localize element name v� level text
        string localizedElement = LocalizationManager.Instance != null 
            ? LocalizationManager.Instance.GetText(elementName, elementName)
            : elementName;
        
        string levelText = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText("level", "Level")
            : "Level";
        
        return $"{localizedElement} {levelText}{gemLevel}";
    }

    private static string GetGemElementName(int elementId)
    {
        GemCollection gemCollection = Resources.Load<GemCollection>("GemCollection");
        if (gemCollection == null || gemCollection.elements == null)
            return "";

        if (elementId >= 0 && elementId < gemCollection.elements.Length && gemCollection.elements[elementId] != null)
            return gemCollection.elements[elementId].element;

        return "";
    }

    private static Sprite GetGemSprite(int elementId, int gemLevel)
    {
        Debug.Log($"[GameManager] GetGemSprite called with elementId={elementId}, gemLevel={gemLevel}");

        // Get GemCollection from GameDataManager
        UnityEngine.Object gemCollectionObj = GameDataManager.Instance?.GemCollectionObject;
        if (gemCollectionObj == null)
        {
            Debug.LogError("[GameManager] GameDataManager.GemCollectionObject is NULL");
            return null;
        }

        GemCollection gemCollection = gemCollectionObj as GemCollection;
        if (gemCollection == null)
        {
            Debug.LogError($"[GameManager] Failed to cast GemCollectionObject to GemCollection (type: {gemCollectionObj.GetType().Name})");
            return null;
        }

        Debug.Log("[GameManager] ? Loaded GemCollection from GameDataManager");

        if (gemCollection.elements == null)
        {
            Debug.LogError("[GameManager] GemCollection.elements is NULL");
            return null;
        }

        Debug.Log($"[GameManager] GemCollection has {gemCollection.elements.Length} elements");

        if (elementId < 0 || elementId >= gemCollection.elements.Length)
        {
            Debug.LogError($"[GameManager] Element ID {elementId} out of range (0-{gemCollection.elements.Length - 1})");
            return null;
        }

        GemCollection.GemElementData elementData = gemCollection.elements[elementId];
        if (elementData == null)
        {
            Debug.LogError($"[GameManager] Element {elementId} data is NULL");
            return null;
        }

        Debug.Log($"[GameManager] Element {elementId}: {elementData.element}");

        if (elementData.gemLevels == null)
        {
            Debug.LogError($"[GameManager] Element {elementId} ({elementData.element}) - gemLevels is NULL");
            return null;
        }

        if (elementData.gemLevels.Length == 0)
        {
            Debug.LogError($"[GameManager] Element {elementId} ({elementData.element}) - gemLevels is empty");
            return null;
        }

        Debug.Log($"[GameManager] Element {elementData.element} has {elementData.gemLevels.Length} levels");

        int levelIndex = Mathf.Clamp(gemLevel - 1, 0, elementData.gemLevels.Length - 1);
        Debug.Log($"[GameManager] Level {gemLevel} ? levelIndex {levelIndex}");

        GemCollection.GemLevelData levelData = elementData.gemLevels[levelIndex];
        
        if (levelData == null)
        {
            Debug.LogError($"[GameManager] Level data for {elementData.element} Lv{gemLevel} (index {levelIndex}) is NULL");
            return null;
        }

        if (levelData.sprite == null)
        {
            Debug.LogError($"[GameManager] Sprite for {elementData.element} Lv{gemLevel} is NULL");
            return null;
        }
        
        Debug.Log($"[GameManager] ? Successfully loaded sprite: {elementData.element} Lv{gemLevel} = {levelData.sprite.name}");
        return levelData.sprite;
    }

    private void ReloadBattleScene()
    {
        // Clean up old battle pets before reloading
        CleanupBattlePets();
        
        if (battleResultPopup != null)
            battleResultPopup.HideAll();

        GameSceneManager gsm = FindObjectOfType<GameSceneManager>();
        if (gsm != null)
        {
            gsm.LoadBattle();
            return;
        }

        SceneManager.LoadScene("SceneBattle", LoadSceneMode.Single);
    }

    private void LoadHomeScene()
    {
        // Clean up battle pets before loading home scene
        CleanupBattlePets();
        
        PrebattleSelectionData.Clear();

        if (battleResultPopup != null)
            battleResultPopup.HideAll();

        GameSceneManager gsm = FindObjectOfType<GameSceneManager>();
        if (gsm != null)
        {
            gsm.LoadHome();
            return;
        }

        SceneManager.LoadScene("SceneHome", LoadSceneMode.Single);
    }

    private void CleanupBattlePets()
    {
        // Destroy player pet
        if (player != null)
        {
            if (player.player != null)
            {
                Destroy(player.player);
                player.player = null;
            }
        }

        // Destroy AI pet
        if (ai != null)
        {
            if (ai.AI != null)
            {
                Destroy(ai.AI);
                ai.AI = null;
            }
        }

        // Cleanup extra audio listeners to prevent warnings (disable extras only)
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            AudioListener preferred = null;
            Camera mainCam = Camera.main;
            if (mainCam != null)
                preferred = mainCam.GetComponent<AudioListener>();

            if (preferred == null)
                preferred = listeners[0];

            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener == null || listener == preferred)
                    continue;

                listener.enabled = false;
            }
        }
    }
}








