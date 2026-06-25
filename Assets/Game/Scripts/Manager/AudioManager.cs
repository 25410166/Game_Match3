using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages all audio in the game (sound effects and music)
/// Singleton pattern with volume control
/// Features:
/// - Random music playlist (loops through random tracks)
/// - Sound effects for UI, fishing events
/// - Volume control for music and sounds
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                }
            }
            return instance;
        }
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource soundSource;

    [Header("Volume Settings")]
    [SerializeField] private float soundVolume = 1f;
    [SerializeField] private float musicVolume = 1f;

    [Header("Music Playlist (NEW)")]
    [SerializeField] private List<AudioClip> musicPlaylist = new List<AudioClip>();
    [SerializeField] private List<AudioClip> battleMusicPlaylist = new List<AudioClip>();
    private Coroutine musicLoopCoroutine;
    private List<AudioClip> activePlaylist = null;

    [Header("Sound Effects (NEW)")]
    [SerializeField] private AudioClip soundButtonClick;

    [SerializeField] private AudioClip soundUpgradeGemProcessing;
    [SerializeField] private AudioClip soundUpgradeGemSuccess;
    [SerializeField] private AudioClip soundUpgradeGemFailed;
    [SerializeField] private AudioClip soundUpgradePetSuccess;
    [SerializeField] private AudioClip soundUpgradePetFailed;
    [SerializeField] private AudioClip soundPurchaseSuccess;
    [SerializeField] private AudioClip soundPurchaseFailed;
    [SerializeField] private AudioClip soundBattleCharacterSkill;
    [SerializeField] private AudioClip soundBattleGainHp;
    [SerializeField] private AudioClip soundBattleGainMana;
    [SerializeField] private AudioClip soundBattleGainRage;
    [SerializeField] private AudioClip soundBattleGainArmor;
    [SerializeField] private AudioClip soundBattleRain;
    [SerializeField] private List<AudioClip> listSoundMatch = new List<AudioClip>();
    [SerializeField] private AudioClip soundBattleHit;

    [Header("Scene Music")]
    [SerializeField] private string battleSceneName = "SceneBattle";
    [SerializeField] private bool autoBindButtonClickSound = true;

    private readonly HashSet<int> registeredButtonIds = new HashSet<int>();

    [Header("Sound Settings")]
    [SerializeField] private float soundEffectVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float masterVolume = 1f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Setup audio sources if not assigned
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = false; // CHANGED: We'll handle looping manually for playlist
            musicSource.playOnAwake = false;
        }

        if (soundSource == null)
        {
            soundSource = gameObject.AddComponent<AudioSource>();
            soundSource.loop = false;
            soundSource.playOnAwake = false;
        }

        // Load settings from SettingsManager
        if (SettingsManager.Instance != null)
        {
            soundVolume = SettingsManager.Instance.SoundVolume;
            musicVolume = SettingsManager.Instance.MusicVolume;
            ApplyVolumes();
        }

        // Start music playlist
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResolvePlaylistByScene(SceneManager.GetActiveScene().name);

        if (autoBindButtonClickSound)
            RegisterAllButtonsInActiveScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ============ MUSIC PLAYLIST ============

    /// <summary>
    /// Start playing random music from playlist (loops continuously)
    /// </summary>
    public void StartMusicPlaylist()
    {
        activePlaylist = musicPlaylist;
        StartPlaylist(activePlaylist);
    }

    public void StartBattleMusicPlaylist()
    {
        activePlaylist = battleMusicPlaylist;
        StartPlaylist(activePlaylist);
    }

    private void StartPlaylist(List<AudioClip> playlist)
    {
        if (musicLoopCoroutine != null)
        {
            StopCoroutine(musicLoopCoroutine);
        }

        if (playlist != null && playlist.Count > 0)
        {
            musicLoopCoroutine = StartCoroutine(MusicPlaylistLoop());
        }
        else
        {
            musicSource.Stop();
            musicLoopCoroutine = null;
        }
    }

    /// <summary>
    /// Continuously play random music from playlist
    /// </summary>
    private IEnumerator MusicPlaylistLoop()
    {
        while (true)
        {
            if (activePlaylist == null || activePlaylist.Count == 0)
                yield break;

            AudioClip randomMusic = activePlaylist[Random.Range(0, activePlaylist.Count)];
            if (randomMusic == null)
            {
                yield return null;
                continue;
            }

            PlayMusic(randomMusic, false);

            while (musicSource != null && musicSource.isPlaying)
                yield return null;
        }
    }

    /// <summary>
    /// Play background music
    /// </summary>
    public void PlayMusic(AudioClip clip, bool loop = false)
    {
        if (clip != null && musicSource != null)
        {
            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.Play();
        }
    }

    /// <summary>
    /// Stop background music
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }

        if (musicLoopCoroutine != null)
        {
            StopCoroutine(musicLoopCoroutine);
            musicLoopCoroutine = null;
        }
    }

    /// <summary>
    /// Pause background music
    /// </summary>
    public void PauseMusic()
    {
        if (musicSource != null)
        {
            musicSource.Pause();
        }
    }

    /// <summary>
    /// Resume background music
    /// </summary>
    public void ResumeMusic()
    {
        if (musicSource != null)
        {
            musicSource.UnPause();
        }
    }

    // ============ SOUND EFFECTS ============

    /// <summary>
    /// Play button click sound
    /// </summary>
    public void PlayButtonClickSound()
    {
        PlaySound(soundButtonClick);
    }

    public void PlayUpgradeGemProcessingSound()
    {
        PlaySound(soundUpgradeGemProcessing);
    }

    public void PlayUpgradeGemSuccessSound()
    {
        PlaySound(soundUpgradeGemSuccess);
    }

    public void PlayUpgradeGemFailedSound()
    {
        PlaySound(soundUpgradeGemFailed);
    }

    public void PlayUpgradePetSuccessSound()
    {
        PlaySound(soundUpgradePetSuccess);
    }

    public void PlayUpgradePetFailedSound()
    {
        PlaySound(soundUpgradePetFailed);
    }

    public void PlayPurchaseSuccessSound()
    {
        PlaySound(soundPurchaseSuccess);
    }

    public void PlayPurchaseFailedSound()
    {
        PlaySound(soundPurchaseFailed);
    }

    public void PlayBattleCharacterSkillSound()
    {
        PlaySound(soundBattleCharacterSkill);
    }

    public void PlayBattleGainHpSound()
    {
        PlaySound(soundBattleGainHp);
    }

    public void PlayBattleGainManaSound()
    {
        PlaySound(soundBattleGainMana);
    }

    public void PlayBattleGainRageSound()
    {
        PlaySound(soundBattleGainRage);
    }

    public void PlayBattleGainArmorSound()
    {
        PlaySound(soundBattleGainArmor);
    }

    public void PlayBattleRainSound()
    {
        PlaySound(soundBattleRain);
    }

    public void PlayBattleHitSound()
    {
        PlaySound(soundBattleHit);
    }

    public void PlayRandomMatchSound()
    {
        if (listSoundMatch == null || listSoundMatch.Count == 0)
            return;

        List<AudioClip> validClips = null;
        for (int i = 0; i < listSoundMatch.Count; i++)
        {
            AudioClip clip = listSoundMatch[i];
            if (clip == null)
                continue;

            if (validClips == null)
                validClips = new List<AudioClip>();

            validClips.Add(clip);
        }

        if (validClips == null || validClips.Count == 0)
            return;

        int randomIndex = Random.Range(0, validClips.Count);
        PlaySound(validClips[randomIndex]);
    }

    /// <summary>
    /// Play cast charge sound (when holding mouse to charge)
    /// </summary>
   

    /// <summary>
    /// Play a sound effect (generic method)
    /// </summary>
    public void PlaySound(AudioClip clip, float volumeScale = 1f)
    {
        if (clip != null && soundSource != null)
        {
            soundSource.PlayOneShot(clip, volumeScale * soundEffectVolume * masterVolume);
        }
    }

    // ============ VOLUME CONTROL ============

    /// <summary>
    /// Set sound effects volume
    /// </summary>
    public void SetSoundVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        if (soundSource != null)
        {
            soundSource.volume = soundVolume;
        }
    }

    /// <summary>
    /// Set music volume
    /// </summary>
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    /// <summary>
    /// Set master volume (affects all audio)
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyVolumes();
    }

    public void RegisterButtonClick(Button button)
    {
        if (!autoBindButtonClickSound || button == null)
            return;

        int id = button.GetInstanceID();
        if (registeredButtonIds.Contains(id))
            return;

        button.onClick.AddListener(PlayButtonClickSound);
        registeredButtonIds.Add(id);
    }

    private void RegisterAllButtonsInActiveScene()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
            RegisterButtonClick(buttons[i]);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResolvePlaylistByScene(scene.name);

        if (autoBindButtonClickSound)
            RegisterAllButtonsInActiveScene();
    }

    private void ResolvePlaylistByScene(string sceneName)
    {
        bool isBattleScene = !string.IsNullOrWhiteSpace(sceneName) &&
                             string.Equals(sceneName, battleSceneName, System.StringComparison.OrdinalIgnoreCase);

        if (isBattleScene)
            StartBattleMusicPlaylist();
        else
            StartMusicPlaylist();
    }

    /// <summary>
    /// Get current sound volume
    /// </summary>
    public float GetSoundVolume()
    {
        return soundVolume;
    }

    /// <summary>
    /// Get current music volume
    /// </summary>
    public float GetMusicVolume()
    {
        return musicVolume;
    }

    /// <summary>
    /// Apply volume settings to audio sources
    /// </summary>
    private void ApplyVolumes()
    {
        if (soundSource != null)
        {
            soundSource.volume = soundVolume * masterVolume;
        }
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
    }

    /// <summary>
    /// Mute all audio
    /// </summary>
    public void MuteAll(bool mute)
    {
        if (soundSource != null)
        {
            soundSource.mute = mute;
        }
        if (musicSource != null)
        {
            musicSource.mute = mute;
        }
    }
}
