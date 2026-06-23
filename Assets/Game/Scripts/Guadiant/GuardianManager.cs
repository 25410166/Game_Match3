using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GuardianManager : MonoBehaviour
{
    [SerializeField] private GuardianDatabase guardianDatabase;
    [SerializeField] private GuardianDataAsset currentGuardian;
    [SerializeField] private int currentGuardianLevel = 1;
    [SerializeField] private GuardianDataAsset currentAiGuardian;
    [SerializeField] private int currentAiGuardianLevel = 1;
    [SerializeField] private Transform guardianSpawnRoot;
    [SerializeField] private Transform aiGuardianSpawnRoot;
    [SerializeField] private Vector3 guardianSpawnScale = Vector3.one;
    [SerializeField] private Vector3 aiGuardianSpawnScale = Vector3.one;
    [SerializeField] private string idleAnimationName = "Idle";
    [SerializeField] private string attackTriggerName = "Attack";
    [SerializeField] private float guardianAnimationDelay = 2f;
    [SerializeField] private float guardianVfxDuration = 2f;

    [Header("Coin Flip (Guardian 6)")]
    [SerializeField] private GameObject coinFlipPrefab;
    [SerializeField] private Transform coinFlipRoot;
    [SerializeField] private Vector3 coinFlipScale = Vector3.one;
    [SerializeField] private Sprite coinHeadsSprite;
    [SerializeField] private Sprite coinTailsSprite;
    [SerializeField] private float coinFlipDuration = 2f;
    [SerializeField] private float coinSpinSpeed = 720f;
    [SerializeField] private float coinResultHoldTime = 0.2f;

    private GameObject spawnedGuardian;
    private GameObject spawnedAiGuardian;
    private bool battleStartApplied;
    private bool aiBattleStartApplied;
    private GameObject burnVfxInstance;
    private Transform burnVfxTarget;
    private bool burnTargetIsPlayer;
    private int burnTurnsRemaining;
    private GameObject stunVfxInstance;
    private Transform stunVfxTarget;

    public GuardianDataAsset CurrentGuardian => currentGuardian;
    public int CurrentGuardianLevel => Mathf.Max(1, currentGuardianLevel);
    public GuardianDataAsset CurrentAiGuardian => currentAiGuardian;
    public int CurrentAiGuardianLevel => Mathf.Max(1, currentAiGuardianLevel);

    public void RefreshCurrentGuardianFromPlayer()
    {
        if (guardianDatabase == null && GameDataManager.Instance != null)
            guardianDatabase = GameDataManager.Instance.GuardianDatabase;

        int guardianId = ResolveSelectedGuardianId();
        int guardianLevel = ResolveSelectedGuardianLevel(guardianId);
        RefreshCurrentGuardianFromMapAi();

        if (guardianId < 0 || guardianDatabase == null)
        {
            currentGuardian = null;
            currentGuardianLevel = Mathf.Max(1, guardianLevel);
            ClearGuardianPreview();
            ResetBattleState();
            return;
        }

        currentGuardian = guardianDatabase.GetGuardianById(guardianId);
        currentGuardianLevel = Mathf.Max(1, guardianLevel);
        SpawnGuardianPreview(currentGuardian != null ? currentGuardian.guardianPrefab : null);
        ResetBattleState();
    }

    private void RefreshCurrentGuardianFromMapAi()
    {
        if (guardianDatabase == null && GameDataManager.Instance != null)
            guardianDatabase = GameDataManager.Instance.GuardianDatabase;

        int guardianId = 0;
        int guardianLevel = 1;

        if (GameManager.Instance != null && GameManager.Instance.currentEnemy != null)
        {
            guardianId = GameManager.Instance.currentEnemy.guardianId;
            guardianLevel = GameManager.Instance.currentEnemy.guardianLevel;
        }

        if (guardianId <= 0 || guardianDatabase == null)
        {
            currentAiGuardian = null;
            currentAiGuardianLevel = Mathf.Max(1, guardianLevel);
            ClearAiGuardianPreview();
            return;
        }

        currentAiGuardian = guardianDatabase.GetGuardianById(guardianId);
        currentAiGuardianLevel = Mathf.Max(1, guardianLevel);
        SpawnAiGuardianPreview(currentAiGuardian != null ? currentAiGuardian.guardianPrefab : null);
    }

    private int ResolveSelectedGuardianId()
    {
        if (!string.IsNullOrWhiteSpace(PrebattleSelectionData.MapId) && PrebattleSelectionData.GuardianId >= 0)
            return PrebattleSelectionData.GuardianId;

        return PlayerManager.Instance != null ? PlayerManager.Instance.GetEquippedGuardianId() : -1;
    }

    private int ResolveSelectedGuardianLevel(int guardianId)
    {
        if (guardianId < 0)
            return 1;

        if (!string.IsNullOrWhiteSpace(PrebattleSelectionData.MapId)
            && PrebattleSelectionData.GuardianId == guardianId
            && PrebattleSelectionData.GuardianLevel > 0)
            return Mathf.Max(1, PrebattleSelectionData.GuardianLevel);

        return PlayerManager.Instance != null ? PlayerManager.Instance.GetGuardianLevel(guardianId) : 1;
    }

    public void OnBattleStart(PlayerStats player, AIStats enemy)
    {
        ApplyGuardianEffect(player, enemy, true);
    }

    public void OnPlayerTurnStart(PlayerStats player, AIStats enemy)
    {
        ApplyGuardianEffect(player, enemy, false);
    }

    public void OnAiTurnStart(PlayerStats player, AIStats enemy, bool isBattleStart)
    {
        ApplyAiGuardianEffect(currentAiGuardian, currentAiGuardian != null ? currentAiGuardian.GetLevelData(CurrentAiGuardianLevel) : null, player, enemy, isBattleStart, out _);
    }

    public IEnumerator PlayGuardianEffectRoutine(PlayerStats player, AIStats enemy, bool isBattleStart)
    {
        yield return StartCoroutine(PlayPlayerGuardianEffectRoutine(player, enemy, isBattleStart));
    }

    public IEnumerator PlayPlayerGuardianEffectRoutine(PlayerStats player, AIStats enemy, bool isBattleStart)
    {
        yield return StartCoroutine(PlayGuardianEffectRoutine(player, enemy, isBattleStart, false));
    }

    public IEnumerator PlayAiGuardianEffectRoutine(PlayerStats player, AIStats enemy, bool isBattleStart)
    {
        yield return StartCoroutine(PlayGuardianEffectRoutine(player, enemy, isBattleStart, true));
    }

    private IEnumerator PlayGuardianEffectRoutine(PlayerStats player, AIStats enemy, bool isBattleStart, bool isAiGuardian)
    {
        GuardianDataAsset activeGuardian = isAiGuardian ? currentAiGuardian : currentGuardian;
        int activeGuardianLevel = isAiGuardian ? CurrentAiGuardianLevel : CurrentGuardianLevel;

        if (activeGuardian == null)
            yield break;

        bool activeCanApplyBattleStart = isBattleStart && activeGuardian.applyOnBattleStart && !(isAiGuardian ? aiBattleStartApplied : battleStartApplied);
        bool activeCanApplyPlayerTurn = activeGuardian.applyOnPlayerTurn;

        if (!activeCanApplyBattleStart && !activeCanApplyPlayerTurn)
            yield break;

        GuardianLevelData activeLevelData = activeGuardian.GetLevelData(activeGuardianLevel);
        if (activeLevelData == null)
            yield break;

        if (isAiGuardian)
        {
            bool aiTargetIsPlayer;
            bool aiApplied = ApplyAiGuardianEffect(activeGuardian, activeLevelData, player, enemy, isBattleStart, out aiTargetIsPlayer);
            if (activeCanApplyBattleStart)
                aiBattleStartApplied = true;

            if (aiApplied)
            {
                PlayAiAttackAnimation();
                Transform aiTarget = ResolveTargetHitPoint(player, enemy, aiTargetIsPlayer);
                GameObject aiVfx = SpawnGuardianVfx(activeGuardian, aiTarget);
                yield return new WaitForSeconds(guardianVfxDuration);
                if (aiVfx != null)
                    Destroy(aiVfx);
            }

            yield break;
        }

        if (currentGuardian == null)
            yield break;

        bool canApplyBattleStart = activeCanApplyBattleStart;
        bool canApplyPlayerTurn = activeCanApplyPlayerTurn;
        GuardianLevelData levelData = activeLevelData;


        int guardianId = currentGuardian.guardianId;
        bool applied = false;
        bool missed = false;
        bool targetIsPlayer = true;
        bool spawnBurn = false;
        bool spawnStun = false;
        int burnTurns = 0;
        float burnPercent = 0f;
        int damageAmount = 0;
        bool showDamagePopup = false;
        System.Action applyEffect = null;

        if (guardianId == 6)
        {
            bool heads = UnityEngine.Random.value > 0.5f;
            Debug.Log($"[GuardianCoin] Flip result={(heads ? "Heads" : "Tails")} | value1={levelData.value1} | value2={levelData.value2}");
            PlayAttackAnimation();
            yield return StartCoroutine(PlayCoinFlipRoutine(heads));

            applied = BuildCoinEffect(heads, player, enemy, levelData, out targetIsPlayer, out applyEffect);
            Debug.Log($"[GuardianCoin] BuildCoinEffect applied={applied} targetIsPlayer={targetIsPlayer} applyEffect={(applyEffect != null)}");
            if (!applied)
                yield break;

            if (canApplyBattleStart)
                battleStartApplied = true;

            if (applyEffect != null)
            {
                applyEffect.Invoke();
                Debug.Log("[GuardianCoin] Apply effect invoked");
            }

            Transform coinTarget = ResolveTargetHitPoint(player, enemy, targetIsPlayer);
            GameObject coinVfx = SpawnVfx(currentGuardian.vfxPrefab, coinTarget);
            yield return new WaitForSeconds(guardianVfxDuration);
            if (coinVfx != null)
                Destroy(coinVfx);

            yield break;
        }

        if (guardianId == 3)
        {
            targetIsPlayer = false;
            if (player != null && enemy != null)
            {
                float atkMultiplier = Mathf.Max(0f, levelData.value1);
                damageAmount = Mathf.CeilToInt(player.baseAttack * atkMultiplier);
                if (damageAmount > 0)
                {
                    applied = true;
                    showDamagePopup = true;
                    applyEffect = () => enemy.TakeDamage(damageAmount);
                }
            }
        }
        else
        {
            switch (currentGuardian.element)
            {
                case GuardianElement.Leaf:
                {
                    targetIsPlayer = true;
                    int healAmount = player != null
                        ? Mathf.CeilToInt(player.maxHP * Mathf.Max(0f, levelData.value1) * 0.01f)
                        : 0;
                    int cleanseCount = Mathf.Max(0, Mathf.RoundToInt(levelData.value2));
                    if (player != null && (healAmount > 0 || cleanseCount > 0))
                    {
                        applied = true;
                        applyEffect = () =>
                        {
                            if (healAmount > 0)
                                player.Heal(healAmount);
                            if (cleanseCount > 0)
                                player.CleanseDebuff(cleanseCount);
                        };
                    }
                    break;
                }
                case GuardianElement.Fire:
                {
                    targetIsPlayer = false;
                    if (enemy != null)
                    {
                        float chance = Mathf.Clamp(levelData.value1 * 0.01f, 0f, 1f);
                        if (UnityEngine.Random.value > chance)
                        {
                            missed = true;
                        }
                        else
                        {
                            burnTurns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
                            burnPercent = Mathf.Max(0f, levelData.value3);
                            if (burnTurns > 0 && burnPercent > 0f)
                            {
                                applied = true;
                                spawnBurn = true;
                                applyEffect = () => enemy.ApplyStatusEffect(StatusEffectType.Burn, burnTurns, burnPercent);
                            }
                        }
                    }
                    break;
                }
                case GuardianElement.Metal:
                {
                    targetIsPlayer = true;
                    if (player != null)
                    {
                        int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
                        float bonusPercent = Mathf.Max(0f, levelData.value1);
                        if (bonusPercent > 0f)
                        {
                            applied = true;
                            applyEffect = () => player.ApplyStatusEffect(StatusEffectType.DamageBoost, turns, bonusPercent);
                        }
                    }
                    break;
                }
                case GuardianElement.Earth:
                {
                    targetIsPlayer = false;
                    if (enemy != null)
                    {
                        float chance = Mathf.Clamp(levelData.value1 * 0.01f, 0f, 1f);
                        if (UnityEngine.Random.value > chance)
                        {
                            missed = true;
                        }
                        else
                        {
                            int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
                            if (turns > 0)
                            {
                                applied = true;
                                spawnStun = true;
                                applyEffect = () => enemy.ApplyStatusEffect(StatusEffectType.Stun, turns, 0f);
                            }
                        }
                    }
                    break;
                }
                case GuardianElement.Water:
                {
                    targetIsPlayer = true;
                    if (player != null)
                    {
                        int manaGain = Mathf.Max(0, Mathf.RoundToInt(levelData.value1));
                        int rageGain = Mathf.Max(0, Mathf.RoundToInt(levelData.value2));
                        if (manaGain > 0 || rageGain > 0)
                        {
                            applied = true;
                            applyEffect = () =>
                            {
                                if (manaGain > 0)
                                    player.GainMana(manaGain);
                                if (rageGain > 0)
                                    player.GainRage(rageGain);
                            };
                        }
                    }
                    break;
                }
                case GuardianElement.Dark:
                {
                    bool heads = UnityEngine.Random.value > 0.5f;
                    if (heads)
                    {
                        targetIsPlayer = true;
                        float bonusPercent = Mathf.Max(0f, levelData.value1);
                        if (player != null && bonusPercent > 0f)
                        {
                            applied = true;
                            applyEffect = () => player.ApplyStatusEffect(StatusEffectType.DamageBoost, 1, bonusPercent);
                        }
                    }
                    else
                    {
                        targetIsPlayer = false;
                        float takenPercent = Mathf.Max(0f, levelData.value2);
                        if (enemy != null && takenPercent > 0f)
                        {
                            applied = true;
                            applyEffect = () => enemy.ApplyStatusEffect(StatusEffectType.DamageTakenIncrease, 1, takenPercent);
                        }
                    }
                    break;
                }
                case GuardianElement.Light:
                {
                    targetIsPlayer = true;
                    if (player != null)
                    {
                        int shieldAmount = Mathf.CeilToInt(player.maxHP * Mathf.Max(0f, levelData.value1) * 0.01f);
                        int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
                        if (shieldAmount > 0)
                        {
                            applied = true;
                            applyEffect = () => player.AddShield(shieldAmount, turns);
                        }
                    }
                    break;
                }
            }
        }

        Transform target = ResolveTargetHitPoint(player, enemy, targetIsPlayer);
        GameObject damagePopupPrefab = targetIsPlayer ? player?.DamagePopupPrefab : enemy?.DamagePopupPrefab;

        if (missed)
        {
            if (guardianId == 2 || guardianId == 4)
                ShowMissPopup(damagePopupPrefab, target);
            yield break;
        }

        if (!applied)
            yield break;

        if (canApplyBattleStart)
            battleStartApplied = true;

        PlayAttackAnimation();
        yield return new WaitForSeconds(guardianAnimationDelay);

        if (spawnBurn && burnTurns > 0)
            EnsureBurnVfx(target, targetIsPlayer);

        if (spawnStun)
            EnsureStunVfx(target);

        if (applyEffect != null)
            applyEffect.Invoke();

        if (showDamagePopup && damageAmount > 0)
            DamagePopupFx.ShowDamage(damagePopupPrefab, target, damageAmount, false);

        GameObject vfx = (!spawnBurn && !spawnStun) ? SpawnVfx(currentGuardian.vfxPrefab, target) : null;
        yield return new WaitForSeconds(guardianVfxDuration);
        if (vfx != null)
            Destroy(vfx);
    }

    public void TickBurnVfxForTurn(bool isPlayerTurn)
    {
        if (burnVfxInstance == null || burnTurnsRemaining <= 0)
            return;

        if (burnTargetIsPlayer != isPlayerTurn)
            return;

        burnTurnsRemaining = Mathf.Max(0, burnTurnsRemaining - 1);
        if (burnTurnsRemaining <= 0)
            ClearBurnVfx();
    }

    public void NotifyBurnApplied(bool isOnPlayer, int turns)
    {
        if (burnVfxInstance == null)
            return;

        if (burnTargetIsPlayer != isOnPlayer)
            return;

        burnTurnsRemaining += Mathf.Max(0, turns);
    }

    public void ResetBattleState()
    {
        battleStartApplied = false;
        aiBattleStartApplied = false;
        ClearBurnVfx();
        ClearStunVfx();
    }

    public void DeactivateGuardian()
    {
        if (spawnedGuardian != null)
            spawnedGuardian.SetActive(false);

        if (spawnedAiGuardian != null)
            spawnedAiGuardian.SetActive(false);
    }

    private bool ApplyAiGuardianEffect(GuardianDataAsset guardian, GuardianLevelData levelData, PlayerStats player, AIStats enemy, bool isBattleStart, out bool targetIsPlayer)
    {
        targetIsPlayer = false;

        if (guardian == null || levelData == null)
            return false;

        bool canApplyBattleStart = isBattleStart && guardian.applyOnBattleStart;
        bool canApplyTurn = guardian.applyOnPlayerTurn;
        if (!canApplyBattleStart && !canApplyTurn)
            return false;

        bool applied = false;

        if (guardian.guardianId == 3)
        {
            targetIsPlayer = true;
            if (enemy != null && player != null)
            {
                int damageAmount = Mathf.CeilToInt(enemy.baseAttack * Mathf.Max(0f, levelData.value1));
                if (damageAmount > 0)
                {
                    player.TakeDamage(damageAmount);
                    applied = true;
                }
            }

            return applied;
        }

        switch (guardian.element)
        {
            case GuardianElement.Leaf:
                targetIsPlayer = false;
                applied = ApplyAiLeafGuardian(enemy, levelData);
                break;
            case GuardianElement.Fire:
                targetIsPlayer = true;
                applied = ApplyAiFireGuardian(player, levelData);
                break;
            case GuardianElement.Metal:
                targetIsPlayer = false;
                applied = ApplyAiMetalGuardian(enemy, levelData);
                break;
            case GuardianElement.Earth:
                targetIsPlayer = true;
                applied = ApplyAiEarthGuardian(player, levelData);
                break;
            case GuardianElement.Water:
                targetIsPlayer = false;
                applied = ApplyAiWaterGuardian(enemy, levelData);
                break;
            case GuardianElement.Dark:
                targetIsPlayer = false;
                applied = ApplyAiDarkGuardian(enemy, levelData);
                break;
            case GuardianElement.Light:
                targetIsPlayer = false;
                applied = ApplyAiLightGuardian(enemy, levelData);
                break;
        }

        return applied;
    }
    private bool ApplyAiLeafGuardian(AIStats enemy, GuardianLevelData levelData)
    {
        if (enemy == null)
            return false;

        int healAmount = Mathf.CeilToInt(enemy.maxHealth * Mathf.Max(0f, levelData.value1) * 0.01f);
        if (healAmount > 0)
            enemy.Heal(healAmount);

        return healAmount > 0;
    }

    private bool ApplyAiFireGuardian(PlayerStats player, GuardianLevelData levelData)
    {
        if (player == null)
            return false;

        float chance = Mathf.Clamp(levelData.value1 * 0.01f, 0f, 1f);
        if (UnityEngine.Random.value > chance)
            return false;

        int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
        float burnPercent = Mathf.Max(0f, levelData.value3);
        player.ApplyStatusEffect(StatusEffectType.Burn, turns, burnPercent);
        return burnPercent > 0f;
    }

    private bool ApplyAiMetalGuardian(AIStats enemy, GuardianLevelData levelData)
    {
        if (enemy == null)
            return false;

        int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
        float bonusPercent = Mathf.Max(0f, levelData.value1);
        enemy.ApplyStatusEffect(StatusEffectType.DamageBoost, turns, bonusPercent);
        return bonusPercent > 0f;
    }

    private bool ApplyAiEarthGuardian(PlayerStats player, GuardianLevelData levelData)
    {
        if (player == null)
            return false;

        float chance = Mathf.Clamp(levelData.value1 * 0.01f, 0f, 1f);
        if (UnityEngine.Random.value > chance)
            return false;

        int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
        player.ApplyStatusEffect(StatusEffectType.Stun, turns, 0f);
        return true;
    }

    private bool ApplyAiWaterGuardian(AIStats enemy, GuardianLevelData levelData)
    {
        if (enemy == null)
            return false;

        int manaGain = Mathf.Max(0, Mathf.RoundToInt(levelData.value1));
        int rageGain = Mathf.Max(0, Mathf.RoundToInt(levelData.value2));
        if (manaGain > 0)
            enemy.GainMana(manaGain);
        if (rageGain > 0)
            enemy.GainRage(rageGain);

        return manaGain > 0 || rageGain > 0;
    }

    private bool ApplyAiDarkGuardian(AIStats enemy, GuardianLevelData levelData)
    {
        if (enemy == null)
            return false;

        bool heads = UnityEngine.Random.value > 0.5f;
        float percent = Mathf.Max(0f, heads ? levelData.value1 : levelData.value2);
        StatusEffectType type = heads ? StatusEffectType.DamageBoost : StatusEffectType.DamageTakenIncrease;
        enemy.ApplyStatusEffect(type, 1, percent);
        return percent > 0f;
    }

    private bool ApplyAiLightGuardian(AIStats enemy, GuardianLevelData levelData)
    {
        if (enemy == null)
            return false;

        int shieldAmount = Mathf.CeilToInt(enemy.maxHealth * Mathf.Max(0f, levelData.value1) * 0.01f);
        int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
        if (shieldAmount > 0)
            enemy.AddShield(shieldAmount, turns);

        return shieldAmount > 0;
    }

    private void ApplyGuardianEffect(PlayerStats player, AIStats enemy, bool isBattleStart)
    {
        if (currentGuardian == null)
            return;

        if (isBattleStart && !currentGuardian.applyOnBattleStart)
            return;

        if (!isBattleStart && !currentGuardian.applyOnPlayerTurn)
            return;

        GuardianLevelData levelData = currentGuardian.GetLevelData(CurrentGuardianLevel);
        if (levelData == null)
            return;

        bool applied = false;

        switch (currentGuardian.element)
        {
            case GuardianElement.Leaf:
                applied = ApplyLeafGuardian(player, levelData);
                break;

            case GuardianElement.Fire:
                applied = ApplyFireGuardian(enemy, levelData);
                break;

            case GuardianElement.Metal:
                applied = ApplyMetalGuardian(player, levelData);
                break;

            case GuardianElement.Earth:
                applied = ApplyEarthGuardian(enemy, levelData);
                break;

            case GuardianElement.Water:
                applied = ApplyWaterGuardian(player, levelData);
                break;

            case GuardianElement.Dark:
                applied = ApplyDarkGuardian(player, levelData);
                break;

            case GuardianElement.Light:
                applied = ApplyLightGuardian(player, levelData);
                break;
        }

        if (applied)
            PlayAttackAnimation();
    }

    private bool ApplyLeafGuardian(PlayerStats player, GuardianLevelData levelData)
    {
        if (player == null)
            return false;

        int healAmount = Mathf.CeilToInt(player.maxHP * Mathf.Max(0f, levelData.value1) * 0.01f);
        if (healAmount > 0)
            player.Heal(healAmount);

        int cleanseCount = Mathf.Max(0, Mathf.RoundToInt(levelData.value2));
        if (cleanseCount > 0)
            player.CleanseDebuff(cleanseCount);

        return healAmount > 0 || cleanseCount > 0;
    }

    private bool ApplyFireGuardian(AIStats enemy, GuardianLevelData levelData)
    {
        if (enemy == null)
            return false;

        float chance = Mathf.Clamp(levelData.value1 * 0.01f, 0f, 1f);
        if (UnityEngine.Random.value > chance)
            return false;

        int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
        float burnPercent = Mathf.Max(0f, levelData.value3);
        enemy.ApplyStatusEffect(StatusEffectType.Burn, turns, burnPercent);
        return true;
    }

    private bool ApplyMetalGuardian(PlayerStats player, GuardianLevelData levelData)
    {
        if (player == null)
            return false;

        int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
        float bonusPercent = Mathf.Max(0f, levelData.value1);
        player.ApplyStatusEffect(StatusEffectType.DamageBoost, turns, bonusPercent);
        return bonusPercent > 0f;
    }

    private bool ApplyEarthGuardian(AIStats enemy, GuardianLevelData levelData)
    {
        if (enemy == null)
            return false;

        float chance = Mathf.Clamp(levelData.value1 * 0.01f, 0f, 1f);
        if (UnityEngine.Random.value > chance)
            return false;

        int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
        enemy.ApplyStatusEffect(StatusEffectType.Stun, turns, 0f);
        return true;
    }

    private bool ApplyWaterGuardian(PlayerStats player, GuardianLevelData levelData)
    {
        if (player == null)
            return false;

        int manaGain = Mathf.Max(0, Mathf.RoundToInt(levelData.value1));
        int rageGain = Mathf.Max(0, Mathf.RoundToInt(levelData.value2));

        if (manaGain > 0)
            player.GainMana(manaGain);
        if (rageGain > 0)
            player.GainRage(rageGain);

        return manaGain > 0 || rageGain > 0;
    }

    private bool ApplyDarkGuardian(PlayerStats player, GuardianLevelData levelData)
    {
        if (player == null)
            return false;

        bool heads = UnityEngine.Random.value > 0.5f;
        int turns = 1;

        if (heads)
        {
            float bonusPercent = Mathf.Max(0f, levelData.value1);
            player.ApplyStatusEffect(StatusEffectType.DamageBoost, turns, bonusPercent);
            return bonusPercent > 0f;
        }

        float takenPercent = Mathf.Max(0f, levelData.value2);
        player.ApplyStatusEffect(StatusEffectType.DamageTakenIncrease, turns, takenPercent);
        return takenPercent > 0f;
    }

    private bool ApplyLightGuardian(PlayerStats player, GuardianLevelData levelData)
    {
        if (player == null)
            return false;

        int shieldAmount = Mathf.CeilToInt(player.maxHP * Mathf.Max(0f, levelData.value1) * 0.01f);
        int turns = Mathf.Max(1, Mathf.RoundToInt(levelData.value2));
        if (shieldAmount > 0)
            player.AddShield(shieldAmount, turns);

        return shieldAmount > 0;
    }

    private void SpawnGuardianPreview(GameObject prefab)
    {
        ClearGuardianPreview();
        if (prefab == null)
            return;

        Transform root = guardianSpawnRoot != null ? guardianSpawnRoot : transform;
        spawnedGuardian = Instantiate(prefab, root);
        spawnedGuardian.transform.localPosition = Vector3.zero;
        spawnedGuardian.transform.localRotation = Quaternion.identity;
        spawnedGuardian.transform.localScale = guardianSpawnScale;

        PlayIdleAnimation();
    }

    private void ClearGuardianPreview()
    {
        if (spawnedGuardian != null)
            Destroy(spawnedGuardian);

        spawnedGuardian = null;
    }

    private void SpawnAiGuardianPreview(GameObject prefab)
    {
        ClearAiGuardianPreview();
        if (prefab == null)
            return;

        Transform root = aiGuardianSpawnRoot != null ? aiGuardianSpawnRoot : guardianSpawnRoot;
        if (root == null)
            return;

        spawnedAiGuardian = Instantiate(prefab, root);
        spawnedAiGuardian.transform.localPosition = Vector3.zero;
        spawnedAiGuardian.transform.localRotation = Quaternion.identity;
        spawnedAiGuardian.transform.localScale = new Vector3(-Mathf.Abs(aiGuardianSpawnScale.x), aiGuardianSpawnScale.y, aiGuardianSpawnScale.z);

        PlayAiIdleAnimation();
    }

    private void ClearAiGuardianPreview()
    {
        if (spawnedAiGuardian != null)
            Destroy(spawnedAiGuardian);

        spawnedAiGuardian = null;
    }

    private Transform ResolveTargetHitPoint(PlayerStats player, AIStats enemy, bool targetIsPlayer)
    {
        return targetIsPlayer ? player?.TargetHitPoint : enemy?.TargetHitPoint;
    }

    private GameObject SpawnVfx(GameObject prefab, Transform target)
    {
        if (prefab == null || target == null)
            return null;

        GameObject instance = Instantiate(prefab, target);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        return instance;
    }

    private GameObject SpawnGuardianVfx(GuardianDataAsset guardian, Transform target)
    {
        return guardian != null ? SpawnVfx(guardian.vfxPrefab, target) : null;
    }

    private void EnsureBurnVfx(Transform target, bool targetIsPlayer)
    {
        if (target == null || currentGuardian == null || currentGuardian.vfxPrefab == null)
            return;

        if (burnVfxInstance == null)
        {
            burnVfxInstance = Instantiate(currentGuardian.vfxPrefab, target);
            burnVfxInstance.transform.localPosition = Vector3.zero;
            burnVfxInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            burnVfxInstance.transform.SetParent(target, false);
            burnVfxInstance.transform.localPosition = Vector3.zero;
        }

        burnVfxTarget = target;
        burnTargetIsPlayer = targetIsPlayer;
    }

    private void ClearBurnVfx()
    {
        if (burnVfxInstance != null)
            Destroy(burnVfxInstance);

        burnVfxInstance = null;
        burnVfxTarget = null;
        burnTurnsRemaining = 0;
    }

    public void ClearStunVfx()
    {
        if (stunVfxInstance != null)
            Destroy(stunVfxInstance);

        stunVfxInstance = null;
        stunVfxTarget = null;
    }

    private void EnsureStunVfx(Transform target)
    {
        if (target == null || currentGuardian == null || currentGuardian.vfxPrefab == null)
            return;

        if (stunVfxInstance == null)
        {
            stunVfxInstance = Instantiate(currentGuardian.vfxPrefab, target);
            stunVfxInstance.transform.localPosition = Vector3.zero;
            stunVfxInstance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            stunVfxInstance.transform.SetParent(target, false);
            stunVfxInstance.transform.localPosition = Vector3.zero;
        }

        stunVfxTarget = target;
    }

    private IEnumerator PlayCoinFlipRoutine(bool heads)
    {
        if (coinFlipPrefab == null)
            yield break;

        Transform root = coinFlipRoot != null ? coinFlipRoot : (guardianSpawnRoot != null ? guardianSpawnRoot : transform);
        GameObject coin = Instantiate(coinFlipPrefab, root);
        coin.transform.localPosition = Vector3.zero;
        coin.transform.localRotation = Quaternion.identity;
        coin.transform.localScale = coinFlipScale;

        float elapsed = 0f;
        while (elapsed < coinFlipDuration)
        {
            coin.transform.Rotate(0f, coinSpinSpeed * Time.deltaTime, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        coin.transform.localRotation = Quaternion.identity;
        SetCoinSprite(coin, heads ? coinHeadsSprite : coinTailsSprite);
        if (coinResultHoldTime > 0f)
            yield return new WaitForSeconds(coinResultHoldTime);
        Destroy(coin);
    }

    private void SetCoinSprite(GameObject coin, Sprite sprite)
    {
        if (coin == null || sprite == null)
            return;

        SpriteRenderer renderer = coin.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer != null)
        {
            renderer.sprite = sprite;
            return;
        }

        Image image = coin.GetComponentInChildren<Image>(true);
        if (image != null)
            image.sprite = sprite;
    }

    private bool BuildCoinEffect(bool heads, PlayerStats player, AIStats enemy, GuardianLevelData levelData, out bool targetIsPlayer, out System.Action applyEffect)
    {
        targetIsPlayer = true;
        applyEffect = null;
        const int coinEffectTurns = 2;

        if (heads)
        {
            targetIsPlayer = true;
            float bonusPercent = Mathf.Max(0f, levelData.value1);
            if (player != null && bonusPercent > 0f)
            {
                applyEffect = () => player.ApplyStatusEffect(StatusEffectType.DamageBoost, coinEffectTurns, bonusPercent);
                return true;
            }
        }
        else
        {
            targetIsPlayer = false;
            float takenPercent = Mathf.Max(0f, levelData.value2);
            if (enemy != null && takenPercent > 0f)
            {
                applyEffect = () => enemy.ApplyStatusEffect(StatusEffectType.DamageTakenIncrease, coinEffectTurns, takenPercent);
                return true;
            }
        }

        return false;
    }

    private void ShowMissPopup(GameObject popupPrefab, Transform target)
    {
        if (popupPrefab == null || target == null)
            return;

        string missText = GetLocalizedText("Miss", "Miss");
        DamagePopupFx.ShowText(popupPrefab, target, missText);
    }

    private string GetLocalizedText(string key, string fallback)
    {
        if (string.IsNullOrWhiteSpace(key))
            return fallback;

        LocalizationManager lm = LocalizationManager.Instance;
        if (lm != null && lm.IsLoaded)
            return lm.GetText(key, fallback);

        return fallback;
    }

    private void PlayIdleAnimation()
    {
        if (spawnedGuardian == null)
            return;

        Animator animator = spawnedGuardian.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.Play(idleAnimationName, 0, 0f);
            return;
        }

        Spine.Unity.SkeletonAnimation skeleton = spawnedGuardian.GetComponentInChildren<Spine.Unity.SkeletonAnimation>(true);
        if (skeleton != null && skeleton.state != null)
            skeleton.state.SetAnimation(0, idleAnimationName, true);
    }

    private void PlayAttackAnimation()
    {
        if (spawnedGuardian == null)
            return;

        Animator animator = spawnedGuardian.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.SetTrigger(attackTriggerName);
            return;
        }

        Spine.Unity.SkeletonAnimation skeleton = spawnedGuardian.GetComponentInChildren<Spine.Unity.SkeletonAnimation>(true);
        if (skeleton != null && skeleton.state != null)
        {
            skeleton.state.SetAnimation(0, "Attack", false);
            skeleton.state.AddAnimation(0, idleAnimationName, true, 0f);
        }
    }

    private void PlayAiIdleAnimation()
    {
        if (spawnedAiGuardian == null)
            return;

        Animator animator = spawnedAiGuardian.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.Play(idleAnimationName, 0, 0f);
            return;
        }

        Spine.Unity.SkeletonAnimation skeleton = spawnedAiGuardian.GetComponentInChildren<Spine.Unity.SkeletonAnimation>(true);
        if (skeleton != null && skeleton.state != null)
            skeleton.state.SetAnimation(0, idleAnimationName, true);
    }

    private void PlayAiAttackAnimation()
    {
        if (spawnedAiGuardian == null)
            return;

        Animator animator = spawnedAiGuardian.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.SetTrigger(attackTriggerName);
            return;
        }

        Spine.Unity.SkeletonAnimation skeleton = spawnedAiGuardian.GetComponentInChildren<Spine.Unity.SkeletonAnimation>(true);
        if (skeleton != null && skeleton.state != null)
        {
            skeleton.state.SetAnimation(0, "Attack", false);
            skeleton.state.AddAnimation(0, idleAnimationName, true, 0f);
        }
    }
}



