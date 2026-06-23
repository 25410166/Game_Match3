using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FxSequencePlayer : MonoBehaviour
{
    [Header("Sequence")]
    [SerializeField] private List<GameObject> fxPrefabs = new List<GameObject>();
    [SerializeField] private float delayBetweenFx = 0.25f;

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Vector3 spawnOffset;
    [SerializeField] private bool parentToSpawnPoint = true;

    [Header("Lifecycle")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool autoDestroyRoot = true;
    [SerializeField] private float fallbackFxLifetime = 2f;
    [SerializeField] private float rootDestroyDelay = 0.1f;

    private Coroutine playCoroutine;

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    public void Play()
    {
        if (playCoroutine != null)
            StopCoroutine(playCoroutine);

        playCoroutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        Transform resolvedSpawn = spawnPoint != null ? spawnPoint : transform;
        Vector3 basePosition = resolvedSpawn.position + spawnOffset;
        float totalDelay = 0f;

        for (int i = 0; i < fxPrefabs.Count; i++)
        {
            GameObject prefab = fxPrefabs[i];
            if (prefab != null)
            {
                Transform parent = parentToSpawnPoint ? resolvedSpawn : null;
                GameObject instance = Instantiate(prefab, basePosition, Quaternion.identity, parent);
                if (fallbackFxLifetime > 0f)
                    Destroy(instance, fallbackFxLifetime);
            }

            if (i < fxPrefabs.Count - 1)
            {
                float wait = Mathf.Max(0f, delayBetweenFx);
                totalDelay += wait;
                if (wait > 0f)
                    yield return new WaitForSeconds(wait);
            }
        }

        if (autoDestroyRoot)
        {
            float total = Mathf.Max(0f, totalDelay + fallbackFxLifetime + rootDestroyDelay);
            if (total > 0f)
                yield return new WaitForSeconds(total);

            Destroy(gameObject);
        }

        playCoroutine = null;
    }
}
