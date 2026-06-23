using UnityEngine;
using System.Collections;

/// <summary>
/// Controls camera shake effects independently without external dependencies
/// </summary>
public class CameraShakeController : MonoBehaviour
{
    public static CameraShakeController Instance { get; private set; }

    private Camera mainCamera;
    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    [SerializeField] private float shakeSpeed = 0.1f; // How fast the shake oscillates

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            Debug.LogError("[CameraShakeController] No camera found!");
        else
            originalPosition = mainCamera.transform.localPosition;
    }

    /// <summary>
    /// Apply camera shake with given intensity (0-1 range)
    /// </summary>
    public void DoCameraShake(float intensity)
    {
        if (mainCamera == null)
            return;

        // Stop previous shake coroutine if still running
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity));
    }

    private IEnumerator ShakeCoroutine(float intensity)
    {
        if (mainCamera == null)
            yield break;

        float safeDuration = Mathf.Max(0.1f, intensity * 0.2f); // Longer shakes for higher intensity
        float safeIntensity = Mathf.Clamp01(intensity);
        float elapsed = 0f;

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / safeDuration;

            // Fade out effect
            float currentIntensity = safeIntensity * (1f - progress);
            
            // Random offset within intensity range
            float offsetX = Random.Range(-currentIntensity, currentIntensity);
            float offsetY = Random.Range(-currentIntensity, currentIntensity);

            mainCamera.transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0f);

            yield return null;
        }

        // Reset camera to original position
        if (mainCamera != null)
            mainCamera.transform.localPosition = originalPosition;

        shakeCoroutine = null;
    }

    private void OnDestroy()
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        if (Instance == this)
            Instance = null;
    }
}
