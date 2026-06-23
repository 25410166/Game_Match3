using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class PopupSettingUI : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown resolutionTmpDropdown;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundSlider;

    [Header("Localization")]
    [SerializeField] private Dropdown languageDropdown;
    [SerializeField] private TMP_Dropdown languageTmpDropdown;

    [Header("Actions")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI versionText;
    [SerializeField] private Button openSettingsButton;
    [SerializeField] private Button surrenderButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private Canvas rootCanvas;

    private bool isBinding;
    private ShortLayer popupShortLayer;

    private void Awake()
    {
        SetupResolutionOptions();
        SetupLanguageOptions();
        RefreshVersionText();

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (openSettingsButton != null)
            openSettingsButton.onClick.AddListener(Open);

        popupShortLayer = GetPopupShortLayer();
    }

    private void OnEnable()
    {
        BindUIEvents(true);
        RefreshFromSettings();
        RefreshVersionText();
        ApplyPopupShortLayer();
        SyncDropdownPopupSorting();
    }

    private void LateUpdate()
    {
        GameObject targetRoot = rootPanel != null ? rootPanel : gameObject;
        if (targetRoot != null && targetRoot.activeInHierarchy)
            SyncDropdownPopupSorting();
    }

    private void OnDisable()
    {
        BindUIEvents(false);
    }

    private void SetupResolutionOptions()
    {
        if (resolutionTmpDropdown == null && resolutionDropdown == null)
            return;

        string[] options = SettingsBridge.GetResolutionOptions();
        if (options == null || options.Length == 0)
        {
            options = new[] { "1920 x 1080", "1600 x 900", "1280 x 720", "960 x 540", "800 x 450", "640 x 360" };
        }

        if (resolutionTmpDropdown != null)
        {
            List<TMP_Dropdown.OptionData> tmpOptions = new List<TMP_Dropdown.OptionData>(options.Length);
            for (int i = 0; i < options.Length; i++)
            {
                tmpOptions.Add(new TMP_Dropdown.OptionData(options[i]));
            }

            resolutionTmpDropdown.ClearOptions();
            resolutionTmpDropdown.AddOptions(tmpOptions);
        }
        else
        {
            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(new List<string>(options));
        }
    }

    private void SetupLanguageOptions()
    {
        if (languageTmpDropdown == null && languageDropdown == null)
            return;

        List<string> options = new List<string> { "English", "Japanese", "Korean", "Chinese", "Vietnamese" };

        if (languageTmpDropdown != null)
        {
            List<TMP_Dropdown.OptionData> tmpOptions = new List<TMP_Dropdown.OptionData>(options.Count);
            for (int i = 0; i < options.Count; i++)
            {
                tmpOptions.Add(new TMP_Dropdown.OptionData(options[i]));
            }

            languageTmpDropdown.ClearOptions();
            languageTmpDropdown.AddOptions(tmpOptions);
        }
        else
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(options);
        }
    }

    private void BindUIEvents(bool bind)
    {
        if (isBinding)
            return;

        isBinding = true;

        if (bind)
        {
            if (resolutionTmpDropdown != null)
                resolutionTmpDropdown.onValueChanged.AddListener(OnResolutionChanged);
            else if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (musicSlider != null)
                musicSlider.onValueChanged.AddListener(OnMusicChanged);
            if (soundSlider != null)
                soundSlider.onValueChanged.AddListener(OnSoundChanged);
            if (languageTmpDropdown != null)
                languageTmpDropdown.onValueChanged.AddListener(OnLanguageChanged);
            else if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            if (surrenderButton != null)
                surrenderButton.onClick.AddListener(OnSurrenderClicked);
            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);
        }
        else
        {
            if (resolutionTmpDropdown != null)
                resolutionTmpDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            else if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            if (soundSlider != null)
                soundSlider.onValueChanged.RemoveListener(OnSoundChanged);
            if (languageTmpDropdown != null)
                languageTmpDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
            else if (languageDropdown != null)
                languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
            if (surrenderButton != null)
                surrenderButton.onClick.RemoveListener(OnSurrenderClicked);
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);
            if (exitButton != null)
                exitButton.onClick.RemoveListener(OnExitClicked);
        }

        isBinding = false;
    }

    public void RefreshFromSettings()
    {
        if (resolutionTmpDropdown != null)
            resolutionTmpDropdown.SetValueWithoutNotify(SettingsBridge.GetInt("ResolutionIndex", 0));
        else if (resolutionDropdown != null)
            resolutionDropdown.SetValueWithoutNotify(SettingsBridge.GetInt("ResolutionIndex", 0));
        if (fullscreenToggle != null)
            fullscreenToggle.SetIsOnWithoutNotify(SettingsBridge.GetBool("IsFullscreen", false));
        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(SettingsBridge.GetFloat("MusicVolume", 1f));
        if (soundSlider != null)
            soundSlider.SetValueWithoutNotify(SettingsBridge.GetFloat("SoundVolume", 1f));
        if (languageTmpDropdown != null)
            languageTmpDropdown.SetValueWithoutNotify(SettingsBridge.GetInt("CurrentLanguage", 0));
        else if (languageDropdown != null)
            languageDropdown.SetValueWithoutNotify(SettingsBridge.GetInt("CurrentLanguage", 0));
    }

    private void OnResolutionChanged(int index)
    {
        SettingsBridge.SetResolutionByIndex(index);
    }

    private void OnFullscreenChanged(bool value)
    {
        SettingsBridge.SetBool("IsFullscreen", value);
    }

    private void OnMusicChanged(float value)
    {
        SettingsBridge.SetFloat("MusicVolume", value);
    }

    private void OnSoundChanged(float value)
    {
        SettingsBridge.SetFloat("SoundVolume", value);
    }

    private void OnLanguageChanged(int index)
    {
        SettingsBridge.SetInt("CurrentLanguage", Mathf.Clamp(index, 0, 4));
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.NotifyLanguageChanged();
        RefreshVersionText();
    }

    private void RefreshVersionText()
    {
        if (versionText == null)
            return;

        versionText.text = "ver " + Application.version;
    }

    public void Close()
    {
        SettingsBridge.TryPlayButtonClickSound();

        if (rootPanel != null)
            rootPanel.SetActive(false);
        else
            gameObject.SetActive(false);
        SetPaused(false);
    }

    public void Open()
    {
        SettingsBridge.TryPlayButtonClickSound();

        if (rootPanel != null)
            rootPanel.SetActive(true);
        else
            gameObject.SetActive(true);
        ApplyPopupShortLayer();
        SyncDropdownPopupSorting();
        SetPaused(true);
    }

    private ShortLayer GetPopupShortLayer()
    {
        GameObject targetRoot = rootPanel != null ? rootPanel : gameObject;
        if (targetRoot == null)
            return null;

        ShortLayer shortLayer = targetRoot.GetComponent<ShortLayer>();
        if (shortLayer == null)
            shortLayer = targetRoot.GetComponentInParent<ShortLayer>(true);

        return shortLayer;
    }

    private void ApplyPopupShortLayer()
    {
        if (popupShortLayer == null)
            popupShortLayer = GetPopupShortLayer();

        popupShortLayer?.Apply();
    }

    private void SyncDropdownPopupSorting()
    {
        ApplyPopupShortLayer();
        SyncDropdownPopupSorting(resolutionTmpDropdown);
        SyncDropdownPopupSorting(languageTmpDropdown);
    }

    private void SyncDropdownPopupSorting(TMP_Dropdown dropdown)
    {
        if (dropdown == null)
            return;

        Canvas dropdownCanvas = dropdown.GetComponentInChildren<Canvas>(true);
        if (dropdownCanvas != null)
            ApplySortingToCanvas(dropdownCanvas);

        Transform template = dropdown.template;
        if (template != null)
        {
            Canvas templateCanvas = template.GetComponent<Canvas>();
            if (templateCanvas == null)
                templateCanvas = template.gameObject.AddComponent<Canvas>();

            ApplySortingToCanvas(templateCanvas);

            if (template.GetComponent<GraphicRaycaster>() == null)
                template.gameObject.AddComponent<GraphicRaycaster>();
        }

        Transform root = dropdown.transform.root;
        if (root == null)
            return;

        Canvas[] rootCanvases = root.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < rootCanvases.Length; i++)
        {
            Canvas canvas = rootCanvases[i];
            if (canvas == null)
                continue;

            if (canvas.name.IndexOf("Dropdown List", StringComparison.OrdinalIgnoreCase) >= 0
                || canvas.name.IndexOf("Blocker", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                ApplySortingToCanvas(canvas);
            }
        }
    }

    private void ApplySortingToCanvas(Canvas canvas)
    {
        if (canvas == null)
            return;

        if (popupShortLayer == null)
            popupShortLayer = GetPopupShortLayer();

        if (popupShortLayer == null)
            return;

        int sortingLayerId = SortingLayer.NameToID(popupShortLayer.SortingLayerName);
        int sortingOrder = popupShortLayer.SortingOrder;
        string canvasName = canvas.name ?? string.Empty;

        if (canvasName.IndexOf("Dropdown List", StringComparison.OrdinalIgnoreCase) >= 0)
            sortingOrder += 10;
        else if (canvasName.IndexOf("Blocker", StringComparison.OrdinalIgnoreCase) >= 0)
            sortingOrder += 5;
        else if (canvasName.IndexOf("Template", StringComparison.OrdinalIgnoreCase) >= 0)
            sortingOrder += 10;
        canvas.overrideSorting = true;
        if (sortingLayerId != 0 || popupShortLayer.SortingLayerName == "Default")
            canvas.sortingLayerID = sortingLayerId;
        canvas.sortingOrder = sortingOrder;
    }
    private void BringToFront()
    {
        Transform target = rootPanel != null ? rootPanel.transform : transform;
        if (target != null)
            target.SetAsLastSibling();
    }

    private void OnSurrenderClicked()
    {
        SettingsBridge.TryPlayButtonClickSound();
        SetPaused(false);
        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadHome();
        else
            Debug.LogWarning("[PopupSettingUI] GameSceneManager.Instance not found to load home.");
    }

    private void OnContinueClicked()
    {
        SettingsBridge.TryPlayButtonClickSound();
        Close();
    }

    private void OnExitClicked()
    {
        SettingsBridge.TryPlayButtonClickSound();
        SetPaused(false);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetPaused(bool paused)
    {
        Time.timeScale = paused ? 0f : 1f;
    }

    private static class SettingsBridge
    {
        private static readonly Type SettingsType = FindType("SettingsManager");
        private static readonly object SettingsInstance = ResolveInstance();

        public static string[] GetResolutionOptions()
        {
            object value = Call("GetResolutionOptions");
            return value as string[];
        }

        public static void SetResolutionByIndex(int index)
        {
            if (SettingsInstance != null)
            {
                Call("SetResolutionByIndex", index);
                return;
            }

            PlayerPrefs.SetInt("settings_resolution_index", index);
            PlayerPrefs.Save();
        }

        public static float GetFloat(string propertyName, float fallback)
        {
            if (SettingsInstance != null && TryGetProperty(propertyName, out object value))
                return Convert.ToSingle(value);

            switch (propertyName)
            {
                case "MusicVolume": return PlayerPrefs.GetFloat("settings_music_volume", fallback);
                case "SoundVolume": return PlayerPrefs.GetFloat("settings_sound_volume", fallback);
                default: return fallback;
            }
        }

        public static void TryPlayButtonClickSound()
        {
            Type audioType = FindType("AudioManager");
            if (audioType == null)
                return;

            PropertyInfo instanceProp = audioType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            object audioInstance = instanceProp != null ? instanceProp.GetValue(null, null) : null;
            if (audioInstance == null)
                return;

            MethodInfo method = audioType.GetMethod("PlayButtonClickSound", BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
                method.Invoke(audioInstance, null);
        }

        public static int GetInt(string propertyName, int fallback)
        {
            if (SettingsInstance != null && TryGetProperty(propertyName, out object value))
                return Convert.ToInt32(value);

            switch (propertyName)
            {
                case "ResolutionIndex": return PlayerPrefs.GetInt("settings_resolution_index", fallback);
                case "CurrentLanguage": return PlayerPrefs.GetInt("settings_language", fallback);
                default: return fallback;
            }
        }

        public static bool GetBool(string propertyName, bool fallback)
        {
            if (SettingsInstance != null && TryGetProperty(propertyName, out object value))
                return Convert.ToBoolean(value);

            switch (propertyName)
            {
                case "IsFullscreen": return PlayerPrefs.GetInt("settings_fullscreen", fallback ? 1 : 0) == 1;
                default: return fallback;
            }
        }

        public static void SetFloat(string propertyName, float value)
        {
            if (SettingsInstance != null && TrySetProperty(propertyName, value))
                return;

            switch (propertyName)
            {
                case "MusicVolume": PlayerPrefs.SetFloat("settings_music_volume", value); break;
                case "SoundVolume": PlayerPrefs.SetFloat("settings_sound_volume", value); break;
            }
            PlayerPrefs.Save();
        }

        public static void SetInt(string propertyName, int value)
        {
            if (SettingsInstance != null && TrySetProperty(propertyName, value))
                return;

            switch (propertyName)
            {
                case "CurrentLanguage": PlayerPrefs.SetInt("settings_language", value); break;
                case "ResolutionIndex": PlayerPrefs.SetInt("settings_resolution_index", value); break;
            }
            PlayerPrefs.Save();
        }

        public static void SetBool(string propertyName, bool value)
        {
            if (SettingsInstance != null && TrySetProperty(propertyName, value))
                return;

            if (propertyName == "IsFullscreen")
            {
                PlayerPrefs.SetInt("settings_fullscreen", value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private static bool TryGetProperty(string propertyName, out object value)
        {
            value = null;
            if (SettingsType == null || SettingsInstance == null)
                return false;

            PropertyInfo property = SettingsType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return false;

            value = property.GetValue(SettingsInstance, null);
            return true;
        }

        private static bool TrySetProperty(string propertyName, object value)
        {
            if (SettingsType == null || SettingsInstance == null)
                return false;

            PropertyInfo property = SettingsType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property == null || !property.CanWrite)
                return false;

            property.SetValue(SettingsInstance, value, null);
            return true;
        }

        private static object Call(string methodName, params object[] args)
        {
            if (SettingsType == null || SettingsInstance == null)
                return null;

            MethodInfo method = SettingsType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
                return null;

            return method.Invoke(SettingsInstance, args);
        }

        private static object ResolveInstance()
        {
            if (SettingsType == null)
                return null;

            PropertyInfo instanceProp = SettingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProp == null)
                return null;

            return instanceProp.GetValue(null, null);
        }

        private static Type FindType(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(typeName);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}








