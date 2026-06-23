using UnityEngine;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Forces CanvasScaler to scale UI by screen size.
/// Apply once at startup; if resolution changes, user can restart game.
/// </summary>
public class UIAutoScaleSetup : MonoBehaviour
{
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
    [SerializeField] [Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<UIAutoScaleSetup>() != null)
            return;

        GameObject go = new GameObject("UIAutoScaleSetup");
        DontDestroyOnLoad(go);
        go.AddComponent<UIAutoScaleSetup>();
    }

    private void Awake()
    {
        ApplyCanvasScaling();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCanvasScaling();
    }

    private void ApplyCanvasScaling()
    {
        CanvasScaler[] scalers = FindObjectsOfType<CanvasScaler>(true);

        foreach (CanvasScaler scaler in scalers)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthOrHeight;
        }

        ;
    }
}
