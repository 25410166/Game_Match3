using Cysharp.Threading.Tasks;
using UnityEngine;

public static class MeleeSkill
{
    public static async UniTask<SkillResult> ExecuteAsync(SkillData skill, SkillContext context, int boardValue)
    {
        context.OnSkillStart?.Invoke();

        Vector3 startPosition = context.AttackerTransform.position;
        // Use MeleeAttackMoveX if set, otherwise fall back to MeleeOffsetX
        float moveOffsetX = context.MeleeAttackMoveX != 0f ? context.MeleeAttackMoveX : context.MeleeOffsetX;
        Vector3 targetPosition = context.TargetTransform.position + new Vector3(moveOffsetX, 0f, 0f);

        try
        {
            if (context.ShouldFlip)
                context.FlipTowards?.Invoke(targetPosition.x);

            context.SetAnimation?.Invoke(context.WalkAnimation, true);
            await MoveToAsync(context.AttackerTransform, targetPosition, context.MoveSpeed, context);

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
                    context.SetAnimation?.Invoke(context.MeleeAnimation, false);

                    if (i < fxCount && skill.fxPrefab != null)
                        BattleFxUtility.SpawnAutoDestroy(skill.fxPrefab, context.TargetTransform.position, Quaternion.identity, skill.animationDuration + 0.5f);

                    await SkillExecutor.WaitSeconds(skill.animationDuration, context);
                }
                else if (i < fxCount && skill.fxPrefab != null)
                {
                    BattleFxUtility.SpawnAutoDestroy(skill.fxPrefab, context.TargetTransform.position, Quaternion.identity, skill.animationDuration + 0.5f);
                }

                if (i < hitCount)
                {
                    int dealt = SkillExecutor.ApplyHitDamage(skill, context, hitBaseDamage);
                    totalDamage += Mathf.Max(0, dealt);

                    hitIndex++;
                    float shakeAmount = baseShakeAmount * hitIndex / Mathf.Max(1, hitCount);
                    CameraShakeController.Instance?.DoCameraShake(shakeAmount);
                }

                if (i < cycleCount - 1)
                    await SkillExecutor.WaitSeconds(skill.hitDelay, context);
            }

            if (context.ShouldFlip)
                context.FlipTowards?.Invoke(startPosition.x);

            context.SetAnimation?.Invoke(context.WalkAnimation, true);
            await MoveToAsync(context.AttackerTransform, startPosition, context.MoveSpeed, context);

            if (context.ShouldFlip)
                context.FlipTowards?.Invoke(context.TargetTransform.position.x);

            SkillExecutor.ApplySkillStatusEffect(skill, context, hitBaseDamage);
            context.SetAnimation?.Invoke(context.IdleAnimation, true);
            return new SkillResult(true, totalDamage, hitCount);
        }
        finally
        {
            context.OnSkillEnd?.Invoke();
        }
    }

    private static async UniTask MoveToAsync(Transform mover, Vector3 target, float speed, SkillContext context)
    {
        float safeSpeed = Mathf.Max(0.01f, speed);

        while (Vector3.Distance(mover.position, target) > 0.01f)
        {
            mover.position = Vector3.MoveTowards(mover.position, target, safeSpeed * Time.deltaTime);
            await UniTask.Yield(cancellationToken: context.CancellationToken);
        }

        mover.position = target;
    }
}
