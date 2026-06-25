using System;
using UnityEngine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class AIStats : MonoBehaviour
{
    [Header("Pet Config (T? d?ng)")]
    public int petId = -1;
    public int level = 1;
    public int skillId = 0;
    public GameObject AI;
    public string petName;

    [Header("Skill Visual")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private Transform projectileSpawnPoint;
[SerializeField] private Transform targetHitPoint;      // Noi Player b?n vÃƒÂ o AI
// Cung c?p thu?c tÃƒÂ­nh d? Player cÃƒÂ³ th? truy c?p di?m dÃƒÂ­ch c?a AI
public Transform TargetHitPoint => targetHitPoint != null ? targetHitPoint : (skeletonAnim != null ? skeletonAnim.transform : transform);
    [Header("Max Stats")]
    public int maxHealth = 300;
    public int maxMana = 200;
    public int maxRage = 100;

    public int armor;
    public int baseAttack;
    public float critRate;
    public float critDamage;
    public string weakness;
    public AttackType attackType;
    public string element;

    public int Health { get; private set; }
    public int Mana { get; private set; }
    public int Rage { get; private set; }
    public int Shield { get; private set; }
    private int shieldTurns;
    public GameObject DamagePopupPrefab => damagePopupPrefab;
    public GameObject HitEffectPrefab => hitEffectPrefab;

    // UI is managed by UIManager. Local UI fields removed.

    [Header("Spine Animation")]
    public SkeletonAnimation skeletonAnim;
    private string currentAnim;

    [Header("Animation Names")]
    public string idleAnim = "Idle";
    public string attackMeleeAnim = "Attack";
    public string attackRangedAnim = "Attack";
    public string deadAnim = "Dead";
    public string walkAnim = "Walk";

    [Header("Animation Speed")]
    [SerializeField] private float attackAnimSpeedMultiplier = 1f;

    [Header("Layer Settings")]
    [SerializeField] private int attackLayer = 20;      // Layer when attacking
    [SerializeField] private int normalLayer = 11;       // Normal layer
    private int originalLayer;
    private Vector3 originalScale;

    [Header("Flip Settings")]
    public bool shouldFlip = true; // Whether pet should flip when moving

    [Header("Visual Feedback")]
    [SerializeField] private GameObject hitEffectPrefab; // Instantiate at pet when hit
    [SerializeField] private float hitFlashDuration = 0.2f;
    [SerializeField] private Color hitFlashColor = Color.red;
    private SkeletonGraphic skeletonGraphic; // For color flashing
    private Color originalColor;

    // Pet configuration (loaded from PetLevelData)
    private GameObject bulletPrefab;
    private float meleeAttackMoveX = 1.2f;  // Positive for AI (moves right)

    public bool isAttacking { get; private set; }
    public event Action OnAttackComplete; // Notify when attack finishes

    private bool isInitialized = false;
    private SkillData runtimeDefaultSkill;
    private SkillData queuedGemAttackSkill;

    // Event to notify listeners (e.g., UIManager) about stat changes
    public event Action<string, int, int> OnStatChanged;
    public event Action<StatusEffectType, int> OnStatusEffectUpdated;
    public event Action<StatusEffectType> OnStatusEffectRemoved;
    public event Action OnStatusEffectsReset;
    private readonly List<StatusEffectEntry> statusEffects = new List<StatusEffectEntry>();
    public bool IsStunnedThisTurn { get; private set; }
    public event Action<bool> OnImmortalStateChanged;
    private int pendingImmortalTurns;
    private int activeImmortalTurns;
    public bool IsImmortalActive => activeImmortalTurns > 0;

    private void Awake()
    {
        StartCoroutine(AutoLoadPetData());
    }

    private IEnumerator AutoLoadPetData()
    {
        yield return new WaitForEndOfFrame();

        bool loadedFromBattleData = ApplyBattleDataFromGameManager();

        if (!loadedFromBattleData && skeletonAnim != null)
        {
            var holder = skeletonAnim.gameObject.GetComponentInParent<PetStatsHolder>();
            if (holder != null)
            {
                petId = holder.petId;
                LoadPetData(holder);
            }
        }

        if (!isInitialized)
        {
            Init();
            isInitialized = true;
        }
    }

    public bool ApplyBattleDataFromGameManager()
    {
        if (GameManager.Instance == null || GameManager.Instance.battleData == null)
            return false;

        GameManager.BattleRuntimeData data = GameManager.Instance.battleData;
        GameManager.MapEnemyData enemyData = GameManager.Instance.currentEnemy;

        int resolvedEnemyId = enemyData != null && enemyData.petId >= 0 ? enemyData.petId : data.enemyPetId;
        int resolvedEnemyLevel = enemyData != null && enemyData.level > 0 ? enemyData.level : data.enemyLevel;

        if (resolvedEnemyId < 0)
            return false;

        petId = resolvedEnemyId;
        level = Mathf.Max(1, resolvedEnemyLevel);

        if (GameDataManager.Instance != null && GameDataManager.Instance.TryGetPetStatSnapshot(petId, level, out var snapshot))
        {
            maxHealth = snapshot.baseHP;
            maxMana = snapshot.baseMana;
            maxRage = snapshot.baseRage;
            armor = snapshot.armor;
            baseAttack = snapshot.baseAttack;
            critRate = snapshot.critRate;
            critDamage = snapshot.critDamage;
            petName = snapshot.petName;
            skillId = snapshot.skillId;
            element = snapshot.element;
            return true;
        }

        return false;
    }

    private void LoadPetData(PetStatsHolder holder)
    {
        var data = holder.GetLevelData(level);
        if (data == null)
        {
            Debug.LogError($"<color=red>[AIStats] Pet ID {holder.petId} khÃƒÂ´ng cÃƒÂ³ level {level}</color>");
            return;
        }

        maxHealth = data.baseHP;
        maxMana = data.baseMana;
        maxRage = data.baseRage;
        armor = data.armor;
        baseAttack = data.baseAttack;
        critRate = data.critRate;
        critDamage = data.critDamage;
        weakness = data.weakness;
        attackType = data.attackType;
        skillId = data.skillId;
        element = "";
        if (GameDataManager.Instance != null)
        {
            GameDataManager.PetStatSnapshot snap;
            if (GameDataManager.Instance.TryGetPetStatSnapshot(holder.petId, level, out snap))
                element = snap.element;
        }
        petName = holder.petName;

        // Load pet configuration
        bulletPrefab = data.bulletPrefab;
        meleeAttackMoveX = data.meleeAttackMoveX;  // Use configured value (typically positive for AI)

        Debug.Log($"<color=orange>AI PET LOADED: {holder.petName} (ID {petId}) LV{level}</color>");
    }
public float baseScale = 0.6f;
    public void Init()
    {
        Health = maxHealth;
        Mana = 0;
        Rage = 0;
        Shield = 0;
        shieldTurns = 0;
        statusEffects.Clear();
        IsStunnedThisTurn = false;
        pendingImmortalTurns = 0;
        activeImmortalTurns = 0;
        OnImmortalStateChanged?.Invoke(false);
        OnStatusEffectRemoved?.Invoke(StatusEffectType.Immortal);
        if (OnStatusEffectsReset != null)
            OnStatusEffectsReset.Invoke();
        originalLayer = AI != null ? AI.layer : normalLayer;

        // Cache SkeletonGraphic for color flashing
        if (skeletonAnim != null)
        {
            skeletonGraphic = skeletonAnim.GetComponent<SkeletonGraphic>();
            if (skeletonGraphic != null)
                originalColor = skeletonGraphic.color;
            // cache original localScale so flip/restore works for prefabs that start mirrored
           
            originalScale = skeletonAnim.transform.localScale;
        }

        // Try to read animation names from PetBehaviour (if present)
        if (AI != null)
        {
            var pb = AI.GetComponent<PetBehaviour>();
            if (pb != null && pb.petData != null)
            {
                idleAnim = pb.petData.idleAnim;
                attackMeleeAnim = pb.petData.attackMeleeAnim;
                attackRangedAnim = pb.petData.attackRangedAnim;
                deadAnim = pb.petData.deadAnim;
            }
        }

        UpdateUI();
        PlayIdle();
        
    }

    public void ApplyEffect(int itemId, int count = 1)
    {
        if (itemId == BoardEffectSystem.AttackItemId)
        {
            TriggerGemAttack(count, 1f);
        }

        UpdateUI();
    }

    public void TriggerGemAttack(int matchCount, float comboMultiplier)
    {
        if (isAttacking)
            return;

        if (damagePopupPrefab == null)
            Debug.LogWarning("[AIStats] damagePopupPrefab is null. Damage text may not spawn.");

        int safeMatchCount = Mathf.Max(3, matchCount);
        float safeCombo = Mathf.Max(1f, comboMultiplier);

        float baseMultiplier = safeMatchCount == 3 ? 1f : safeMatchCount == 4 ? 1.2f : safeMatchCount == 5 ? 1.5f : 2f;
        float bonusRatio = safeMatchCount == 4 ? 1f : safeMatchCount == 5 ? 0.05f : safeMatchCount >= 6 ? 0.1f : 0f;
        float finalMultiplier = Mathf.Max(0.1f, (baseMultiplier + bonusRatio) * safeCombo);

        float rageMultiplier = 1f;
        int rageCost = 0;
        if (Rage >= 100)
        {
            rageCost = 100;
            rageMultiplier = 1.5f;
        }
        else if (Rage >= 50)
        {
            rageCost = 50;
            rageMultiplier = 1.2f;
        }

        if (rageCost > 0)
        {
            Rage = Mathf.Max(0, Rage - rageCost);
            UpdateUI();
        }

        finalMultiplier *= rageMultiplier;

        queuedGemAttackSkill = ScriptableObject.CreateInstance<SkillData>();
        queuedGemAttackSkill.skillId = -200;
        queuedGemAttackSkill.skillName = "Gem Attack";
        queuedGemAttackSkill.attackType = attackType == AttackType.Melee ? SkillAttackType.Melee : SkillAttackType.Range;
        queuedGemAttackSkill.rangeType = SkillRangeType.DirectFX;
        queuedGemAttackSkill.hitCount = 1;
        queuedGemAttackSkill.hitDelay = 0f;
        queuedGemAttackSkill.damageMultiplier = finalMultiplier;
        queuedGemAttackSkill.animationPlayCount = 1;
        queuedGemAttackSkill.fxPlayCount = 1;
        queuedGemAttackSkill.animationDuration = 1.2f;
        queuedGemAttackSkill.boardEffectType = SkillBoardEffectType.None;

        Attack();
    }

    public int Heal(int amount)
    {
        int before = Health;
        Health = StatSystem.AddClamped(Health, maxHealth, amount);
        int actualGain = Mathf.Max(0, Health - before);
        UpdateUI();
        return actualGain;
    }

    public int GainMana(int amount)
    {
        int before = Mana;
        Mana = StatSystem.AddClamped(Mana, maxMana, amount);
        int actualGain = Mathf.Max(0, Mana - before);
        UpdateUI();
        return actualGain;
    }

    public int GainRage(int amount)
    {
        int before = Rage;
        Rage = StatSystem.AddClamped(Rage, maxRage, amount);
        int actualGain = Mathf.Max(0, Rage - before);
        UpdateUI();
        return actualGain;
    }

    public void TakeDamage(int amount)
    {
        int hp = Health;
        int shield = Shield;
        int finalDmg = StatSystem.ApplyDamageWithShieldAndArmor(ref hp, ref shield, amount, armor);
        Health = IsImmortalActive ? Mathf.Max(1, hp) : hp;
        Shield = shield;

        UpdateUI();
        if (Health <= 0) PlayDead();
        if (GameManager.Instance != null)
            GameManager.Instance.ProcessImmediateBattleResultIfNeeded();

        // Visual feedback for taking damage
        if (finalDmg > 0)
        {
            AudioManager.Instance?.PlayBattleHitSound();
            StartCoroutine(TakeDamageVisualFeedback(finalDmg));
        }
    }

    private Coroutine _hitShakeCoroutine;

    private IEnumerator TakeDamageVisualFeedback(int damageAmount)
    {
        // Play hit effect
        if (hitEffectPrefab != null && skeletonAnim != null)
        {
            BattleFxUtility.SpawnAutoDestroy(hitEffectPrefab, skeletonAnim.transform.position, Quaternion.identity, hitFlashDuration + 0.5f);
        }

        // Start camera shake if available
        CameraShakeController.Instance?.DoCameraShake(0.25f);

        // Start positional hit shake on AI object (or skeleton if null)
        Transform shakeTarget = AI != null ? AI.transform : (skeletonAnim != null ? skeletonAnim.transform : transform);
        if (_hitShakeCoroutine != null)
            StopCoroutine(_hitShakeCoroutine);
        _hitShakeCoroutine = StartCoroutine(HitShakeRoutine(shakeTarget, 0.32f, 0.12f, 20f));

        // Flash red for 1 second
        if (skeletonGraphic != null)
        {
            skeletonGraphic.color = hitFlashColor;
        }

        // Flash duration fixed at 1s
        yield return new WaitForSeconds(1f);

        // Restore color
        if (skeletonGraphic != null)
        {
            skeletonGraphic.color = originalColor;
        }

        // Stop shake and restore position
        if (_hitShakeCoroutine != null)
        {
            StopCoroutine(_hitShakeCoroutine);
            _hitShakeCoroutine = null;
        }
        if (shakeTarget != null)
        {
            if (shakeTarget.parent != null)
                shakeTarget.localPosition = Vector3.zero;
            else
                shakeTarget.position = shakeTarget.position; // no-op but kept for clarity
        }
    }

    private IEnumerator HitShakeRoutine(Transform t, float duration, float amplitude, float frequency)
    {
        if (t == null)
            yield break;

        Vector3 originalLocal = t.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            float damping = 1f - progress; // reduce amplitude over time
            float offset = Mathf.Sin(elapsed * frequency) * amplitude * damping;
            t.localPosition = originalLocal + new Vector3(offset, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // restore
        t.localPosition = originalLocal;
    }

    public void AddShield(int amount, int durationTurns = 2)
    {
        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
            return;

        Shield += safeAmount;
        shieldTurns = Mathf.Max(shieldTurns, Mathf.Max(1, durationTurns));
        UpdateUI();
    }

    public void TickShieldDuration()
    {
        if (shieldTurns <= 0)
            return;

        shieldTurns--;
        if (shieldTurns <= 0)
            Shield = 0;
    }

    public void ApplyStatusEffect(StatusEffectType type, int turns, float value)
    {
        ApplyStatusEffect(type, turns, value, false);
    }

    public void ApplyStatusEffect(StatusEffectType type, int turns, float value, bool useDirectValue)
    {
        int safeTurns = Mathf.Max(1, turns);
        StatusEffectEntry existing = statusEffects.Find(e => e != null && e.type == type);
        if (existing != null)
        {
            existing.remainingTurns = Mathf.Max(existing.remainingTurns, safeTurns);
            existing.value = Mathf.Max(existing.value, value);
            existing.useDirectValue = existing.useDirectValue || useDirectValue;
            if (OnStatusEffectUpdated != null)
                OnStatusEffectUpdated.Invoke(type, existing.remainingTurns);
            if (type == StatusEffectType.Burn && GameManager.Instance != null && GameManager.Instance.guardianManager != null)
                GameManager.Instance.guardianManager.NotifyBurnApplied(false, safeTurns);
            return;
        }

        statusEffects.Add(new StatusEffectEntry(type, safeTurns, value, useDirectValue));

        if (OnStatusEffectUpdated != null)
            OnStatusEffectUpdated.Invoke(type, safeTurns);

        if (type == StatusEffectType.Burn && GameManager.Instance != null && GameManager.Instance.guardianManager != null)
            GameManager.Instance.guardianManager.NotifyBurnApplied(false, safeTurns);
    }

    public List<StatusEffectEntry> GetStatusEffectsSnapshot()
    {
        List<StatusEffectEntry> snapshot = new List<StatusEffectEntry>();
        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectEntry entry = statusEffects[i];
            if (entry == null)
                continue;

            snapshot.Add(new StatusEffectEntry(entry.type, entry.remainingTurns, entry.value, entry.useDirectValue));
        }

        if (activeImmortalTurns > 0)
            snapshot.Add(new StatusEffectEntry(StatusEffectType.Immortal, activeImmortalTurns, 0f));

        return snapshot;
    }

    public void CleanseDebuff(int count)
    {
        if (count <= 0 || statusEffects.Count == 0)
            return;

        int removed = 0;
        for (int i = statusEffects.Count - 1; i >= 0 && removed < count; i--)
        {
            StatusEffectEntry entry = statusEffects[i];
            if (entry == null)
                continue;

            if (!IsDebuff(entry.type))
                continue;

            statusEffects.RemoveAt(i);
            removed++;
            if (OnStatusEffectRemoved != null)
                OnStatusEffectRemoved.Invoke(entry.type);
        }
    }

    public void TickStatusEffects()
    {
        IsStunnedThisTurn = false;
        if (statusEffects.Count == 0)
            return;

        List<StatusEffectType> removedTypes = null;
        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectEntry entry = statusEffects[i];
            if (entry == null || entry.remainingTurns <= 0)
                continue;

            switch (entry.type)
            {
                case StatusEffectType.Poison:
                case StatusEffectType.Burn:
                {
                    int damage = entry.useDirectValue
                        ? Mathf.RoundToInt(Mathf.Max(0f, entry.value))
                        : Mathf.CeilToInt(maxHealth * Mathf.Max(0f, entry.value) * 0.01f);
                    if (damage > 0)
                    {
                        int before = Health;
                        TakeDamage(damage);
                        int actual = Mathf.Max(0, before - Health);
                        if (actual > 0)
                            DamagePopupFx.ShowDamage(DamagePopupPrefab, TargetHitPoint, actual, false);
                    }
                    break;
                }
                case StatusEffectType.Stun:
                    IsStunnedThisTurn = true;
                    break;
            }

            entry.remainingTurns--;
            if (entry.remainingTurns <= 0)
            {
                if (removedTypes == null)
                    removedTypes = new List<StatusEffectType>();
                removedTypes.Add(entry.type);
            }
            else if (OnStatusEffectUpdated != null)
            {
                OnStatusEffectUpdated.Invoke(entry.type, entry.remainingTurns);
            }
        }

        if (removedTypes != null && removedTypes.Count > 0)
        {
            for (int i = 0; i < removedTypes.Count; i++)
            {
                StatusEffectType type = removedTypes[i];
                statusEffects.RemoveAll(e => e != null && e.type == type);
                if (OnStatusEffectRemoved != null)
                    OnStatusEffectRemoved.Invoke(type);
            }
        }
        UpdateUI();
    }

    public float GetOutgoingDamageMultiplierValue()
    {
        float bonus = 0f;
        float reduction = 0f;
        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectEntry entry = statusEffects[i];
            if (entry == null)
                continue;

            if (entry.type == StatusEffectType.DamageBoost)
                bonus += entry.value;
            else if (entry.type == StatusEffectType.DamageReduction)
                reduction += entry.value;
        }

        float multiplier = 1f + Mathf.Max(0f, bonus) * 0.01f - Mathf.Max(0f, reduction) * 0.01f;
        return Mathf.Max(0f, multiplier);
    }

    public float GetIncomingDamageMultiplierValue()
    {
        float bonus = 0f;
        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectEntry entry = statusEffects[i];
            if (entry != null && entry.type == StatusEffectType.DamageTakenIncrease)
                bonus += entry.value;
        }

        return 1f + Mathf.Max(0f, bonus) * 0.01f;
    }

    private bool IsDebuff(StatusEffectType type)
    {
        return type == StatusEffectType.Poison
            || type == StatusEffectType.Burn
            || type == StatusEffectType.Silence
            || type == StatusEffectType.DamageReduction
            || type == StatusEffectType.Stun
            || type == StatusEffectType.DamageTakenIncrease;
    }

    public int ReduceMana(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        int reduced = Mathf.Min(Mana, safeAmount);
        Mana -= reduced;
        UpdateUI();
        return reduced;
    }

    public int ReduceRage(int amount)
    {
        int safeAmount = Mathf.Max(0, amount);
        int reduced = Mathf.Min(Rage, safeAmount);
        Rage -= reduced;
        UpdateUI();
        return reduced;
    }

    public int TakeRawDamage(int damage)
    {
        if (damage <= 0 || Health <= 0)
            return 0;

        int remaining = Mathf.Max(0, damage);
        if (Shield > 0)
        {
            int absorbed = Mathf.Min(Shield, remaining);
            Shield -= absorbed;
            remaining -= absorbed;
        }

        if (remaining <= 0)
            return 0;

        int before = Health;
        Health = IsImmortalActive ? Mathf.Max(1, Health - remaining) : Mathf.Max(0, Health - remaining);
        int actual = before - Health;

        UpdateUI();
        if (Health <= 0)
            PlayDead();
        if (GameManager.Instance != null)
            GameManager.Instance.ProcessImmediateBattleResultIfNeeded();

        if (actual > 0)
        {
            AudioManager.Instance?.PlayBattleHitSound();
            StartCoroutine(TakeDamageVisualFeedback(actual));
        }
        return actual;
    }

    public void ScheduleImmortal(int turns)
    {
        pendingImmortalTurns = Mathf.Max(pendingImmortalTurns, Mathf.Max(1, turns));
    }

    public void TickImmortalTurnStart()
    {
        if (activeImmortalTurns > 0)
        {
            activeImmortalTurns--;
            if (activeImmortalTurns <= 0)
            {
                activeImmortalTurns = 0;
                OnImmortalStateChanged?.Invoke(false);
            }
        }

        if (activeImmortalTurns <= 0 && pendingImmortalTurns > 0)
        {
            activeImmortalTurns = pendingImmortalTurns;
            pendingImmortalTurns = 0;
            OnImmortalStateChanged?.Invoke(true);
        }
    }
    public void ActivatePendingImmortal()
    {
        if (activeImmortalTurns > 0 || pendingImmortalTurns <= 0)
            return;

        activeImmortalTurns = pendingImmortalTurns;
        pendingImmortalTurns = 0;
        OnImmortalStateChanged?.Invoke(true);
        OnStatusEffectUpdated?.Invoke(StatusEffectType.Immortal, activeImmortalTurns);
    }

    public void ConsumeImmortalRoundOnOwnerTurnStart()
    {
        if (activeImmortalTurns <= 0)
            return;

        activeImmortalTurns--;
        if (activeImmortalTurns > 0)
        {
            OnStatusEffectUpdated?.Invoke(StatusEffectType.Immortal, activeImmortalTurns);
            return;
        }

        activeImmortalTurns = 0;
        OnImmortalStateChanged?.Invoke(false);
        OnStatusEffectRemoved?.Invoke(StatusEffectType.Immortal);
    }

    private void UpdateUI()
    {
        // Raise stat changed events instead of directly modifying UI.
        OnStatChanged?.Invoke("HP", Health, maxHealth);
        OnStatChanged?.Invoke("Shield", Shield, maxHealth);
        OnStatChanged?.Invoke("Mana", Mana, maxMana);
        OnStatChanged?.Invoke("Rage", Rage, maxRage);
    }

    // ================= SPINE + ATTACK =================
    public void Attack()
    {
        if (isAttacking)
            return;

        ExecuteAttackAsync().Forget();
    }

    private async UniTaskVoid ExecuteAttackAsync()
    {
        isAttacking = true;
        if (AI != null)
        {
            int layerToUse = ResolveAttackLayer();
            SetLayerRecursively(AI, layerToUse);
        }

        try
        {
            SkillData selectedSkill = ResolveSkillData();
            SkillContext context = BuildSkillContext();

            SkillResult result = await SkillExecutor.PlaySkillAsync(selectedSkill, context);
            if (!result.Executed && selectedSkill != runtimeDefaultSkill)
            {
                await SkillExecutor.PlaySkillAsync(GetRuntimeDefaultSkill(), context);
            }
        }
        finally
        {
            if (queuedGemAttackSkill != null)
            {
                Destroy(queuedGemAttackSkill);
                queuedGemAttackSkill = null;
            }

            if (AI != null)
                SetLayerRecursively(AI, originalLayer);

            isAttacking = false;
            OnAttackComplete?.Invoke();
        }
    }

    private int ResolveAttackLayer()
    {
        int fallback = attackLayer;
        PlayerStats targetPlayer = GameManager.Instance != null ? GameManager.Instance.player : null;
        GameObject targetObject = targetPlayer != null
            ? (targetPlayer.player != null ? targetPlayer.player : targetPlayer.gameObject)
            : null;

        if (targetObject == null)
            return fallback;

        return Mathf.Clamp(targetObject.layer + 1, 0, 31);
    }

    private SkillData ResolveSkillData()
    {
        if (queuedGemAttackSkill != null)
        {
            if (queuedGemAttackSkill.skillId > 0 && HasStatusEffect(StatusEffectType.Silence))
                return GetRuntimeDefaultSkill();

            return queuedGemAttackSkill;
        }

        if (skillId > 0 && !HasStatusEffect(StatusEffectType.Silence) && GameDataManager.Instance != null)
        {
            SkillData loaded = GameDataManager.Instance.GetSkillData(skillId);
            if (loaded != null)
                return loaded;
        }

        return GetRuntimeDefaultSkill();
    }

    public bool CanUseEquippedSkill()
    {
        if (isAttacking || skillId <= 0 || HasStatusEffect(StatusEffectType.Silence) || GameDataManager.Instance == null)
            return false;

        SkillData skill = GameDataManager.Instance.GetSkillData(skillId);
        if (skill == null)
            return false;

        int hpCost = Mathf.CeilToInt(maxHealth * Mathf.Clamp(skill.hpCostPercent, 0f, 100f) * 0.01f);
        return Mana >= skill.manaCost
            && Rage >= skill.rageCost
            && Health > hpCost;
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        for (int i = 0; i < statusEffects.Count; i++)
        {
            StatusEffectEntry entry = statusEffects[i];
            if (entry != null && entry.type == type && entry.remainingTurns > 0)
                return true;
        }

        return false;
    }

    private SkillData GetRuntimeDefaultSkill()
    {
        if (runtimeDefaultSkill != null)
            return runtimeDefaultSkill;

        runtimeDefaultSkill = ScriptableObject.CreateInstance<SkillData>();
        runtimeDefaultSkill.skillId = 0;
        runtimeDefaultSkill.skillName = "Normal Attack";
        runtimeDefaultSkill.attackType = attackType == AttackType.Melee ? SkillAttackType.Melee : SkillAttackType.Range;
        runtimeDefaultSkill.rangeType = SkillRangeType.DirectFX;
        runtimeDefaultSkill.hitCount = 1;
        runtimeDefaultSkill.hitDelay = 0f;
        runtimeDefaultSkill.damageMultiplier = 1f;
        runtimeDefaultSkill.animationPlayCount = 1;
        runtimeDefaultSkill.fxPlayCount = 1;
        runtimeDefaultSkill.animationDuration = 1.2f;
        runtimeDefaultSkill.boardEffectType = SkillBoardEffectType.None;

        return runtimeDefaultSkill;
    }

   private SkillContext BuildSkillContext()
{
    PlayerStats targetPlayer = GameManager.Instance != null ? GameManager.Instance.player : null;
    
    // ÃƒÂI?M QUAN TR?NG: L?y TargetHitPoint c?a Player lÃƒÂ m di?m dÃƒÂ­ch
    Transform hitTransform = (targetPlayer != null) ? targetPlayer.TargetHitPoint : null;

    return new SkillContext
    {
        AttackerTransform = skeletonAnim != null ? skeletonAnim.transform : transform,
        
        // TargetTransform bÃƒÂ¢y gi? lÃƒÂ  di?m Hit Point c?a Player
        TargetTransform = hitTransform,
        
        // ÃƒÂi?m d?n bay ra t? AI
        ProjectileSpawnPoint = projectileSpawnPoint != null ? projectileSpawnPoint : transform,
        
        GetBaseAttack = () => baseAttack,
        GetCurrentMana = () => Mana,
        SpendMana = SpendManaForSkill,
        GetCurrentRage = () => Rage,
        SpendRage = SpendRageForSkill,
        GetCurrentHp = () => Health,
        GetMaxHp = () => maxHealth,
        SpendHp = SpendHpForSkill,
        GetCritRate = () => critRate,
        GetCritDamage = () => critDamage,
        GetOutgoingDamageMultiplier = GetOutgoingDamageMultiplierValue,
        ApplyDamage = ApplyDamageToPlayerWithResult,
        ApplyTargetStatusEffect = (type, turns, value, useDirectValue) =>
        {
            if (targetPlayer != null)
                targetPlayer.ApplyStatusEffect(type, turns, value, useDirectValue);
        },
            GetAttackerElement = () => element,
            GetTargetWeakness = () => targetPlayer != null ? targetPlayer.weakness : "",
        SetAnimation = SetAnim,
        FlipTowards = FlipTowards,
        ShouldFlip = shouldFlip,
        MoveSpeed = 18f,
        MeleeOffsetX = meleeAttackMoveX,
        MeleeAttackMoveX = meleeAttackMoveX,
        IdleAnimation = idleAnim,
        WalkAnimation = walkAnim,
        MeleeAnimation = attackMeleeAnim,
        RangedAnimation = attackRangedAnim,
        DamagePopupPrefab = damagePopupPrefab,
        HitFxPrefab = hitEffectPrefab,
        ProjectilePrefabOverride = bulletPrefab,
        Board = Board.Instance,
        IsPlayerControlled = false,
        PlayAuditionMiniGame = skill => UniTask.FromResult(new AuditionMiniGameResult(AuditionResultType.Great, Mathf.Max(0f, skill != null ? skill.auditionGreatMultiplier : 2f), 0, 0)),
        CancellationToken = this.GetCancellationTokenOnDestroy()
    };
}
    private int ApplyDamageToPlayerWithResult(int amount)
    {
        PlayerStats target = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (target == null)
            return 0;

        int before = target.HP;
        float incomingMultiplier = target.GetIncomingDamageMultiplierValue();
        int adjusted = Mathf.RoundToInt(amount * Mathf.Max(0f, incomingMultiplier));
        target.TakeDamage(adjusted);
        return Mathf.Max(0, before - target.HP);
    }

    private void SpendManaForSkill(int amount)
    {
        Mana = Mathf.Max(0, Mana - Mathf.Max(0, amount));
        UpdateUI();
    }

    private void SpendRageForSkill(int amount)
    {
        Rage = Mathf.Max(0, Rage - Mathf.Max(0, amount));
        UpdateUI();
    }

    private void SpendHpForSkill(int amount)
    {
        Health = Mathf.Max(1, Health - Mathf.Max(0, amount));
        UpdateUI();
    }

    public void PlayDead() => SetAnim("Dead", false);
    public void PlayIdle() => SetAnim("Idle", true);

    private void SetAnim(string n, bool l)
    {
        if (!skeletonAnim)
            return;

        if (currentAnim == n && l)
            return;

        var entry = skeletonAnim.state.SetAnimation(0, n, l);
        if (entry != null)
            entry.TimeScale = GetAnimTimeScale(n);
        currentAnim = n;
    }

    private float GetAnimTimeScale(string animName)
    {
        float baseScale = skeletonAnim != null ? skeletonAnim.timeScale : 1f;
        bool isAttack = animName == attackMeleeAnim || animName == attackRangedAnim;
        float multiplier = isAttack ? Mathf.Max(0.01f, attackAnimSpeedMultiplier) : 1f;
        return baseScale * multiplier;
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    private void FlipTowards(float targetX)
    {
        if (skeletonAnim == null) return;
        float direction = targetX - skeletonAnim.transform.position.x;
        if (Mathf.Abs(direction) < 0.01f) return;

        Vector3 s = originalScale;
        // N?u m?c tiÃƒÂªu ? bÃƒÂªn ph?i AI -> scale x duong (ho?c ÃƒÂ¢m tÃƒÂ¹y asset)
        // N?u m?c tiÃƒÂªu ? bÃƒÂªn trÃƒÂ¡i AI -> scale x ngu?c l?i
        s.x = (direction > 0) ? Mathf.Abs(originalScale.x) : -Mathf.Abs(originalScale.x);
        skeletonAnim.transform.localScale = s;
    }

    private void RestoreOriginalScale()
    {
        if (skeletonAnim == null) return;
        skeletonAnim.transform.localScale = originalScale;
    }
    public void UpdateOriginalScale(Vector3 scale)
{
    originalScale = scale;
    if (skeletonAnim != null)
    {
        skeletonAnim.transform.localScale = scale;
    }
}
}





