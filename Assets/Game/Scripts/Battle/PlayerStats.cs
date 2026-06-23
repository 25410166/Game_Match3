using System;
using UnityEngine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class PlayerStats : MonoBehaviour
{
    [Header("Player Info")]
    public string playerName;
    public GameObject player;

    [Header("Pet Config (Tự động)")]
    public int petId = -1;
    public int level = 1;
    public int skillId = 0;

    [Header("Skill Visual")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private Transform projectileSpawnPoint;

[SerializeField] private Transform targetHitPoint;      // Nơi đối thủ bắn vào mình
public Transform TargetHitPoint => targetHitPoint != null ? targetHitPoint : (skeletonAnim != null ? skeletonAnim.transform : transform);
 
    [Header("Base Stats")]
    public int maxHP = 300;
    public int maxMana = 200;
    public int maxRage = 100;

    public int armor;
    public int baseAttack;
    public float critRate;
    public float critDamage;
    public string weakness;
    public AttackType attackType;
    public string element;

    public int HP { get; private set; }
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
    private float meleeAttackMoveX = -1.2f;

    public bool isAttacking { get; private set; }
    public event Action OnAttackComplete; // Notify when attack finishes (for GameManager)

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

    // Trong PlayerStats.cs
public bool ApplyBattleDataFromGameManager()
{
    if (GameManager.Instance == null || GameManager.Instance.battleData == null)
        return false;

    GameManager.BattleRuntimeData data = GameManager.Instance.battleData;
    if (!data.HasValidData) return false;

    petId = data.playerPetId;
    level = Mathf.Max(1, data.playerLevel);

    if (GameDataManager.Instance != null && GameDataManager.Instance.TryGetPetStatSnapshot(petId, level, out var snapshot))
    {
        maxHP = snapshot.baseHP;
        maxMana = snapshot.baseMana;
        maxRage = snapshot.baseRage;
        armor = snapshot.armor;
        baseAttack = snapshot.baseAttack;
        critRate = snapshot.critRate;
        critDamage = snapshot.critDamage;
        playerName = snapshot.petName;
        skillId = snapshot.skillId;
        element = snapshot.element; // Load element từ snapshot để hiển thị icon
        
        return true;
    }
    return false;
}

    private void LoadPetData(PetStatsHolder holder)
    {
        var data = holder.GetLevelData(level);
        if (data == null)
        {
            Debug.LogError($"<color=red>[PlayerStats] Pet ID {holder.petId} không có level {level}</color>");
            return;
        }

        maxHP = data.baseHP;
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

        // Load pet configuration
        bulletPrefab = data.bulletPrefab;
        meleeAttackMoveX = data.meleeAttackMoveX;
    }
public float baseScale = 0.6f;
    public void Init()
    {
        HP = maxHP;
        Mana = 0;
        Rage = 0;
        Shield = 0;
        statusEffects.Clear();
        IsStunnedThisTurn = false;
        pendingImmortalTurns = 0;
        activeImmortalTurns = 0;
        OnImmortalStateChanged?.Invoke(false);
        OnStatusEffectRemoved?.Invoke(StatusEffectType.Immortal);
        if (OnStatusEffectsReset != null)
            OnStatusEffectsReset.Invoke();
        originalLayer = player != null ? player.layer : normalLayer;

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
        if (player != null)
        {
            var pb = player.GetComponent<PetBehaviour>();
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
    }

    public void TriggerGemAttack(int matchCount, float comboMultiplier)
    {
        if (isAttacking)
            return;

        if (damagePopupPrefab == null)
            Debug.LogWarning("[PlayerStats] damagePopupPrefab is null. Damage text may not spawn.");

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
        queuedGemAttackSkill.skillId = -100;
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

    /// <summary>
    /// Set up a card attack skill to be queued and executed (called by CardManager)
    /// </summary>
    public void SetQueuedCardSkill(SkillData cardSkill, int baseDamage)
    {
        if (cardSkill == null)
            return;

        // Clone the skill so we don't modify the original
        queuedGemAttackSkill = ScriptableObject.CreateInstance<SkillData>();
        queuedGemAttackSkill.skillId = cardSkill.skillId;
        queuedGemAttackSkill.skillName = cardSkill.skillName;
        queuedGemAttackSkill.attackType = cardSkill.attackType;
        queuedGemAttackSkill.rangeType = cardSkill.rangeType;
        queuedGemAttackSkill.hitCount = cardSkill.hitCount;
        queuedGemAttackSkill.hitDelay = cardSkill.hitDelay;
        queuedGemAttackSkill.damageMultiplier = cardSkill.damageMultiplier;
        queuedGemAttackSkill.animationPlayCount = cardSkill.animationPlayCount;
        queuedGemAttackSkill.fxPlayCount = cardSkill.fxPlayCount;
        queuedGemAttackSkill.animationDuration = cardSkill.animationDuration;
        queuedGemAttackSkill.manaCost = cardSkill.manaCost;
        queuedGemAttackSkill.rageCost = cardSkill.rageCost;
        queuedGemAttackSkill.hpCostPercent = cardSkill.hpCostPercent;
        queuedGemAttackSkill.fxPrefab = cardSkill.fxPrefab;
        queuedGemAttackSkill.projectilePrefab = cardSkill.projectilePrefab;
        queuedGemAttackSkill.gemTypesAffected = cardSkill.gemTypesAffected;
        queuedGemAttackSkill.boardEffectType = cardSkill.boardEffectType;
    }

    public void Heal(int amount)
    {
        HP = StatSystem.AddClamped(HP, maxHP, amount);
        UpdateUI();
    }

    public void GainMana(int amount)
    {
        Mana = StatSystem.AddClamped(Mana, maxMana, amount);
        UpdateUI();
    }

    public void GainRage(int amount)
    {
        Rage = StatSystem.AddClamped(Rage, maxRage, amount);
        UpdateUI();
    }

    public void GainShield(int amount)
    {
        AddShield(amount, 2);
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
                GameManager.Instance.guardianManager.NotifyBurnApplied(true, safeTurns);
            return;
        }

        statusEffects.Add(new StatusEffectEntry(type, safeTurns, value, useDirectValue));

        if (OnStatusEffectUpdated != null)
            OnStatusEffectUpdated.Invoke(type, safeTurns);

        if (type == StatusEffectType.Burn && GameManager.Instance != null && GameManager.Instance.guardianManager != null)
            GameManager.Instance.guardianManager.NotifyBurnApplied(true, safeTurns);
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
                        : Mathf.CeilToInt(maxHP * Mathf.Max(0f, entry.value) * 0.01f);
                    if (damage > 0)
                    {
                        int before = HP;
                        TakeDamage(damage);
                        int actual = Mathf.Max(0, before - HP);
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
        if (damage <= 0 || HP <= 0)
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

        int before = HP;
        HP = IsImmortalActive ? Mathf.Max(1, HP - remaining) : Mathf.Max(0, HP - remaining);
        int actual = before - HP;

        UpdateUI();
        if (HP <= 0)
            PlayDead();
        if (GameManager.Instance != null)
            GameManager.Instance.ProcessImmediateBattleResultIfNeeded();

        if (actual > 0)
            StartCoroutine(TakeDamageVisualFeedback(actual));

        return actual;
    }

    public void TakeDamage(int dmg)
    {
        int hp = HP;
        int shield = Shield;
        int actualDamage = StatSystem.ApplyDamageWithShieldAndArmor(ref hp, ref shield, dmg, armor);
        HP = IsImmortalActive ? Mathf.Max(1, hp) : hp;
        Shield = shield;

        UpdateUI();
        if (HP <= 0) PlayDead();
        if (GameManager.Instance != null)
            GameManager.Instance.ProcessImmediateBattleResultIfNeeded();

        // Visual feedback for taking damage
        if (actualDamage > 0)
        {
            StartCoroutine(TakeDamageVisualFeedback(actualDamage));
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

        // Start positional hit shake on player object (or skeleton if null)
        Transform shakeTarget = player != null ? player.transform : (skeletonAnim != null ? skeletonAnim.transform : transform);
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
    public void ScheduleImmortal(int turns)
    {
        pendingImmortalTurns = Mathf.Max(pendingImmortalTurns, Mathf.Max(1, turns));
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

    public void UpdateUI()
    {
        // Raise stat changed events so UIManager (or others) can update UI
        OnStatChanged?.Invoke("HP", HP, maxHP);
        OnStatChanged?.Invoke("Shield", Shield, maxHP);
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
        if (player != null)
        {
            int layerToUse = ResolveAttackLayer();
            SetLayerRecursively(player, layerToUse);
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

            if (player != null)
                SetLayerRecursively(player, originalLayer);

            isAttacking = false;
            if (UIManager.Instance != null)
        {
            UIManager.Instance.CheckSkillRequirements();
        }
            OnAttackComplete?.Invoke();
        }
    }

    private int ResolveAttackLayer()
    {
        int fallback = attackLayer;
        AIStats targetAI = GameManager.Instance != null ? GameManager.Instance.ai : null;
        GameObject targetObject = targetAI != null
            ? (targetAI.AI != null ? targetAI.AI : targetAI.gameObject)
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
    AIStats targetAI = GameManager.Instance != null ? GameManager.Instance.ai : null;
    
    // ĐIỂM QUAN TRỌNG: Lấy TargetHitPoint của đối thủ làm điểm đích
    Transform hitTransform = (targetAI != null) ? targetAI.TargetHitPoint : null;

    return new SkillContext
    {
        AttackerTransform = skeletonAnim != null ? skeletonAnim.transform : transform,
        
        // TargetTransform bây giờ là điểm Hit Point của đối thủ
        TargetTransform = hitTransform, 
        
        // Điểm đạn bay ra từ mình
        ProjectileSpawnPoint = projectileSpawnPoint != null ? projectileSpawnPoint : transform,
        
        GetBaseAttack = () => baseAttack,
        GetCurrentMana = () => Mana,
        SpendMana = SpendManaForSkill,
        GetCurrentRage = () => Rage,
        SpendRage = SpendRageForSkill,
        GetCurrentHp = () => HP,
        GetMaxHp = () => maxHP,
        SpendHp = SpendHpForSkill,
        GetCritRate = () => critRate,
        GetCritDamage = () => critDamage,
        GetOutgoingDamageMultiplier = GetOutgoingDamageMultiplierValue,
        ApplyDamage = ApplyDamageToAIWithResult,
        ApplyTargetStatusEffect = (type, turns, value, useDirectValue) =>
        {
            if (targetAI != null)
                targetAI.ApplyStatusEffect(type, turns, value, useDirectValue);
        },
            GetAttackerElement = () => element,
            GetTargetWeakness = () => targetAI != null ? targetAI.weakness : "",
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
        IsPlayerControlled = true,
        PlayAuditionMiniGame = skill => UIManager.Instance != null
            ? UIManager.Instance.PlayAuditionMiniGameAsync(skill, this.GetCancellationTokenOnDestroy())
            : UniTask.FromResult(new AuditionMiniGameResult(AuditionResultType.Miss, Mathf.Max(0f, skill != null ? skill.auditionMissMultiplier : 1f), 0, 0)),
        CancellationToken = this.GetCancellationTokenOnDestroy()
    };
}
    private int ApplyDamageToAIWithResult(int amount)
    {
        AIStats target = GameManager.Instance != null ? GameManager.Instance.ai : null;
        if (target == null)
            return 0;

        int before = target.Health;
        float incomingMultiplier = target.GetIncomingDamageMultiplierValue();
        int adjusted = Mathf.RoundToInt(amount * Mathf.Max(0f, incomingMultiplier));
        target.TakeDamage(adjusted);
        return Mathf.Max(0, before - target.Health);
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
        HP = Mathf.Max(1, HP - Mathf.Max(0, amount));
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

        float currentX = skeletonAnim.transform.position.x;
        float direction = targetX - currentX;

        // Nếu khoảng cách quá nhỏ thì không cần xoay
        if (Mathf.Abs(direction) < 0.01f) return;

        Vector3 s = originalScale;

        // Nếu target nằm bên phải nhân vật
        if (direction > 0)
        {
            // Giữ nguyên scale gốc (giả định hướng gốc là nhìn bên phải)
            s.x = Mathf.Abs(originalScale.x);
        }
        // Nếu target nằm bên trái nhân vật
        else
        {
            // Đảo ngược scale x
            s.x = -Mathf.Abs(originalScale.x);
        }

        skeletonAnim.transform.localScale = s;
    }

    private void RestoreOriginalScale()
    {
        if (skeletonAnim == null) return;
        skeletonAnim.transform.localScale = originalScale;
    }
    
}




