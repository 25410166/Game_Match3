using UnityEngine;

public static class BattleFxUtility
{
    public static GameObject SpawnAutoDestroy(GameObject prefab, Vector3 position, Quaternion rotation, float fallbackLifetime = 2f)
    {
        if (prefab == null)
            return null;

        GameObject instance = Object.Instantiate(prefab, position, rotation);
        float lifetime = Mathf.Max(0.1f, fallbackLifetime);
        lifetime = Mathf.Max(lifetime, CalculateAutoDestroyLifetime(instance));
        Object.Destroy(instance, lifetime);
        return instance;
    }

    private static float CalculateAutoDestroyLifetime(GameObject instance)
    {
        if (instance == null)
            return 0f;

        float lifetime = 0f;

        ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem system = particleSystems[i];
            if (system == null)
                continue;

            ParticleSystem.MainModule main = system.main;
            float systemLifetime = main.duration;
            if (!main.loop)
                systemLifetime += main.startLifetime.constantMax;

            lifetime = Mathf.Max(lifetime, systemLifetime);
        }

        Animator animator = instance.GetComponentInChildren<Animator>(true);
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip != null)
                    lifetime = Mathf.Max(lifetime, clip.length);
            }
        }

        return lifetime;
    }
}