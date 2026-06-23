using System.Collections;
using System.Collections;
using DG.Tweening;
using UnityEngine;

public class CloudSpawner : MonoBehaviour
{
    [Header("Cloud Prefabs")]
    [SerializeField] private GameObject[] cloudPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] private int minCloudPerWave = 1;
    [SerializeField] private int maxCloudPerWave = 5;
    [SerializeField] private float minSpawnInterval = 20f;
    [SerializeField] private float maxSpawnInterval = 50f;
    [SerializeField] private float cloudLifeTime = 5f; // th?i gian bay ? t?c ?? t?i ?a (hi?n t?i)

    [Header("Cloud Speed (Random)")]
    [SerializeField] private float minSpeedMultiplier = 0.2f; // ch?m t?i ?a 5 l?n
    [SerializeField] private float maxSpeedMultiplier = 1f;   // t?c ?? t?i ?a = t?c ?? hi?n t?i

    [Header("Spawn Timing In Wave")]
    [SerializeField] private float minDelayBetweenCloudInWave = 0f;
    [SerializeField] private float maxDelayBetweenCloudInWave = 1.2f;

    [Header("Vertical Range (Viewport Y)")]
    [SerializeField] private float minViewportY = 0.2f;
    [SerializeField] private float maxViewportY = 0.9f;

    [Header("Horizontal Offset")]
    [SerializeField] private float sideOffset = 2f;

    private Camera mainCam;

    private void Start()
    {
        mainCam = Camera.main;
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            int spawnCount = Random.Range(minCloudPerWave, maxCloudPerWave + 1);
            for (int i = 0; i < spawnCount; i++)
            {
                SpawnOneCloud();

                // M?i ?ám mây có th? spawn cùng lúc (delay=0) ho?c l?ch th?i gian
                if (i < spawnCount - 1)
                {
                    float delay = Random.Range(minDelayBetweenCloudInWave, maxDelayBetweenCloudInWave);
                    if (delay > 0f)
                        yield return new WaitForSeconds(delay);
                }
            }

            float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(wait);
        }
    }

    private void SpawnOneCloud()
    {
        if (cloudPrefabs == null || cloudPrefabs.Length == 0 || mainCam == null)
            return;

        GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Length)];
        if (prefab == null) return;

        bool fromLeft = Random.value > 0.5f;
        float y = Random.Range(minViewportY, maxViewportY);

        Vector3 leftEdge = mainCam.ViewportToWorldPoint(new Vector3(0f, y, Mathf.Abs(mainCam.transform.position.z)));
        Vector3 rightEdge = mainCam.ViewportToWorldPoint(new Vector3(1f, y, Mathf.Abs(mainCam.transform.position.z)));

        Vector3 startPos = fromLeft
            ? new Vector3(leftEdge.x - sideOffset, leftEdge.y, 0f)
            : new Vector3(rightEdge.x + sideOffset, rightEdge.y, 0f);

        Vector3 endPos = fromLeft
            ? new Vector3(rightEdge.x + sideOffset, rightEdge.y, 0f)
            : new Vector3(leftEdge.x - sideOffset, leftEdge.y, 0f);

        GameObject cloud = Instantiate(prefab, startPos, Quaternion.identity, transform);

        float speedMultiplier = Random.Range(minSpeedMultiplier, maxSpeedMultiplier);
        speedMultiplier = Mathf.Clamp(speedMultiplier, 0.2f, 1f);

        float travelDuration = cloudLifeTime / speedMultiplier;

        cloud.transform.DOMoveX(endPos.x, travelDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (cloud != null) Destroy(cloud);
            });

        Destroy(cloud, travelDuration + 0.05f);
    }
}
