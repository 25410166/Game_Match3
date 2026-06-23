using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class SkillContext
{
    public Transform AttackerTransform;
    public Transform TargetTransform;
    public Transform ProjectileSpawnPoint;

    public Func<int> GetBaseAttack;
    public Func<int> GetCurrentMana;
    public Action<int> SpendMana;
    public Func<int> GetCurrentRage;
    public Action<int> SpendRage;
    public Func<int> GetCurrentHp;
    public Func<int> GetMaxHp;
    public Action<int> SpendHp;
    public Func<float> GetCritRate;
    public Func<float> GetCritDamage;
    public Func<float> GetOutgoingDamageMultiplier;

    public Func<string> GetAttackerElement;
    public Func<string> GetTargetWeakness;
    public Func<int, int> ApplyDamage;
    public Action<StatusEffectType, int, float, bool> ApplyTargetStatusEffect;

    public Action<string, bool> SetAnimation;
    public Action<float> FlipTowards;
    public bool ShouldFlip;
    public float MoveSpeed = 18f;
    public float MeleeOffsetX = 0f;

    public string IdleAnimation = "Idle";
    public string WalkAnimation = "Walk";
    public string MeleeAnimation = "Attack";
    public string RangedAnimation = "Attack";

    public GameObject DamagePopupPrefab;
    public GameObject HitFxPrefab;
    public AudioSource HitAudioSource;
    public AudioClip HitSfx;
    public Board Board;

    public GameObject ProjectilePrefabOverride;
    public float MeleeAttackMoveX = -1.2f;

    public Action OnSkillStart;
    public Action OnSkillEnd;
    public bool IsPlayerControlled;
    public Func<SkillData, UniTask<AuditionMiniGameResult>> PlayAuditionMiniGame;

    public CancellationToken CancellationToken;
}
