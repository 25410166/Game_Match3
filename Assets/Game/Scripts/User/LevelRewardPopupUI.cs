using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelRewardPopupUI : MonoBehaviour
{
    [Serializable]
    public class LevelRewardGemEntry
    {
        public int elementId;
        public int gemLevel = 1;
        public int quantity = 10;
    }

    [Serializable]
    public class LevelRewardDefinition
    {
        [Range(2, 10)] public int level = 2;
        public int gold = 1000;
        public int diamond = 200;
        public List<LevelRewardGemEntry> gemRewards = new List<LevelRewardGemEntry>();
    }

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private Button closeButton;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float showDelay = 0.05f;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI levelText;

    [Header("Reward List")]
    [SerializeField] private Transform rewardListRoot;
    [SerializeField] private WinRewardItemUI rewardItemPrefab;
    [SerializeField] private Sprite goldSprite;
    [SerializeField] private Sprite diamondSprite;

    [Header("FX")]
    [SerializeField] private GameObject openFxPrefab;
    [SerializeField] private Transform fxSpawnRoot;

    [Header("Reward Data")]
    [SerializeField] private List<LevelRewardDefinition> rewardsByLevel = new List<LevelRewardDefinition>();

    private readonly List<WinRewardItemUI> spawnedRewardItems = new List<WinRewardItemUI>();
    private Coroutine showRoutine;
    private Tween popupTween;
    private int currentRewardLevel = -1;
    private bool isShowing;
    private bool isClaiming;

    public Button ContinueButton => closeButton;
    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;
    public event Action OnOpened;
    public event Action OnRewardClaimed;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (popupCanvasGroup == null && popupRoot != null)
            popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();

        if (popupCanvasGroup == null && popupRoot != null)
            popupCanvasGroup = popupRoot.AddComponent<CanvasGroup>();

        EnsureDefaultRewards();
        BindButtons();
        SetVisible(false, true);
    }

    private void OnValidate()
    {
        EnsureDefaultRewards();
    }

    private void OnDestroy()
    {
        popupTween?.Kill();
    }

    private void BindButtons()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(HandleCloseClicked);
        closeButton.onClick.AddListener(HandleCloseClicked);
    }

    public void TryShowPendingReward()
    {
        if (!isActiveAndEnabled)
            return;

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(TryShowPendingRewardRoutine());
    }

    private IEnumerator TryShowPendingRewardRoutine()
    {
        yield return null;

        if (showDelay > 0f)
            yield return new WaitForSecondsRealtime(showDelay);

        showRoutine = null;
        ShowNextPendingRewardIfAny();
    }

    private void ShowNextPendingRewardIfAny()
    {
        if (isShowing || isClaiming || PlayerManager.Instance == null)
            return;

        int pendingLevel = PlayerManager.Instance.GetNextPendingLevelReward();
        if (pendingLevel < 2)
        {
            SetVisible(false, true);
            return;
        }

        LevelRewardDefinition definition = GetRewardDefinition(pendingLevel);
        if (definition == null)
        {
            Debug.LogWarning($"[LevelRewardPopupUI] Missing reward definition for level {pendingLevel}.");
            return;
        }

        currentRewardLevel = pendingLevel;
        BuildRewardList(definition);
        RefreshTexts(pendingLevel);
        PlayOpenFx();
        SetVisible(true, false);
    }

    private void HandleCloseClicked()
    {
        if (isClaiming || currentRewardLevel < 2)
        {
            SetVisible(false, false);
            return;
        }

        StartCoroutine(ClaimAndContinueRoutine(currentRewardLevel));
    }

    private IEnumerator ClaimAndContinueRoutine(int rewardLevel)
    {
        isClaiming = true;

        LevelRewardDefinition definition = GetRewardDefinition(rewardLevel);
        if (definition != null && PlayerManager.Instance != null)
        {
            GrantReward(definition);
            PlayerManager.Instance.TryClaimLevelReward(rewardLevel);
            PlayerManager.Instance.SaveData();
        }

        SetVisible(false, false);
        OnRewardClaimed?.Invoke();
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, fadeDuration));

        isClaiming = false;
        currentRewardLevel = -1;
        ShowNextPendingRewardIfAny();
    }

    private void GrantReward(LevelRewardDefinition definition)
    {
        if (PlayerManager.Instance == null || definition == null)
            return;

        if (definition.gold > 0)
            PlayerManager.Instance.AddGold(definition.gold);

        if (definition.diamond > 0)
            PlayerManager.Instance.AddDiamond(definition.diamond);

        if (definition.gemRewards == null)
            return;

        for (int i = 0; i < definition.gemRewards.Count; i++)
        {
            LevelRewardGemEntry gemReward = definition.gemRewards[i];
            if (gemReward == null || gemReward.quantity <= 0)
                continue;

            PlayerManager.Instance.AddOrUpdateOwnedGem(
                Mathf.Max(0, gemReward.elementId),
                Mathf.Clamp(gemReward.gemLevel, 1, 5),
                gemReward.quantity);
        }
    }

    private void BuildRewardList(LevelRewardDefinition definition)
    {
        ClearRewardItems();
        if (rewardListRoot == null || rewardItemPrefab == null || definition == null)
            return;

        if (definition.gold > 0)
            SpawnRewardItem(GetLocalizedText("gold", "Gold"), definition.gold, goldSprite);

        if (definition.diamond > 0)
            SpawnRewardItem(GetLocalizedText("diamond", "Diamond"), definition.diamond, diamondSprite);

        if (definition.gemRewards == null)
            return;

        for (int i = 0; i < definition.gemRewards.Count; i++)
        {
            LevelRewardGemEntry gemReward = definition.gemRewards[i];
            if (gemReward == null || gemReward.quantity <= 0)
                continue;

            SpawnRewardItem(
                BuildGemDisplayName(gemReward.elementId, gemReward.gemLevel),
                gemReward.quantity,
                GetGemSprite(gemReward.elementId, gemReward.gemLevel));
        }
    }

    private void SpawnRewardItem(string displayName, int amount, Sprite sprite)
    {
        WinRewardItemUI item = Instantiate(rewardItemPrefab, rewardListRoot);
        item.Setup(displayName, amount, sprite);
        spawnedRewardItems.Add(item);
    }

    private void RefreshTexts(int rewardLevel)
    {
        if (titleText != null)
            titleText.text = GetLocalizedText("level_reward_title", "Level Up Reward");

        if (levelText != null)
        {
            string format = GetLocalizedText("level_reward_level_format", "Level {0}");
            levelText.text = string.Format(format, rewardLevel);
        }
    }

    private void PlayOpenFx()
    {
        if (openFxPrefab == null)
            return;

        Transform spawnRoot = fxSpawnRoot != null ? fxSpawnRoot : transform;
        GameObject fxInstance = Instantiate(openFxPrefab, spawnRoot);
        fxInstance.transform.localPosition = Vector3.zero;
        fxInstance.transform.localRotation = Quaternion.identity;
    }

    private void ClearRewardItems()
    {
        for (int i = 0; i < spawnedRewardItems.Count; i++)
        {
            if (spawnedRewardItems[i] != null)
                Destroy(spawnedRewardItems[i].gameObject);
        }

        spawnedRewardItems.Clear();
    }

    private void SetVisible(bool visible, bool instant)
    {
        if (popupRoot == null || popupCanvasGroup == null)
            return;

        bool wasOpen = popupRoot.activeSelf;
        isShowing = visible;
        popupTween?.Kill();

        if (visible)
        {
            popupRoot.SetActive(true);
            popupCanvasGroup.alpha = instant ? 1f : 0f;
            popupCanvasGroup.blocksRaycasts = true;
            popupCanvasGroup.interactable = true;

            if (!wasOpen)
                OnOpened?.Invoke();

            if (!instant)
                popupTween = popupCanvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }
        else
        {
            popupCanvasGroup.blocksRaycasts = false;
            popupCanvasGroup.interactable = false;

            if (instant)
            {
                popupCanvasGroup.alpha = 0f;
                popupRoot.SetActive(false);
            }
            else
            {
                popupTween = popupCanvasGroup.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(() =>
                {
                    if (popupRoot != null)
                        popupRoot.SetActive(false);
                });
            }
        }
    }
    private LevelRewardDefinition GetRewardDefinition(int level)
    {
        for (int i = 0; i < rewardsByLevel.Count; i++)
        {
            LevelRewardDefinition definition = rewardsByLevel[i];
            if (definition != null && definition.level == level)
                return definition;
        }

        return null;
    }

    private void EnsureDefaultRewards()
    {
        if (rewardsByLevel == null)
            rewardsByLevel = new List<LevelRewardDefinition>();

        for (int level = 2; level <= 10; level++)
        {
            if (GetRewardDefinition(level) != null)
                continue;

            rewardsByLevel.Add(CreateDefaultReward(level));
        }

        rewardsByLevel.Sort((a, b) =>
        {
            int left = a != null ? a.level : int.MaxValue;
            int right = b != null ? b.level : int.MaxValue;
            return left.CompareTo(right);
        });
    }

    private LevelRewardDefinition CreateDefaultReward(int level)
    {
        LevelRewardDefinition definition = new LevelRewardDefinition
        {
            level = level,
            gold = 1000,
            diamond = 200,
            gemRewards = new List<LevelRewardGemEntry>()
        };

        int elementCount = GetGemElementCount();
        for (int elementId = 0; elementId < elementCount; elementId++)
        {
            definition.gemRewards.Add(new LevelRewardGemEntry
            {
                elementId = elementId,
                gemLevel = 1,
                quantity = 10
            });
        }

        return definition;
    }

    private int GetGemElementCount()
    {
        GemCollection gemCollection = GameDataManager.Instance != null
            ? GameDataManager.Instance.GemCollectionObject as GemCollection
            : null;

        if (gemCollection != null && gemCollection.elements != null && gemCollection.elements.Length > 0)
            return gemCollection.elements.Length;

        return 7;
    }

    private Sprite GetGemSprite(int elementId, int gemLevel)
    {
        GemCollection gemCollection = GameDataManager.Instance != null
            ? GameDataManager.Instance.GemCollectionObject as GemCollection
            : null;

        if (gemCollection == null || gemCollection.elements == null)
            return null;

        if (elementId < 0 || elementId >= gemCollection.elements.Length)
            return null;

        GemCollection.GemElementData elementData = gemCollection.elements[elementId];
        if (elementData == null || elementData.gemLevels == null)
            return null;

        int levelIndex = Mathf.Clamp(gemLevel, 1, 5) - 1;
        if (levelIndex < 0 || levelIndex >= elementData.gemLevels.Length)
            return null;

        GemCollection.GemLevelData levelData = elementData.gemLevels[levelIndex];
        return levelData != null ? levelData.sprite : null;
    }

    private string BuildGemDisplayName(int elementId, int gemLevel)
    {
        string elementName = GetGemElementName(elementId);
        string localizedElement = GetLocalizedText(elementName, elementName);
        return $"{localizedElement} Lv.{Mathf.Clamp(gemLevel, 1, 5)}";
    }

    private string GetGemElementName(int elementId)
    {
        GemCollection gemCollection = GameDataManager.Instance != null
            ? GameDataManager.Instance.GemCollectionObject as GemCollection
            : null;

        if (gemCollection == null || gemCollection.elements == null)
            return "Gem";

        if (elementId < 0 || elementId >= gemCollection.elements.Length)
            return "Gem";

        GemCollection.GemElementData elementData = gemCollection.elements[elementId];
        if (elementData == null || string.IsNullOrWhiteSpace(elementData.element))
            return "Gem";

        return elementData.element;
    }

    private string GetLocalizedText(string key, string fallback)
    {
        LocalizationManager lm = LocalizationManager.Instance;
        if (lm != null && lm.IsLoaded)
            return lm.GetText(key, fallback);

        return fallback;
    }
}
