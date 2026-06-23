using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class HomeDemoCtaPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;

    [Header("Popup Buttons")]
    [SerializeField] private Button discordButton;
    [SerializeField] private Button steamButton;
    [SerializeField] private Button closeButton;

    [Header("Launcher Button")]
    [SerializeField] private Button ctaOpenButton;

    private const string HomeSceneName = "SceneHome";
    private static HomeDemoCtaPopupUI instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        UnitySceneManager.sceneLoaded -= OnSceneLoaded;
        UnitySceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.Equals(scene.name, HomeSceneName, System.StringComparison.Ordinal))
            return;

        if (instance == null)
            instance = FindObjectOfType<HomeDemoCtaPopupUI>(true);

        if (instance == null)
        {
            Debug.LogWarning("[HomeDemoCtaPopupUI] Popup controller not found in SceneHome.");
            return;
        }

        instance.Initialize();
        instance.StartCoroutine(instance.ShowIfNeededRoutine());
    }

    private void Awake()
    {
        if (instance == null)
            instance = this;

        Initialize();
    }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged += RefreshLauncherButtonVisibility;

        RefreshLauncherButtonVisibility();
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged -= RefreshLauncherButtonVisibility;
    }

    private void Initialize()
    {
        BindButtons();
        SetPopupVisible(false);
        RefreshLauncherButtonVisibility();
    }

    private IEnumerator ShowIfNeededRoutine()
    {
        yield return null;

        RefreshLauncherButtonVisibility();

        if (PlayerManager.Instance == null || !PlayerManager.Instance.HasPendingDemoCtaPopup())
            yield break;

        OpenPopup();
    }

    private void BindButtons()
    {
        if (discordButton != null)
        {
            discordButton.onClick.RemoveListener(OpenDiscordUrl);
            discordButton.onClick.AddListener(OpenDiscordUrl);
        }

        if (steamButton != null)
        {
            steamButton.onClick.RemoveListener(OpenSteamUrl);
            steamButton.onClick.AddListener(OpenSteamUrl);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePopup);
            closeButton.onClick.AddListener(ClosePopup);
        }

        if (ctaOpenButton != null)
        {
            ctaOpenButton.onClick.RemoveListener(OpenPopup);
            ctaOpenButton.onClick.AddListener(OpenPopup);
        }
    }

    private void RefreshLauncherButtonVisibility()
    {
        if (ctaOpenButton == null)
            return;

        bool visible = PlayerManager.Instance != null && PlayerManager.Instance.IsDemoCtaUnlocked;
        ctaOpenButton.gameObject.SetActive(visible);
    }

    public void OpenPopup()
    {
        SetPopupVisible(true);
        RefreshLauncherButtonVisibility();
    }

    public void ClosePopup()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.TryConsumePendingDemoCtaPopup();

        SetPopupVisible(false);
        RefreshLauncherButtonVisibility();
    }

    private void SetPopupVisible(bool visible)
    {
        GameObject target = popupRoot != null ? popupRoot : gameObject;
        if (target != null)
            target.SetActive(visible);
    }

    private void OpenDiscordUrl()
    {
        string url = PlayerManager.Instance != null ? PlayerManager.Instance.DemoDiscordUrl : string.Empty;
        if (!string.IsNullOrWhiteSpace(url))
            Application.OpenURL(url);
    }

    private void OpenSteamUrl()
    {
        string url = PlayerManager.Instance != null ? PlayerManager.Instance.DemoSteamUrl : string.Empty;
        if (!string.IsNullOrWhiteSpace(url))
            Application.OpenURL(url);
    }
}
