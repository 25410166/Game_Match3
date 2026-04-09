using UnityEngine;
using Spine.Unity;

public class PetBehaviour : MonoBehaviour
{
    [Header("Spine Reference")]
    public SkeletonAnimation skeletonAnimation;

    private string currentAnim;
    private PetDataAsset petData;

    public void Init(PetDataAsset data)
    {
        petData = data;

        if (petData == null)
        {
            Debug.LogError("PetBehaviour: PetDataAsset null");
            return;
        }

        // Khi spawn thì set Idle
        PlayAnimation(petData.idleAnim, true);
    }

    public void OnGemEffect(int itemId)
    {
        if (skeletonAnimation == null || petData == null) return;

        switch (itemId)
        {
            case 0:
            case 1:
            case 2:
            case 3:
                PlayAnimation(petData.idleAnim, true);
                break;

            case 4:
                string attackAnim = petData.attackType == AttackType.Melee ?
                                    petData.attackMeleeAnim :
                                    petData.attackRangedAnim;

                PlayAnimation(attackAnim, false);
                skeletonAnimation.state.AddAnimation(0, petData.idleAnim, true, 0.5f);
                break;
        }
    }

    public void OnOwnerDead()
    {
        if (petData != null)
            PlayAnimation(petData.deadAnim, false);
    }

    private void PlayAnimation(string anim, bool loop)
    {
        if (skeletonAnimation == null || string.IsNullOrEmpty(anim)) return;
        if (currentAnim == anim) return;

        skeletonAnimation.state.SetAnimation(0, anim, loop);
        currentAnim = anim;
    }
}
