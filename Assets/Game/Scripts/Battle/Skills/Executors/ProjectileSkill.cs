using Cysharp.Threading.Tasks;
using UnityEngine;

public static class ProjectileSkill
{
    public static async UniTask<SkillResult> ExecuteAsync(SkillData skill, SkillContext context, int boardValue)
    {
        context.OnSkillStart?.Invoke();

        try
        {
            string anim = string.IsNullOrEmpty(context.RangedAnimation) ? context.MeleeAnimation : context.RangedAnimation;
            int animationCount = Mathf.Max(1, skill.animationPlayCount);
            int fxCount = Mathf.Max(1, skill.fxPlayCount);
            int hitCount = Mathf.Max(1, skill.hitCount);
            int cycleCount = Mathf.Max(animationCount, Mathf.Max(fxCount, hitCount));
            int hitBaseDamage = SkillExecutor.ComputeHitDamage(skill, context, boardValue);
            int totalDamage = 0;
            int hitIndex = 0;
            float baseShakeAmount = 0.4f; // Base shake amount for attacks

            for (int i = 0; i < cycleCount; i++)
            {
                if (i < animationCount)
                {
                    context.SetAnimation?.Invoke(anim, false);
                    await SkillExecutor.WaitSeconds(skill.animationDuration, context);
                }

                if (i < hitCount)
                {
                    await SpawnProjectileHitAsync(skill, context);

                    int dealt = SkillExecutor.ApplyHitDamage(skill, context, hitBaseDamage);
                    totalDamage += Mathf.Max(0, dealt);

                    hitIndex++;
                    float shakeAmount = baseShakeAmount * hitIndex / Mathf.Max(1, hitCount);
                    CameraShakeController.Instance?.DoCameraShake(shakeAmount);
                }

                if (i < fxCount && skill.fxPrefab != null)
                    BattleFxUtility.SpawnAutoDestroy(skill.fxPrefab, context.TargetTransform.position, Quaternion.identity, skill.animationDuration + 0.5f);

                if (i < cycleCount - 1)
                    await SkillExecutor.WaitSeconds(skill.hitDelay, context);
            }

            SkillExecutor.ApplySkillStatusEffect(skill, context, hitBaseDamage);
            context.SetAnimation?.Invoke(context.IdleAnimation, true);
            return new SkillResult(true, totalDamage, hitCount);
        }
        finally
        {
            context.OnSkillEnd?.Invoke();
        }
    }

    private static async UniTask SpawnProjectileHitAsync(SkillData skill, SkillContext context)
    {
        // Use pet's custom bullet prefab if available, otherwise use skill's projectile
        GameObject prefab = context.ProjectilePrefabOverride != null ? context.ProjectilePrefabOverride : skill.projectilePrefab;
        if (prefab == null)
            return;

        Transform spawn = context.ProjectileSpawnPoint != null ? context.ProjectileSpawnPoint : context.AttackerTransform;
        GameObject projectile = Object.Instantiate(prefab, spawn.position, Quaternion.identity);

        float speed = Mathf.Max(6f, context.MoveSpeed * 0.8f);
        while (projectile != null && Vector3.Distance(projectile.transform.position, context.TargetTransform.position) > 0.05f)
        {
            projectile.transform.position = Vector3.MoveTowards(
                projectile.transform.position,
                context.TargetTransform.position,
                speed * Time.deltaTime);

            await UniTask.Yield(cancellationToken: context.CancellationToken);
        }

        if (projectile != null)
            Object.Destroy(projectile);
    }
}
