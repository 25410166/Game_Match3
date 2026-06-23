using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSceneManager : MonoBehaviour
{
    public static GameSceneManager Instance { get; private set; }

    private const string SCENE_HOME = "SceneHome";
    private const string SCENE_BATTLE = "SceneBattle";
    private const string SCENE_MAP = "SceneMap";
    private const string SCENE_SHOP = "SceneShop";
    private const string SCENE_UPDATE = "SceneUpdate";

    [Header("Loading UI")]
    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private Slider loadingBar;
    [SerializeField] private TextMeshProUGUI loadingPercentText;
    [SerializeField] private bool showLoadingAtGameStart = true;
    [SerializeField] private float startupLoadingDuration = 1f;

    private bool isLoading;
    private Canvas loadingCanvas;
    private const int CanvasCameraResolveFrames = 20;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Ensure loading canvas is always on top and doesn't conflict with scene canvases
            loadingCanvas = GetComponent<Canvas>();
            if (loadingCanvas != null)
            {
                loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                loadingCanvas.sortingOrder = 9999; // Always on top
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        SetLoadingVisible(false);
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Ensure loading UI is hidden and reset after any scene load
        SetLoadingVisible(false);
        SetProgress(0f);
        isLoading = false;

        CleanupExtraAudioListeners();
        StartCoroutine(EnsureSceneCanvasCameras(scene));
    }

    private void Start()
    {
        if (showLoadingAtGameStart)
            StartCoroutine(StartupLoadingRoutine());
    }

    public void LoadHome() { BeginLoadScene(SCENE_HOME); }
    public void LoadBattle() { BeginLoadScene(SCENE_BATTLE); }
    public void LoadMap() { BeginLoadScene(SCENE_MAP); }
    public void LoadShop() { BeginLoadScene(SCENE_SHOP); }

    public void LoadUpdatePetWithId(int id)
    {
        PlayerPrefs.SetInt("UpdatePetId", id);
        PlayerPrefs.Save();
        BeginLoadScene(SCENE_UPDATE);
    }

    private void BeginLoadScene(string sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator StartupLoadingRoutine()
    {
        if (loadingRoot == null)
            yield break;

        SetLoadingVisible(true);

        float duration = Mathf.Max(0.1f, startupLoadingDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            SetProgress(p);
            yield return null;
        }

        SetProgress(1f);
        yield return new WaitForSeconds(0.05f);
        SetLoadingVisible(false);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;
        SetLoadingVisible(true);
        SetProgress(0f);

        AsyncOperation operation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            isLoading = false;
            SetLoadingVisible(false);
            yield break;
        }

        operation.allowSceneActivation = false;

        while (operation.progress < 0.9f)
        {
            float p = Mathf.Clamp01(operation.progress / 0.9f);
            SetProgress(p);
            yield return null;
        }

        SetProgress(1f);
        yield return new WaitForSeconds(0.1f);

        operation.allowSceneActivation = true;

        while (!operation.isDone)
            yield return null;

        // After scene activation, cleanup extra audio listeners
        yield return new WaitForEndOfFrame();
        CleanupExtraAudioListeners();

        // Hide loading UI after scene is fully loaded
        SetLoadingVisible(false);
        isLoading = false;
    }

    private IEnumerator EnsureSceneCanvasCameras(Scene scene)
    {
        for (int i = 0; i < CanvasCameraResolveFrames; i++)
        {
            FixSceneCanvasCameras(scene);
            yield return null;
        }
    }

    private void FixSceneCanvasCameras(Scene scene)
    {
        Camera sceneCamera = GetSceneCamera(scene);
        if (sceneCamera == null)
            return;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || canvas == loadingCanvas)
                continue;

            if (canvas.renderMode != RenderMode.ScreenSpaceCamera)
                continue;

            if (canvas.worldCamera == null || canvas.worldCamera.gameObject.scene != scene)
                canvas.worldCamera = sceneCamera;
        }
    }

    private Camera GetSceneCamera(Scene scene)
    {
        Camera mainCam = Camera.main;
        Camera[] cameras = FindObjectsOfType<Camera>(true);

        if (mainCam != null && mainCam.gameObject.scene == scene)
            return mainCam;
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam != null && cam.enabled && cam.gameObject.scene == scene)
                return cam;
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam != null && cam.gameObject.scene == scene)
                return cam;
        }

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam != null && cam.name == "Main Camera")
                return cam;
        }

        if (mainCam != null)
            return mainCam;

        for (int i = 0; i < cameras.Length; i++)
        {
            Camera cam = cameras[i];
            if (cam != null && cam.enabled)
                return cam;
        }

        return cameras.Length > 0 ? cameras[0] : null;
    }

    /// <summary>
    /// Ensures only one AudioListener exists in the scene to prevent warning messages.
    /// Keeps the listener on the MainCamera when possible and disables any extras.
    /// </summary>
    private void CleanupExtraAudioListeners()
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length <= 1)
            return;

        AudioListener preferred = null;
        Camera mainCam = Camera.main;
        if (mainCam != null)
            preferred = mainCam.GetComponent<AudioListener>();

        if (preferred == null)
            preferred = listeners[0];

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener listener = listeners[i];
            if (listener == null || listener == preferred)
                continue;

            listener.enabled = false;
        }
    }

    private void SetLoadingVisible(bool visible)
    {
        if (loadingRoot != null)
            loadingRoot.SetActive(visible);
    }

    private void SetProgress(float progress)
    {
        float p = Mathf.Clamp01(progress);

        if (loadingBar != null)
            loadingBar.value = p;

        if (loadingPercentText != null)
            loadingPercentText.text = Mathf.RoundToInt(p * 100f) + "%";
    }
}
