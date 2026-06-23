using System.Collections;
using DG.Tweening;
using UnityEngine;

public class MeteorShowerFx : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject meteorPrefab;
    [SerializeField] private GameObject explosionPrefab;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset;

    [Header("Start Area")]
    [SerializeField] private Transform startHeightAnchor;
    [SerializeField] private float startHeight = 8f;
    [SerializeField] private Vector2 startHeightOffsetRange = new Vector2(-0.5f, 0.5f);
    [SerializeField] private Vector2 startRadiusRange = new Vector2(2f, 5f);

    [Header("Count")]
    [SerializeField] private int minMeteorCount = 2;
    [SerializeField] private int maxMeteorCount = 3;

    [Header("Movement")]
    [SerializeField] private float fallSpeed = 12f;
    [SerializeField] private Vector2 fallDurationRange = new Vector2(0.6f, 1.2f);
    [SerializeField] private AnimationCurve fallCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private bool orientAlongPath = true;
    [SerializeField] private bool useDurationRange = true;

    [Header("Impact")]
    [SerializeField] private bool autoAdjustImpactPoint = true;
    [SerializeField] private float impactPadding = 0.05f;

    [Header("Lifecycle")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool autoDestroyRoot = true;
    [SerializeField] private float rootDestroyDelay = 0.2f;
    [SerializeField] private float explosionFallbackLifetime = 2f;

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
        int count = Mathf.Clamp(Random.Range(minMeteorCount, maxMeteorCount + 1), 1, 100);
        Vector3 impactTarget = GetTargetPosition();
        float longestDuration = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector3 startPos = BuildStartPosition(impactTarget);
            float duration = GetFallDuration(startPos, impactTarget);
            longestDuration = Mathf.Max(longestDuration, duration);
            SpawnMeteor(startPos, impactTarget, duration);
        }

        if (autoDestroyRoot)
        {
            float total = Mathf.Max(0f, longestDuration + explosionFallbackLifetime + rootDestroyDelay);
            if (total > 0f)
                yield return new WaitForSeconds(total);

            Destroy(gameObject);
        }

        playCoroutine = null;
    }

    private Vector3 GetTargetPosition()
    {
        Transform resolvedTarget = target != null ? target : transform;
        return resolvedTarget.position + targetOffset;
    }

    private void SpawnMeteor(Vector3 startPos, Vector3 impactTarget, float duration)
    {
        if (meteorPrefab == null)
            return;

        GameObject meteor = Instantiate(meteorPrefab, startPos, Quaternion.identity, transform);
        Vector3 destination = GetMeteorDestination(meteor, startPos, impactTarget);
        FallWithTween(meteor, destination, impactTarget, duration);
    }

    private float GetFallDuration(Vector3 startPos, Vector3 targetPos)
    {
        if (useDurationRange)
        {
            float minDuration = Mathf.Max(0.01f, fallDurationRange.x);
            float maxDuration = Mathf.Max(minDuration, fallDurationRange.y);
            return Random.Range(minDuration, maxDuration);
        }

        float distance = Vector3.Distance(startPos, targetPos);
        float speed = Mathf.Max(0.01f, fallSpeed);
        return Mathf.Max(0.01f, distance / speed);
    }

    private Vector3 BuildStartPosition(Vector3 targetPos)
    {
        Vector3 anchorPos = startHeightAnchor != null
            ? startHeightAnchor.position
            : transform.position + Vector3.up * startHeight;

        Vector2 randomOffset = Random.insideUnitCircle;
        if (randomOffset.sqrMagnitude > 0.0001f)
            randomOffset = randomOffset.normalized * Random.Range(startRadiusRange.x, startRadiusRange.y);

        float randomHeight = Random.Range(startHeightOffsetRange.x, startHeightOffsetRange.y);

        return new Vector3(
            anchorPos.x + randomOffset.x,
            anchorPos.y + randomHeight,
            targetPos.z);
    }

    private void FallWithTween(GameObject meteor, Vector3 destination, Vector3 impactTarget, float duration)
    {
        if (meteor == null)
            return;

        Vector3 travelDirection = (impactTarget - meteor.transform.position).normalized;
        if (orientAlongPath && travelDirection.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg;
            meteor.transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
        }

        meteor.transform.DOMove(destination, duration)
            .SetEase(fallCurve)
            .OnComplete(() =>
            {
                if (meteor != null)
                    Destroy(meteor);

                SpawnExplosion(impactTarget);
            });
    }

    private void SpawnExplosion(Vector3 impactTarget)
    {
        if (explosionPrefab == null)
            return;

        GameObject explosion = Instantiate(explosionPrefab, impactTarget, Quaternion.identity, transform);
        if (explosionFallbackLifetime > 0f)
            Destroy(explosion, explosionFallbackLifetime);
    }

    private Vector3 GetMeteorDestination(GameObject meteor, Vector3 startPos, Vector3 impactTarget)
    {
        if (!autoAdjustImpactPoint || meteor == null)
            return impactTarget;

        Vector3 direction = (impactTarget - startPos).normalized;
        if (direction.sqrMagnitude <= 0.0001f)
            return impactTarget;

        Bounds combinedBounds;
        if (!TryGetCombinedBounds(meteor, out combinedBounds))
            return impactTarget;

        Vector3 localDirection = meteor.transform.InverseTransformDirection(direction).normalized;
        Vector3 extents = combinedBounds.extents;
        Vector3 center = combinedBounds.center;

        float projectedHalfSize =
            Mathf.Abs(localDirection.x) * extents.x +
            Mathf.Abs(localDirection.y) * extents.y +
            Mathf.Abs(localDirection.z) * extents.z;

        float centerOffset = Vector3.Dot(center, localDirection);
        float forwardExtent = centerOffset + projectedHalfSize;
        float offset = Mathf.Max(0f, forwardExtent + impactPadding);

        return impactTarget - direction * offset;
    }

    private bool TryGetCombinedBounds(GameObject meteor, out Bounds combinedBounds)
    {
        Renderer[] renderers = meteor.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            combinedBounds = new Bounds();
            return false;
        }

        Matrix4x4 worldToLocal = meteor.transform.worldToLocalMatrix;
        combinedBounds = TransformBounds(worldToLocal, renderers[0].bounds);

        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(TransformBounds(worldToLocal, renderers[i].bounds));

        return true;
    }

    private static Bounds TransformBounds(Matrix4x4 matrix, Bounds worldBounds)
    {
        Vector3 center = matrix.MultiplyPoint3x4(worldBounds.center);
        Vector3 extents = worldBounds.extents;

        Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
        Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
        Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));

        Vector3 transformedExtents = new Vector3(
            Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
            Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
            Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));

        return new Bounds(center, transformedExtents * 2f);
    }
}
