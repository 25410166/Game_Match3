using System;
using System;
using UnityEngine;
using Spine.Unity;

/// <summary>
/// Event-driven system for pet attacks.
/// Allows decoupling of attack logic from presentation (animation, effects, damage).
/// </summary>
public class PetAttackController
{
    // Events for attack sequence
    public event Action OnAttackStart;      // Attack begins
    public event Action OnWalkStart;        // Pet starts walking to target
    public event Action OnWalkEnd;          // Pet reached target position
    public event Action OnAnimationStart;   // Attack animation starts
    public event Action OnAnimationEnd;     // Attack animation ends
    public event Action OnDamageDealt;      // Damage dealt to opponent
    public event Action OnReturnStart;      // Pet starts walking back
    public event Action OnReturnEnd;        // Pet returned to original position
    public event Action OnAttackEnd;        // Attack complete

    private Transform petTransform;
    private SkeletonAnimation skeletonAnim;
    private float moveSpeed;
    private int baseAttack;
    private AttackType attackType;
    private Vector3 originalPos;
    private Vector3 targetPos;
    private string walkAnim;
    private string attackMeleeAnim;
    private string attackRangedAnim;
    private string idleAnim;
    private Action<int> onDealDamage; // Callback to deal damage

    public PetAttackController(
        Transform petTransform,
        SkeletonAnimation skeletonAnim,
        float moveSpeed,
        int baseAttack,
        AttackType attackType,
        string walkAnim,
        string attackMeleeAnim,
        string attackRangedAnim,
        string idleAnim,
        Action<int> onDealDamage
    )
    {
        this.petTransform = petTransform;
        this.skeletonAnim = skeletonAnim;
        this.moveSpeed = moveSpeed;
        this.baseAttack = baseAttack;
        this.attackType = attackType;
        this.walkAnim = walkAnim;
        this.attackMeleeAnim = attackMeleeAnim;
        this.attackRangedAnim = attackRangedAnim;
        this.idleAnim = idleAnim;
        this.onDealDamage = onDealDamage;
        this.originalPos = petTransform.position;
    }

    public void SetTargetPosition(Vector3 target)
    {
        this.targetPos = target;
    }

    public void ExecuteMeleeAttack(MonoBehaviour coroutineRunner)
    {
        coroutineRunner.StartCoroutine(MeleeAttackRoutine());
    }

    public void ExecuteRangedAttack(MonoBehaviour coroutineRunner)
    {
        coroutineRunner.StartCoroutine(RangedAttackRoutine());
    }

    private System.Collections.IEnumerator MeleeAttackRoutine()
    {
        OnAttackStart?.Invoke();

        // Walk to target
        OnWalkStart?.Invoke();
        yield return MoveToTarget(targetPos);
        OnWalkEnd?.Invoke();

        // Attack animation
        OnAnimationStart?.Invoke();
        SetAnimation(attackMeleeAnim, false);
        yield return new WaitForSeconds(1.2f);
        OnAnimationEnd?.Invoke();

        // Deal damage
        OnDamageDealt?.Invoke();
        onDealDamage?.Invoke(baseAttack);

        // Walk back
        OnReturnStart?.Invoke();
        yield return MoveToTarget(originalPos);
        OnReturnEnd?.Invoke();

        // Reset to idle
        SetAnimation(idleAnim, true);
        OnAttackEnd?.Invoke();
    }

    private System.Collections.IEnumerator RangedAttackRoutine()
    {
        OnAttackStart?.Invoke();

        // Ranged: no walk, just attack animation
        OnAnimationStart?.Invoke();
        SetAnimation(attackRangedAnim, false);
        yield return new WaitForSeconds(1.2f);
        OnAnimationEnd?.Invoke();

        // Deal damage
        OnDamageDealt?.Invoke();
        onDealDamage?.Invoke(baseAttack);

        // Reset to idle
        SetAnimation(idleAnim, true);
        OnAttackEnd?.Invoke();
    }

    private System.Collections.IEnumerator MoveToTarget(Vector3 target)
    {
        while (Vector3.Distance(petTransform.position, target) > 0.01f)
        {
            petTransform.position = Vector3.MoveTowards(petTransform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        petTransform.position = target; // Ensure exact position
    }

    private void SetAnimation(string name, bool loop)
    {
        if (skeletonAnim != null && !string.IsNullOrEmpty(name))
        {
            skeletonAnim.state.SetAnimation(0, name, loop);
        }
    }
}
