using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Header Item Settings")]
    public RectTransform headerItemParent;
    public GameObject headerItemPrefab;
    public float iconSpacing = 180f;
    public float fadeDuration = 0.5f;
    public float delayBetween = 1f;
    public Vector2 iconSize = new Vector2(100f, 100f);

    [Header("Turn Timer Settings")]
    public Sprite[] imgTime;
    public GameObject time;

    private Coroutine timerCoroutine;

    [Header("Player Effect")]
    public GameObject _effectHealthPl;
    public GameObject _effectManaPl;
    public GameObject _effectRagePl;
    public GameObject _effectShieldPl;
    [SerializeField] private GameObject _effectImmortalPlPrefab;
    public TextMeshProUGUI _txtNameAI;

    // effect coroutines
    private Coroutine healthEffectRoutine;
    private Coroutine manaEffectRoutine;
    private Coroutine rageEffectRoutine;
    // AI effect coroutines
    private Coroutine aiHealthEffectRoutine;
    private Coroutine aiManaEffectRoutine;
    private Coroutine aiRageEffectRoutine;
    private GameObject immortalPlayerInstance;
    private GameObject immortalAIInstance;

    [Header("AI Effect")]
    public GameObject _effectHealthAI;
    public GameObject _effectManaAI;
    public GameObject _effectRageAI;
    public GameObject _effectShieldAI;
    [SerializeField] private GameObject _effectImmortalAIPrefab;
    public TextMeshProUGUI _txtNamePlayer;

    [Header("Player Stats UI (Image Fill)")]
    [SerializeField] private Image playerHpBar;
    [SerializeField] private Image playerShieldBar;
    [SerializeField] private TextMeshProUGUI playerHpText;
    [SerializeField] private Image playerManaBar;
    [SerializeField] private TextMeshProUGUI playerManaText;
    [SerializeField] private Image playerRageBar;
    [SerializeField] private TextMeshProUGUI playerRageText;
    

    [Header("AI Stats UI (Image Fill)")]
    [SerializeField] private Image aiHpBar;
    [SerializeField] private Image aiShieldBar;
    [SerializeField] private TextMeshProUGUI aiHpText;
    [SerializeField] private Image aiManaBar;
    [SerializeField] private TextMeshProUGUI aiManaText;
    [SerializeField] private Image aiRageBar;
    [SerializeField] private TextMeshProUGUI aiRageText;

    [Header("Element Icons")]
    [SerializeField] private Image _elementIconPlayer;
    [SerializeField] private Image _elementIconAI;
    [SerializeField] private Sprite[] _elementSprites; // Order: Dark, Earth, Fire, Light, Metal, Water, Wood
    private static readonly string[] ElementNames = { "Dark", "Earth", "Fire", "Light", "Metal", "Water", "Wood" };

    [Header("Stat Change Settings")]
    [SerializeField] private float statUpdateDuration = 0.3f;

    [Header("Skill Button Settings")]
    [SerializeField] private PetSkillButtonUI petSkillButton;

    [Header("Player Skill UI Fx")]
    [SerializeField] private BattleCharacterSkillUIFx playerSkillUIFx;

    [Header("Turn Banner")]
    [SerializeField] private BattleTurnBannerUI turnBannerUI;
    [Header("Audition Mini Game")]
    [SerializeField] private RhythmMiniGameManager rhythmMiniGameManager;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializeStatSliders();

        if (GameManager.Instance != null && turnBannerUI != null)
            turnBannerUI.ShowTurn(GameManager.Instance.currentTurn);
    }

    public void PlayPlayerSkillUIFx()
    {
        playerSkillUIFx?.Play();
    }

    public void InitializeStatSliders()
    {
        if (GameManager.Instance != null)
        {
            var player = GameManager.Instance.player;
            var ai = GameManager.Instance.ai;

            if (player != null)
            {
                player.OnStatChanged -= HandlePlayerStatChanged;
                player.OnStatChanged += HandlePlayerStatChanged;
                player.OnImmortalStateChanged -= HandlePlayerImmortalStateChanged;
                player.OnImmortalStateChanged += HandlePlayerImmortalStateChanged;

                if (petSkillButton != null)
                    petSkillButton.Initialize(player);
            }

            if (ai != null)
            {
                ai.OnStatChanged -= HandleAIStatChanged;
                ai.OnStatChanged += HandleAIStatChanged;
                ai.OnImmortalStateChanged -= HandleAIImmortalStateChanged;
                ai.OnImmortalStateChanged += HandleAIImmortalStateChanged;
            }

            UpdateAllPlayerStats();
            UpdateAllAIStats();
            HandlePlayerImmortalStateChanged(player != null && player.IsImmortalActive);
            HandleAIImmortalStateChanged(ai != null && ai.IsImmortalActive);
            CheckSkillRequirements(); // Kiá»ƒm tra nÃºt skill ngay khi vÃ o tráº­n
            // Update displayed pet names and elements (localized)
            UpdatePetNames();
            UpdateElementIcons(); // Display element icons for player and AI pets
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= UpdatePetNames;
                LocalizationManager.Instance.OnLocalizationLoaded -= UpdatePetNames;
                LocalizationManager.Instance.OnLanguageChanged += UpdatePetNames;
                LocalizationManager.Instance.OnLocalizationLoaded += UpdatePetNames;
            }
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.player != null)
            {
                GameManager.Instance.player.OnStatChanged -= HandlePlayerStatChanged;
                GameManager.Instance.player.OnImmortalStateChanged -= HandlePlayerImmortalStateChanged;
            }
            if (GameManager.Instance.ai != null)
            {
                GameManager.Instance.ai.OnStatChanged -= HandleAIStatChanged;
                GameManager.Instance.ai.OnImmortalStateChanged -= HandleAIImmortalStateChanged;
            }
        }
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= UpdatePetNames;
            LocalizationManager.Instance.OnLocalizationLoaded -= UpdatePetNames;
        }
    }
    public void CheckSkillRequirements()
    {
        if (petSkillButton != null)
            petSkillButton.RefreshState();
    }

    public async UniTask<AuditionMiniGameResult> PlayAuditionMiniGameAsync(SkillData skill, System.Threading.CancellationToken cancellationToken)
    {
        if (rhythmMiniGameManager == null)
        {
            RhythmMiniGameManager[] managers = Resources.FindObjectsOfTypeAll<RhythmMiniGameManager>();
            for (int i = 0; i < managers.Length; i++)
            {
                if (managers[i] != null)
                {
                    rhythmMiniGameManager = managers[i];
                    break;
                }
            }
        }

        if (rhythmMiniGameManager == null)
        {
            Debug.LogWarning("[UIManager] RhythmMiniGameManager not found. Audition will fallback to Miss.");
            return new AuditionMiniGameResult(AuditionResultType.Miss, Mathf.Max(0f, skill != null ? skill.auditionMissMultiplier : 1f), 0, 0);
        }

        GameObject boardObject = Board.Instance != null ? Board.Instance.gameObject : null;
        bool previousBoardState = boardObject != null && boardObject.activeSelf;
        if (boardObject != null)
            boardObject.SetActive(false);

        try
        {
            return await rhythmMiniGameManager.PlayAsync(skill, cancellationToken);
        }
        finally
        {
            if (boardObject != null)
                boardObject.SetActive(previousBoardState);
        }
    }

    // --- EVENT HANDLERS ---
    // --- EVENT HANDLERS ---
    private void HandlePlayerStatChanged(string statType, int current, int max)
    {
        UpdatePlayerStat(statType, current, max);
        CheckSkillRequirements();
    }

    private void HandleAIStatChanged(string statType, int current, int max)
    {
        UpdateAIStat(statType, current, max);
    }

    private void HandlePlayerImmortalStateChanged(bool active)
    {
        SetImmortalEffect(true, active);
    }

    private void HandleAIImmortalStateChanged(bool active)
    {
        SetImmortalEffect(false, active);
    }
    public void OnTurnChanged()
    {
        CheckSkillRequirements();

        if (GameManager.Instance != null && turnBannerUI != null)
            turnBannerUI.ShowTurn(GameManager.Instance.currentTurn);

        if (TutorialProgressManager.Instance != null && GameManager.Instance != null)
            TutorialProgressManager.Instance.NotifyBattleTurnChanged(GameManager.Instance.currentTurn);
    }

    public void UpdatePlayerStat(string statType, int current, int max)
    {
        if (string.Equals(statType, "HP", System.StringComparison.OrdinalIgnoreCase))
        {
            if (playerHpBar != null)
                playerHpBar.fillAmount = max > 0 ? (float)current / max : 0f;
            if (playerHpText != null)
                playerHpText.text = $"{current}/{max}";
        }
        else if (string.Equals(statType, "Mana", System.StringComparison.OrdinalIgnoreCase))
        {
            if (playerManaBar != null)
                playerManaBar.fillAmount = max > 0 ? (float)current / max : 0f;
            if (playerManaText != null)
                playerManaText.text = $"{current}/{max}";
        }
        else if (string.Equals(statType, "Rage", System.StringComparison.OrdinalIgnoreCase))
        {
            if (playerRageBar != null)
                playerRageBar.fillAmount = max > 0 ? (float)current / max : 0f;
            if (playerRageText != null)
                playerRageText.text = $"{current}/{max}";
        }
        else if (string.Equals(statType, "Shield", System.StringComparison.OrdinalIgnoreCase))
        {
            if (playerShieldBar != null)
                playerShieldBar.fillAmount = max > 0 ? (float)current / max : 0f;
        }
    }

    public void UpdateAIStat(string statType, int current, int max)
    {
        if (string.Equals(statType, "HP", System.StringComparison.OrdinalIgnoreCase))
        {
            if (aiHpBar != null)
                aiHpBar.fillAmount = max > 0 ? (float)current / max : 0f;
            if (aiHpText != null)
                aiHpText.text = $"{current}/{max}";
        }
        else if (string.Equals(statType, "Mana", System.StringComparison.OrdinalIgnoreCase))
        {
            if (aiManaBar != null)
                aiManaBar.fillAmount = max > 0 ? (float)current / max : 0f;
            if (aiManaText != null)
                aiManaText.text = $"{current}/{max}";
        }
        else if (string.Equals(statType, "Rage", System.StringComparison.OrdinalIgnoreCase))
        {
            if (aiRageBar != null)
                aiRageBar.fillAmount = max > 0 ? (float)current / max : 0f;
            if (aiRageText != null)
                aiRageText.text = $"{current}/{max}";
        }
        else if (string.Equals(statType, "Shield", System.StringComparison.OrdinalIgnoreCase))
        {
            if (aiShieldBar != null)
                aiShieldBar.fillAmount = max > 0 ? (float)current / max : 0f;
        }
    }

    public void UpdateAllPlayerStats()
    {
        if (GameManager.Instance == null || GameManager.Instance.player == null)
            return;

        PlayerStats stats = GameManager.Instance.player;
        UpdatePlayerStat("HP", stats.HP, stats.maxHP);
        UpdatePlayerStat("Shield", stats.Shield, stats.maxHP);
        UpdatePlayerStat("Mana", stats.Mana, stats.maxMana);
        UpdatePlayerStat("Rage", stats.Rage, stats.maxRage);

        if (playerShieldBar == null)
            UpdatePlayerStat("Shield", 0, 1);
    }

    public void UpdateAllAIStats()
    {
        if (GameManager.Instance == null || GameManager.Instance.ai == null)
            return;

        AIStats stats = GameManager.Instance.ai;
        UpdateAIStat("HP", stats.Health, stats.maxHealth);
        UpdateAIStat("Shield", stats.Shield, stats.maxHealth);
        UpdateAIStat("Mana", stats.Mana, stats.maxMana);
        UpdateAIStat("Rage", stats.Rage, stats.maxRage);

        if (aiShieldBar == null)
            UpdateAIStat("Shield", 0, 1);
    }

    public IEnumerator PlayDestroyedItemsSequence(List<int> destroyedIds, Func<int, int, IEnumerator> onGemResolved = null)
    {
        if (destroyedIds == null || destroyedIds.Count == 0)
        {
            if (Board.Instance != null)
                Board.Instance.gameObject.SetActive(true);
            yield break;
        }

        if (Board.Instance != null)
            Board.Instance.gameObject.SetActive(false);

        Dictionary<int, int> counts = new Dictionary<int, int>();
        foreach (int id in destroyedIds)
        {
            if (counts.ContainsKey(id)) counts[id]++;
            else counts[id] = 1;
        }

        List<int> keys = counts.Keys.OrderBy(k => k).ToList();

        if (headerItemParent != null)
        {
            foreach (Transform t in headerItemParent) Destroy(t.gameObject);
        }

        List<GameObject> icons = new List<GameObject>();
        int countUnique = keys.Count;
        float totalWidth = (countUnique - 1) * iconSpacing;
        float startX = -totalWidth / 2f;

        var spriteMap = Board.Instance?.SpriteResource;

        for (int i = 0; i < countUnique; i++)
        {
            int id = keys[i];
            int qty = counts[id];

            GameObject icon = Instantiate(headerItemPrefab, headerItemParent);
            icon.SetActive(true);

            TextMeshProUGUI[] existingTexts = icon.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int textIndex = 0; textIndex < existingTexts.Length; textIndex++)
            {
                if (existingTexts[textIndex] != null)
                    existingTexts[textIndex].gameObject.SetActive(false);
            }

            RectTransform rect = icon.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(startX + i * iconSpacing, 0f);
                rect.sizeDelta = iconSize;
            }

            Image img = icon.GetComponent<Image>();
            if (img != null && spriteMap != null && spriteMap.spriteItems != null)
            {
                var match = spriteMap.spriteItems.FirstOrDefault(s => s.id == id);
                if (match != null && match.sprite != null)
                {
                    img.sprite = match.sprite;
                    RectTransform imgRect = img.GetComponent<RectTransform>();
                    if (imgRect != null) imgRect.sizeDelta = iconSize;
                }
            }

            GameObject textObj = new GameObject("CountText", typeof(RectTransform));
            textObj.transform.SetParent(icon.transform, false);

            RectTransform txtRect = textObj.GetComponent<RectTransform>();
            txtRect.anchorMin = new Vector2(0.5f, 0f);
            txtRect.anchorMax = new Vector2(0.5f, 0f);
            txtRect.pivot = new Vector2(0.5f, 1f);
            txtRect.anchoredPosition = new Vector2(0f, -20f);
            txtRect.sizeDelta = new Vector2(100f, 40f);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "x" + qty.ToString();
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            CanvasGroup cg = icon.GetComponent<CanvasGroup>();
            if (cg == null) cg = icon.AddComponent<CanvasGroup>();
            cg.alpha = 1f;

            icons.Add(icon);
        }

        yield return null;

        for (int i = 0; i < icons.Count; i++)
        {
            yield return new WaitForSeconds(delayBetween);

            GameObject icon = icons[i];
            if (icon == null) continue;

            CanvasGroup cg = icon.GetComponent<CanvasGroup>();
            if (cg == null) cg = icon.AddComponent<CanvasGroup>();

            // Chá»‰ hiá»ƒn thá»‹ visual, khÃ´ng xá»­ lÃ½ gameplay/effect táº¡i UI layer.
            int id = keys[i];
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.currentTurn == GameManager.Turn.Player && GameManager.Instance.player != null)
                {
                    SetBoardEffectVisual(true, id, true);
                    // play corresponding player stat effect
                    if (id == BoardEffectSystem.HealItemId && _effectHealthPl != null)
                    {
                        if (healthEffectRoutine != null) StopCoroutine(healthEffectRoutine);
                        healthEffectRoutine = StartCoroutine(PlayEffectRoutine(_effectHealthPl, statUpdateDuration + 0.2f));
                    }
                    else if (id == BoardEffectSystem.ManaItemId && _effectManaPl != null)
                    {
                        if (manaEffectRoutine != null) StopCoroutine(manaEffectRoutine);
                        manaEffectRoutine = StartCoroutine(PlayEffectRoutine(_effectManaPl, statUpdateDuration + 0.2f));
                    }
                    else if (id == BoardEffectSystem.RageItemId && _effectRagePl != null)
                    {
                        if (rageEffectRoutine != null) StopCoroutine(rageEffectRoutine);
                        rageEffectRoutine = StartCoroutine(PlayEffectRoutine(_effectRagePl, statUpdateDuration + 0.2f));
                    }
                }
                else if (GameManager.Instance.currentTurn == GameManager.Turn.AI && GameManager.Instance.ai != null)
                {
                    SetBoardEffectVisual(false, id, true);
                    // play corresponding AI stat effect
                    if (id == BoardEffectSystem.HealItemId && _effectHealthAI != null)
                    {
                        if (aiHealthEffectRoutine != null) StopCoroutine(aiHealthEffectRoutine);
                        aiHealthEffectRoutine = StartCoroutine(PlayEffectRoutine(_effectHealthAI, statUpdateDuration + 0.2f));
                    }
                    else if (id == BoardEffectSystem.ManaItemId && _effectManaAI != null)
                    {
                        if (aiManaEffectRoutine != null) StopCoroutine(aiManaEffectRoutine);
                        aiManaEffectRoutine = StartCoroutine(PlayEffectRoutine(_effectManaAI, statUpdateDuration + 0.2f));
                    }
                    else if (id == BoardEffectSystem.RageItemId && _effectRageAI != null)
                    {
                        if (aiRageEffectRoutine != null) StopCoroutine(aiRageEffectRoutine);
                        aiRageEffectRoutine = StartCoroutine(PlayEffectRoutine(_effectRageAI, statUpdateDuration + 0.2f));
                    }
                }
            }

            if (onGemResolved != null)
            {
                IEnumerator routine = onGemResolved.Invoke(id, counts[id]);
                if (routine != null)
                    yield return StartCoroutine(routine);
            }

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / fadeDuration);
                yield return null;
            }

            // Táº¯t hiá»‡u á»©ng khi item biáº¿n máº¥t
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.currentTurn == GameManager.Turn.Player)
                {
                    SetAllBoardEffectVisual(true, false);
                }
                else if (GameManager.Instance.currentTurn == GameManager.Turn.AI)
                {
                    SetAllBoardEffectVisual(false, false);
                }
            }

            Destroy(icon);
        }

        foreach (Transform t in headerItemParent)
        {
            Destroy(t.gameObject);
        }

        if (Board.Instance != null)
            Board.Instance.gameObject.SetActive(true);

        yield break;
    }

    // Async wrapper so other systems can await the sequence
    public UniTask PlayDestroyedItemsSequenceAsync(List<int> destroyedIds, Func<int, int, IEnumerator> onGemResolved = null)
    {
        var tcs = new UniTaskCompletionSource();
        StartCoroutine(PlayDestroyedItemsSequence_Coroutine(destroyedIds, onGemResolved, tcs));
        return tcs.Task;
    }

    private IEnumerator PlayDestroyedItemsSequence_Coroutine(List<int> destroyedIds, Func<int, int, IEnumerator> onGemResolved, UniTaskCompletionSource tcs)
    {
        yield return StartCoroutine(PlayDestroyedItemsSequence(destroyedIds, onGemResolved));
        tcs.TrySetResult();
    }

    private void SetBoardEffectVisual(bool forPlayer, int itemId, bool active)
    {
        if (forPlayer)
        {
            if (itemId == BoardEffectSystem.ShieldItemId && _effectShieldPl != null) _effectShieldPl.SetActive(active);
            if (itemId == BoardEffectSystem.RageItemId && _effectRagePl != null) _effectRagePl.SetActive(active);
            if (itemId == BoardEffectSystem.HealItemId && _effectHealthPl != null) _effectHealthPl.SetActive(active);
            if (itemId == BoardEffectSystem.ManaItemId && _effectManaPl != null) _effectManaPl.SetActive(active);
            return;
        }

        if (itemId == BoardEffectSystem.ShieldItemId && _effectShieldAI != null) _effectShieldAI.SetActive(active);
        if (itemId == BoardEffectSystem.RageItemId && _effectRageAI != null) _effectRageAI.SetActive(active);
        if (itemId == BoardEffectSystem.HealItemId && _effectHealthAI != null) _effectHealthAI.SetActive(active);
        if (itemId == BoardEffectSystem.ManaItemId && _effectManaAI != null) _effectManaAI.SetActive(active);
    }

    private void SetAllBoardEffectVisual(bool forPlayer, bool active)
    {
        if (forPlayer)
        {
            if (_effectShieldPl != null) _effectShieldPl.SetActive(active);
            if (_effectRagePl != null) _effectRagePl.SetActive(active);
            if (_effectHealthPl != null) _effectHealthPl.SetActive(active);
            if (_effectManaPl != null) _effectManaPl.SetActive(active);
            return;
        }

        if (_effectShieldAI != null) _effectShieldAI.SetActive(active);
        if (_effectRageAI != null) _effectRageAI.SetActive(active);
        if (_effectHealthAI != null) _effectHealthAI.SetActive(active);
        if (_effectManaAI != null) _effectManaAI.SetActive(active);
    }

    public IEnumerator ShowTurnTimer(float duration, GameManager.Turn currentTurn)
    {
        if (time == null || imgTime == null || imgTime.Length < 10)
        {
            Debug.LogWarning("ChÆ°a gÃ¡n time GameObject hoáº·c imgTime khÃ´ng Ä‘á»§ 10 sprite!");
            yield return new WaitForSeconds(duration);
            yield break;
        }

        Image timeImage = time.GetComponent<Image>();
        if (timeImage == null)
        {
            Debug.LogWarning("GameObject time khÃ´ng cÃ³ Image component!");
            yield return new WaitForSeconds(duration);
            yield break;
        }

        timerCoroutine = StartCoroutine(TimerDisplay(duration, timeImage));
        yield return timerCoroutine;
    }

    private IEnumerator TimerDisplay(float duration, Image timeImage)
    {
        time.SetActive(true);
        float timeLeft = duration;

        // Giáº£ sá»­ imgTime[0] lÃ  sprite sá»‘ 9, imgTime[1] lÃ  sá»‘ 8, ..., imgTime[9] lÃ  sá»‘ 0
        while (timeLeft > 0)
        {
            int spriteIndex = Mathf.Clamp(Mathf.CeilToInt(timeLeft) - 1, 0, 9);
            timeImage.sprite = imgTime[spriteIndex];
            timeLeft -= Time.deltaTime;
            yield return null;
        }

        time.SetActive(false);
        timerCoroutine = null;
    }

    public void StopTurnTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        if (time != null)
        {
            time.SetActive(false);
        }
    }

    /// <summary>
    /// Giáº£m HP cá»§a player (gÃ¢y sÃ¡t thÆ°Æ¡ng)
    /// </summary>
    public void DecreasePlayerHP(int amount)
    {
        if (GameManager.Instance?.player != null)
        {
            GameManager.Instance.player.TakeDamage(amount);
            UpdatePlayerStat("HP", GameManager.Instance.player.HP, GameManager.Instance.player.maxHP);
        }
    }

    /// <summary>
    /// TÄƒng HP cá»§a player (há»“i mÃ¡u)
    /// </summary>
    public void IncreasePlayerHP(int amount)
    {
        if (GameManager.Instance?.player != null)
        {
            GameManager.Instance.player.Heal(amount);
            UpdatePlayerStat("HP", GameManager.Instance.player.HP, GameManager.Instance.player.maxHP);
            // play UI effect
            if (_effectHealthPl != null)
            {
                if (healthEffectRoutine != null) StopCoroutine(healthEffectRoutine);
                healthEffectRoutine = StartCoroutine(PlayEffectRoutine(_effectHealthPl, statUpdateDuration + 0.2f));
            }
        }
    }

    /// <summary>
    /// TÄƒng Mana cá»§a player
    /// </summary>
    public void IncreasePlayerMana(int amount)
    {
        if (GameManager.Instance?.player != null)
        {
            GameManager.Instance.player.GainMana(amount);
            UpdatePlayerStat("Mana", GameManager.Instance.player.Mana, GameManager.Instance.player.maxMana);
            if (_effectManaPl != null)
            {
                if (manaEffectRoutine != null) StopCoroutine(manaEffectRoutine);
                manaEffectRoutine = StartCoroutine(PlayEffectRoutine(_effectManaPl, statUpdateDuration + 0.2f));
            }
        }
    }

    /// <summary>
    /// TÄƒng Rage cá»§a player
    /// </summary>
    public void IncreasePlayerRage(int amount)
    {
        if (GameManager.Instance?.player != null)
        {
            GameManager.Instance.player.GainRage(amount);
            UpdatePlayerStat("Rage", GameManager.Instance.player.Rage, GameManager.Instance.player.maxRage);
            if (_effectRagePl != null)
            {
                if (rageEffectRoutine != null) StopCoroutine(rageEffectRoutine);
                rageEffectRoutine = StartCoroutine(PlayEffectRoutine(_effectRagePl, statUpdateDuration + 0.2f));
            }
        }
    }

    public void PreviewImmortalEffectOnPlayer()
    {
        SetImmortalEffect(true, true);
    }

    public void PreviewImmortalEffectOnAI()
    {
        SetImmortalEffect(false, true);
    }

    private void SetImmortalEffect(bool isPlayer, bool active)
    {
        GameObject currentInstance = isPlayer ? immortalPlayerInstance : immortalAIInstance;
        if (!active)
        {
            if (currentInstance != null)
                Destroy(currentInstance);

            if (isPlayer)
                immortalPlayerInstance = null;
            else
                immortalAIInstance = null;
            return;
        }

        if (currentInstance != null)
            return;

        GameObject prefab = isPlayer ? _effectImmortalPlPrefab : _effectImmortalAIPrefab;
        if (prefab == null)
            return;

        Transform anchor = null;
        if (GameManager.Instance != null)
        {
            if (isPlayer && GameManager.Instance.player != null)
                anchor = GameManager.Instance.player.player != null ? GameManager.Instance.player.player.transform : GameManager.Instance.player.transform;
            else if (!isPlayer && GameManager.Instance.ai != null)
                anchor = GameManager.Instance.ai.AI != null ? GameManager.Instance.ai.AI.transform : GameManager.Instance.ai.transform;
        }

        if (anchor == null)
            return;

        GameObject instance = Instantiate(prefab, anchor);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.SetActive(true);

        if (isPlayer)
            immortalPlayerInstance = instance;
        else
            immortalAIInstance = instance;
    }

    private IEnumerator PlayEffectRoutine(GameObject go, float duration)
    {
        if (go == null) yield break;
        go.SetActive(true);
        yield return new WaitForSeconds(duration);
        go.SetActive(false);
    }

    /// <summary>
    /// Giáº£m HP cá»§a AI (gÃ¢y sÃ¡t thÆ°Æ¡ng)
    /// </summary>
    public void DecreaseAIHP(int amount)
    {
        if (GameManager.Instance?.ai != null)
        {
            GameManager.Instance.ai.TakeDamage(amount);
            UpdateAIStat("HP", GameManager.Instance.ai.Health, GameManager.Instance.ai.maxHealth);
        }
    }

    /// <summary>
    /// TÄƒng HP cá»§a AI (há»“i mÃ¡u)
    /// </summary>
    public void IncreaseAIHP(int amount)
    {
        if (GameManager.Instance?.ai != null)
        {
            GameManager.Instance.ai.Heal(amount);
            UpdateAIStat("HP", GameManager.Instance.ai.Health, GameManager.Instance.ai.maxHealth);
        }
    }

    /// <summary>
    /// TÄƒng Mana cá»§a AI
    /// </summary>
    public void IncreaseAIMana(int amount)
    {
        if (GameManager.Instance?.ai != null)
        {
            GameManager.Instance.ai.GainMana(amount);
            UpdateAIStat("Mana", GameManager.Instance.ai.Mana, GameManager.Instance.ai.maxMana);
        }
    }

    /// <summary>
    /// TÄƒng Rage cá»§a AI
    /// </summary>
    public void IncreaseAIRage(int amount)
    {
        if (GameManager.Instance?.ai != null)
        {
            GameManager.Instance.ai.GainRage(amount);
            UpdateAIStat("Rage", GameManager.Instance.ai.Rage, GameManager.Instance.ai.maxRage);
        }
    }

    // Update pet name + element display with localization if available
    private void UpdatePetNames()
    {
        try
        {
            string playerText = "";
            string aiText = "";

            var player = GameManager.Instance?.player;
            var ai = GameManager.Instance?.ai;

            if (player != null)
            {
                string name = !string.IsNullOrEmpty(player.playerName) ? player.playerName : "";
                string elementKey = ResolveElementName(player.element, player.playerName);
                bool hasAdvantage = ai != null && HasElementAdvantage(elementKey, ai.weakness);
                string elementLabel = BuildElementDisplay(elementKey, hasAdvantage);

                if (LocalizationManager.Instance != null)
                    name = LocalizationManager.Instance.GetText(name, name);

                playerText = string.IsNullOrEmpty(elementLabel) ? name : $"{name} ({elementLabel})";
            }

            if (ai != null)
            {
                string name = !string.IsNullOrEmpty(ai.petName) ? ai.petName : "";
                string elementKey = ResolveElementName(ai.element, ai.petName);
                bool hasAdvantage = player != null && HasElementAdvantage(elementKey, player.weakness);
                string elementLabel = BuildElementDisplay(elementKey, hasAdvantage);

                if (LocalizationManager.Instance != null)
                    name = LocalizationManager.Instance.GetText(name, name);

                aiText = string.IsNullOrEmpty(elementLabel) ? name : $"{name} ({elementLabel})";
            }

            // set UI
            if (_txtNamePlayer != null)
            {
                var tmp = _txtNamePlayer.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = playerText;
                else
                {
                    var txt = _txtNamePlayer.GetComponent<UnityEngine.UI.Text>();
                    if (txt != null) txt.text = playerText;
                }
            }

            if (_txtNameAI != null)
            {
                var tmp = _txtNameAI.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = aiText;
                else
                {
                    var txt = _txtNameAI.GetComponent<UnityEngine.UI.Text>();
                    if (txt != null) txt.text = aiText;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("UpdatePetNames failed: " + ex.Message);
        }
    }

    // Update element icons for player and AI pets
    private void UpdateElementIcons()
    {
        try
        {
            var player = GameManager.Instance?.player;
            var ai = GameManager.Instance?.ai;

            // Set player element icon
            if (player != null && _elementIconPlayer != null && _elementSprites != null && _elementSprites.Length > 0)
            {
                int playerElementIndex = GetElementIndexFromElementOrName(player.element, player.playerName);
                if (playerElementIndex >= 0 && playerElementIndex < _elementSprites.Length)
                {
                    _elementIconPlayer.sprite = _elementSprites[playerElementIndex];
                }
            }

            // Set AI element icon
            if (ai != null && _elementIconAI != null && _elementSprites != null && _elementSprites.Length > 0)
            {
                int aiElementIndex = GetElementIndexFromElementOrName(ai.element, ai.petName);
                if (aiElementIndex >= 0 && aiElementIndex < _elementSprites.Length)
                {
                    _elementIconAI.sprite = _elementSprites[aiElementIndex];
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("UpdateElementIcons failed: " + ex.Message);
        }
    }

    private string ResolveElementName(string elementName, string petName)
    {
        int elementIndex = GetElementIndexFromElementOrName(elementName, petName);
        if (elementIndex < 0 || elementIndex >= ElementNames.Length)
            return string.Empty;

        return ElementNames[elementIndex];
    }

    private string BuildElementDisplay(string elementKey, bool hasAdvantage)
    {
        if (string.IsNullOrEmpty(elementKey))
            return string.Empty;

        string elementLabel = elementKey;
        if (LocalizationManager.Instance != null)
            elementLabel = LocalizationManager.Instance.GetText(elementKey, elementKey);

        if (!hasAdvantage)
            return elementLabel;

        string atkLabel = "ATK";
        if (LocalizationManager.Instance != null)
            atkLabel = LocalizationManager.Instance.GetText("ATK", "ATK");

        return elementLabel + " +20% " + atkLabel;
    }

    private bool HasElementAdvantage(string attackerElementKey, string targetWeakness)
    {
        return ElementWeaknessSystem.GetWeaknessMultiplier(attackerElementKey, targetWeakness) > 1.0f;
    }

    // Get element index from element name or pet name (mirrors ChoosePet logic)
    private int GetElementIndexFromElementOrName(string elementName, string petName)
    {
        string rawElement = !string.IsNullOrWhiteSpace(elementName) ? elementName.Trim() : string.Empty;
        if (string.IsNullOrEmpty(rawElement) && !string.IsNullOrEmpty(petName))
        {
            rawElement = petName;
            int firstNumberIndex = -1;
            for (int i = 0; i < rawElement.Length; i++)
            {
                if (char.IsDigit(rawElement[i]))
                {
                    firstNumberIndex = i;
                    break;
                }
            }

            if (firstNumberIndex >= 0)
                rawElement = rawElement.Substring(0, firstNumberIndex);
        }

        if (string.IsNullOrEmpty(rawElement))
            return -1;

        for (int i = 0; i < ElementNames.Length; i++)
        {
            if (string.Equals(rawElement, ElementNames[i], System.StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }
}

