using System;
using UnityEngine;

public enum GameLanguage
{
    EN,
    JP,
    KR,
    CN,
    VN
}

/// <summary>
/// Central settings state (audio/display/language) with persistence.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private const string KeySoundVolume = "settings_sound_volume";
    private const string KeyMusicVolume = "settings_music_volume";
    private const string KeyResolutionIndex = "settings_resolution_index";
    private const string KeyFullscreen = "settings_fullscreen";
    private const string KeyLanguage = "settings_language";

    private static SettingsManager instance;
    public static SettingsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<SettingsManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject(nameof(SettingsManager));
                    instance = go.AddComponent<SettingsManager>();
                }
            }
            return instance;
        }
    }

    [Header("Audio")]
    [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;

    [Header("Display")]
    [SerializeField] private int resolutionIndex;
    [SerializeField] private bool isFullscreen;

    [Header("Localization")]
    [SerializeField] private GameLanguage currentLanguage = GameLanguage.EN;

    public static readonly Vector2Int[] AvailableResolutions =
    {
        new Vector2Int(1920, 1080),
        new Vector2Int(1600, 900),
        new Vector2Int(1280, 720),
        new Vector2Int(960, 540),
        new Vector2Int(800, 450),
        new Vector2Int(640, 360)
    };

    public event Action<GameLanguage> OnLanguageChanged;

    public float SoundVolume
    {
        get => soundVolume;
        set
        {
            float next = Mathf.Clamp01(value);
            if (Mathf.Approximately(soundVolume, next)) return;
            soundVolume = next;
            ApplySoundVolume();
            SaveSettings();
        }
    }

    public float MusicVolume
    {
        get => musicVolume;
        set
        {
            float next = Mathf.Clamp01(value);
            if (Mathf.Approximately(musicVolume, next)) return;
            musicVolume = next;
            ApplyMusicVolume();
            SaveSettings();
        }
    }

    public bool IsFullscreen
    {
        get => isFullscreen;
        set
        {
            if (isFullscreen == value) return;
            isFullscreen = value;
            ApplyResolution();
            SaveSettings();
        }
    }

    public int ResolutionIndex
    {
        get => resolutionIndex;
        set
        {
            int next = Mathf.Clamp(value, 0, AvailableResolutions.Length - 1);
            if (resolutionIndex == next) return;
            resolutionIndex = next;
            ApplyResolution();
            SaveSettings();
        }
    }

    public GameLanguage CurrentLanguage
    {
        get => currentLanguage;
        set
        {
            if (currentLanguage == value) return;
            currentLanguage = value;
            SaveSettings();
            NotifyLocalizationLanguageChanged();
            if (OnLanguageChanged != null)
                OnLanguageChanged.Invoke(currentLanguage);
        }
    }

    public Vector2Int CurrentResolution => AvailableResolutions[Mathf.Clamp(resolutionIndex, 0, AvailableResolutions.Length - 1)];

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplyAll();
    }

    public string[] GetResolutionOptions()
    {
        string[] options = new string[AvailableResolutions.Length];
        for (int i = 0; i < AvailableResolutions.Length; i++)
        {
            options[i] = $"{AvailableResolutions[i].x} x {AvailableResolutions[i].y}";
        }
        return options;
    }

    public void SetResolutionByIndex(int index)
    {
        ResolutionIndex = index;
    }

    public void SetLanguageByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            CurrentLanguage = GameLanguage.EN;
            return;
        }

        switch (code.Trim().ToLowerInvariant())
        {
            case "jp":
                CurrentLanguage = GameLanguage.JP;
                break;
            case "kr":
                CurrentLanguage = GameLanguage.KR;
                break;
            case "cn":
                CurrentLanguage = GameLanguage.CN;
                break;
            case "vn":
                CurrentLanguage = GameLanguage.VN;
                break;
            default:
                CurrentLanguage = GameLanguage.EN;
                break;
        }
    }

    public void ApplySettings()
    {
        ApplyAll();
    }

    private void LoadSettings()
    {
        soundVolume = PlayerPrefs.GetFloat(KeySoundVolume, soundVolume);
        musicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, musicVolume);
        resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt(KeyResolutionIndex, resolutionIndex), 0, AvailableResolutions.Length - 1);
        isFullscreen = PlayerPrefs.GetInt(KeyFullscreen, isFullscreen ? 1 : 0) == 1;

        int savedLanguage = PlayerPrefs.GetInt(KeyLanguage, (int)currentLanguage);
        if (Enum.IsDefined(typeof(GameLanguage), savedLanguage))
        {
            currentLanguage = (GameLanguage)savedLanguage;
        }
        else
        {
            currentLanguage = GameLanguage.EN;
        }
    }

    public void ReloadSettings()
    {
        LoadSettings();
        ApplyAll();
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(KeySoundVolume, soundVolume);
        PlayerPrefs.SetFloat(KeyMusicVolume, musicVolume);
        PlayerPrefs.SetInt(KeyResolutionIndex, resolutionIndex);
        PlayerPrefs.SetInt(KeyFullscreen, isFullscreen ? 1 : 0);
        PlayerPrefs.SetInt(KeyLanguage, (int)currentLanguage);
        PlayerPrefs.Save();
    }

    private void ApplyAll()
    {
        ApplySoundVolume();
        ApplyMusicVolume();
        ApplyResolution();
    }

    private void ApplySoundVolume()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSoundVolume(soundVolume);
    }

    private void ApplyMusicVolume()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMusicVolume(musicVolume);
    }

    private void ApplyResolution()
    {
        Vector2Int resolution = CurrentResolution;
        Screen.SetResolution(resolution.x, resolution.y, isFullscreen);
    }

    private void NotifyLocalizationLanguageChanged()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.ReloadLocalization();
        }
        else
        {
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour != null && behaviour.GetType().Name == "LocalizationManager")
                {
                    behaviour.SendMessage("ReloadLocalization", SendMessageOptions.DontRequireReceiver);
                    break;
                }
            }
        }
    }
}
