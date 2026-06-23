using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public static class SkillExecutor
{
    public static async UniTask<SkillResult> PlaySkillAsync(SkillData skill, SkillContext context)
    {
        if (skill == null || context == null)
            return SkillResult.Failed;

        if (context.AttackerTransform == null || context.TargetTransform == null)
            return SkillResult.Failed;

        if (!CanPayCost(skill, context))
            return SkillResult.Failed;

        SpendCost(skill, context);
        int boardValue = await ApplyBoardEffect(skill, context);

        bool hideBoardForSkill = skill.typeSkill != SkillType.Audition && context.Board != null;
        if (hideBoardForSkill)
            context.Board.SetPresentationVisible(false);

        try
        {
            if (skill.typeSkill == SkillType.Audition)
                return await ExecuteAuditionAsync(skill, context, boardValue);

            if (skill.attackType == SkillAttackType.Melee)
                return await MeleeSkill.ExecuteAsync(skill, context, boardValue);

            if (skill.rangeType == SkillRangeType.Projectile)
                return await ProjectileSkill.ExecuteAsync(skill, context, boardValue);

            if (skill.rangeType == SkillRangeType.DirectImpact)
                return await ExecuteDirectImpactAsync(skill, context, boardValue);

            return await ExecuteDirectFxAsync(skill, context, boardValue);
        }
        finally
        {
            if (hideBoardForSkill)
                context.Board.SetPresentationVisible(true);
        }
    }

    internal static int ComputeHitDamage(SkillData skill, SkillContext context, int boardValue)
    {
        int baseAttack = Mathf.Max(1, context.GetBaseAttack != null ? context.GetBaseAttack() : 1);
        int consumedGemCount = Mathf.Max(1, boardValue);

        if (skill != null && skill.boardEffectType != SkillBoardEffectType.None && boardValue > 0)
            return Mathf.Max(1, baseAttack * consumedGemCount);

        return Mathf.Max(1, baseAttack);
    }

    internal static int ApplyHitDamage(SkillData skill, SkillContext context, int hitBaseDamage)
    {
        float critRate = context.GetCritRate != null ? context.GetCritRate() : 0f;
        float critDamage = context.GetCritDamage != null ? context.GetCritDamage() : 0f;
        string attackerElement = context.GetAttackerElement != null ? context.GetAttackerElement() : string.Empty;
        string targetWeakness = context.GetTargetWeakness != null ? context.GetTargetWeakness() : string.Empty;
        BattleDamageActor attacker = BattleDamageService.BuildActor("SkillAttacker", context.AttackerTransform, critRate, critDamage, attackerElement);
        BattleDamageTarget target = BattleDamageService.BuildTarget(
            "SkillTarget",
            context.TargetTransform,
            context.ApplyDamage,
            context.DamagePopupPrefab,
            context.HitFxPrefab,
            context.HitAudioSource,
            context.HitSfx,
            targetWeakness);

        float outgoingMultiplier = context.GetOutgoingDamageMultiplier != null ? context.GetOutgoingDamageMultiplier() : 1f;
        float finalMultiplier = skill.damageMultiplier * Mathf.Max(0f, outgoingMultiplier);
        return BattleDamageService.ApplyDamage(attacker, target, hitBaseDamage, finalMultiplier);
    }

    internal static async UniTask WaitSeconds(float seconds, SkillContext context)
    {
        float safeSeconds = Mathf.Max(0f, seconds);
        if (safeSeconds <= 0f)
            return;

        await UniTask.Delay(TimeSpan.FromSeconds(safeSeconds), cancellationToken: context.CancellationToken);
    }

    private static bool CanPayCost(SkillData skill, SkillContext context)
    {
        int mana = context.GetCurrentMana != null ? context.GetCurrentMana() : int.MaxValue;
        int rage = context.GetCurrentRage != null ? context.GetCurrentRage() : int.MaxValue;
        int hp = context.GetCurrentHp != null ? context.GetCurrentHp() : int.MaxValue;
        int maxHp = context.GetMaxHp != null ? context.GetMaxHp() : hp;
        int hpCost = Mathf.CeilToInt(maxHp * Mathf.Clamp(skill.hpCostPercent, 0f, 100f) * 0.01f);

        if (mana < skill.manaCost)
            return false;
        if (rage < skill.rageCost)
            return false;
        if (hp <= hpCost)
            return false;

        return true;
    }

    private static void SpendCost(SkillData skill, SkillContext context)
    {
        if (skill.manaCost > 0)
            context.SpendMana?.Invoke(skill.manaCost);

        if (skill.rageCost > 0)
            context.SpendRage?.Invoke(skill.rageCost);

        if (skill.hpCostPercent > 0f && context.GetMaxHp != null)
        {
            int hpCost = Mathf.CeilToInt(context.GetMaxHp() * Mathf.Clamp(skill.hpCostPercent, 0f, 100f) * 0.01f);
            if (hpCost > 0)
                context.SpendHp?.Invoke(hpCost);
        }
    }

    private static async UniTask<int> ApplyBoardEffect(SkillData skill, SkillContext context)
    {
        if (context.Board == null)
            return 0;

        switch (skill.boardEffectType)
        {
            case SkillBoardEffectType.Match:
            {
                var removed = await context.Board.ClearItemsByTypesAndRefillAnimatedAsync(skill.gemTypesAffected);
                return removed != null ? removed.Count : 0;
            }
            case SkillBoardEffectType.Row:
            {
                var removed = await context.Board.ClearRandomRowAndRefillAnimatedAsync();
                return removed != null ? removed.Count : 0;
            }
            case SkillBoardEffectType.Column:
            {
                var removed = await context.Board.ClearRandomColumnAndRefillAnimatedAsync();
                return removed != null ? removed.Count : 0;
            }
            case SkillBoardEffectType.Swap:
            {
                var removed = await context.Board.AutoSwapPreferredAdjacentAndResolveAsync(skill.gemTypesAffected);
                return removed != null ? removed.Count : 0;
            }
            default:
                return 0;
        }
    }

    private static async UniTask<SkillResult> ExecuteDirectFxAsync(SkillData skill, SkillContext context, int boardValue)
    {
        context.OnSkillStart?.Invoke();

        try
        {
            string anim = string.IsNullOrEmpty(context.RangedAnimation) ? context.MeleeAnimation : context.RangedAnimation;
            int animationCount = Mathf.Max(1, skill.animationPlayCount);
            int fxCount = Mathf.Max(1, skill.fxPlayCount);
            int hitCount = Mathf.Max(1, skill.hitCount);
            int cycleCount = Mathf.Max(animationCount, Mathf.Max(fxCount, hitCount));
            int hitBaseDamage = ComputeHitDamage(skill, context, boardValue);
            int totalDamage = 0;

            for (int i = 0; i < cycleCount; i++)
            {
                if (i < animationCount)
                    context.SetAnimation?.Invoke(anim, false);

                if (i < fxCount && skill.fxPrefab != null)
                    BattleFxUtility.SpawnAutoDestroy(skill.fxPrefab, context.TargetTransform.position, Quaternion.identity, skill.animationDuration + 0.5f);

                if (i < animationCount)
                    await WaitSeconds(skill.animationDuration, context);

                if (i < hitCount)
                    totalDamage += Mathf.Max(0, ApplyHitDamage(skill, context, hitBaseDamage));

                if (i < cycleCount - 1)
                    await WaitSeconds(skill.hitDelay, context);
            }

            ApplySkillStatusEffect(skill, context, hitBaseDamage);
            context.SetAnimation?.Invoke(context.IdleAnimation, true);
            return new SkillResult(true, totalDamage, hitCount);
        }
        finally
        {
            context.OnSkillEnd?.Invoke();
        }
    }

    private static async UniTask<SkillResult> ExecuteDirectImpactAsync(SkillData skill, SkillContext context, int boardValue)
    {
        context.OnSkillStart?.Invoke();

        try
        {
            string anim = string.IsNullOrEmpty(context.RangedAnimation) ? context.MeleeAnimation : context.RangedAnimation;
            int animationCount = Mathf.Max(1, skill.animationPlayCount);
            int fxCount = Mathf.Max(1, skill.fxPlayCount);
            int hitCount = Mathf.Max(1, skill.hitCount);
            int cycleCount = Mathf.Max(animationCount, Mathf.Max(fxCount, hitCount));
            int hitBaseDamage = ComputeHitDamage(skill, context, boardValue);
            int totalDamage = 0;
            int hitIndex = 0;
            const float baseShakeAmount = 0.4f;

            for (int i = 0; i < cycleCount; i++)
            {
                if (i < animationCount)
                    context.SetAnimation?.Invoke(anim, false);

                if (i < fxCount && skill.fxPrefab != null)
                    BattleFxUtility.SpawnAutoDestroy(skill.fxPrefab, context.AttackerTransform.position, Quaternion.identity, skill.animationDuration + 0.5f);

                if (i < animationCount)
                    await WaitSeconds(skill.animationDuration, context);

                if (i < hitCount)
                {
                    totalDamage += Mathf.Max(0, ApplyHitDamage(skill, context, hitBaseDamage));
                    hitIndex++;
                    CameraShakeController.Instance?.DoCameraShake(baseShakeAmount * hitIndex / Mathf.Max(1, hitCount));
                }

                if (i < cycleCount - 1)
                    await WaitSeconds(skill.hitDelay, context);
            }

            ApplySkillStatusEffect(skill, context, hitBaseDamage);
            context.SetAnimation?.Invoke(context.IdleAnimation, true);
            return new SkillResult(true, totalDamage, hitCount);
        }
        finally
        {
            context.OnSkillEnd?.Invoke();
        }
    }

    private static async UniTask<SkillResult> ExecuteAuditionAsync(SkillData skill, SkillContext context, int boardValue)
    {
        context.OnSkillStart?.Invoke();

        try
        {
            AuditionMiniGameResult auditionResult = await ResolveAuditionResultAsync(skill, context);
            string anim = string.IsNullOrEmpty(context.RangedAnimation) ? context.MeleeAnimation : context.RangedAnimation;
            int animationCount = Mathf.Max(1, skill.animationPlayCount);
            int fxCount = Mathf.Max(1, skill.fxPlayCount);
            int hitCount = Mathf.Max(1, skill.hitCount);
            int cycleCount = Mathf.Max(animationCount, Mathf.Max(fxCount, hitCount));
            int hitBaseDamage = ComputeHitDamage(skill, context, boardValue);
            int scaledBaseDamage = Mathf.Max(1, Mathf.RoundToInt(hitBaseDamage * auditionResult.DamageMultiplier));
            int totalDamage = 0;

            for (int i = 0; i < cycleCount; i++)
            {
                if (i < animationCount)
                    context.SetAnimation?.Invoke(anim, false);

                if (i < fxCount && skill.fxPrefab != null)
                    BattleFxUtility.SpawnAutoDestroy(skill.fxPrefab, context.TargetTransform.position, Quaternion.identity, skill.animationDuration + 0.5f);

                if (i < animationCount)
                    await WaitSeconds(skill.animationDuration, context);

                if (i < hitCount)
                    totalDamage += Mathf.Max(0, ApplyHitDamage(skill, context, scaledBaseDamage));

                if (i < cycleCount - 1)
                    await WaitSeconds(skill.hitDelay, context);
            }

            ApplySkillStatusEffect(skill, context, hitBaseDamage);
            context.SetAnimation?.Invoke(context.IdleAnimation, true);
            return new SkillResult(true, totalDamage, hitCount);
        }
        finally
        {
            context.OnSkillEnd?.Invoke();
        }
    }

    private static async UniTask<AuditionMiniGameResult> ResolveAuditionResultAsync(SkillData skill, SkillContext context)
    {
        if (skill == null)
            return AuditionMiniGameResult.DefaultMiss;

        if (!context.IsPlayerControlled)
        {
            AuditionResultType aiResultType = ResolveRandomAiAuditionResult();
            return new AuditionMiniGameResult(
                aiResultType,
                ResolveAuditionMultiplier(skill, aiResultType),
                0,
                0);
        }

        if (context.PlayAuditionMiniGame == null)
        {
            return new AuditionMiniGameResult(
                AuditionResultType.Miss,
                ResolveAuditionMultiplier(skill, AuditionResultType.Miss),
                0,
                0);
        }

        return await context.PlayAuditionMiniGame.Invoke(skill);
    }

    private static AuditionResultType ResolveRandomAiAuditionResult()
    {
        int randomIndex = UnityEngine.Random.Range(0, 3);
        switch (randomIndex)
        {
            case 0:
                return AuditionResultType.Perfect;
            case 1:
                return AuditionResultType.Great;
            default:
                return AuditionResultType.Miss;
        }
    }

    private static float ResolveAuditionMultiplier(SkillData skill, AuditionResultType resultType)
    {
        if (skill == null)
            return 1f;

        switch (resultType)
        {
            case AuditionResultType.Perfect:
                return Mathf.Max(0f, skill.auditionPerfectMultiplier);
            case AuditionResultType.Great:
                return Mathf.Max(0f, skill.auditionGreatMultiplier);
            case AuditionResultType.Miss:
            default:
                return Mathf.Max(0f, skill.auditionMissMultiplier);
        }
    }

    internal static void ApplySkillStatusEffect(SkillData skill, SkillContext context, int hitBaseDamage)
    {
        if (skill == null || context == null || context.ApplyTargetStatusEffect == null)
            return;

        if (skill.effect == StatusEffectType.None || skill.roundEffect <= 0)
            return;

        int baseAttack = Mathf.Max(1, context.GetBaseAttack != null ? context.GetBaseAttack() : 1);
        float effectValue = ResolveEffectValue(skill.effect, baseAttack);
        bool useDirectValue = skill.effect == StatusEffectType.Poison || skill.effect == StatusEffectType.Burn;
        context.ApplyTargetStatusEffect.Invoke(skill.effect, skill.roundEffect, effectValue, useDirectValue);
    }

    private static float ResolveEffectValue(StatusEffectType effectType, int baseAttack)
    {
        switch (effectType)
        {
            case StatusEffectType.Poison:
            case StatusEffectType.Burn:
                return Mathf.Max(1, baseAttack);
            case StatusEffectType.DamageReduction:
                return 50f;
            case StatusEffectType.Silence:
            default:
                return 0f;
        }
    }
}

