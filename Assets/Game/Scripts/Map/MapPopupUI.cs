using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class MapPopupUI : MonoBehaviour
{
    public static Button AdventureButtonStatic { get; private set; }
    public static Button FirstMapButtonStatic { get; private set; }
    [Header("Home -> Popup")]
    [SerializeField] private Button adventureButton;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button closePopupButton;
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private Vector2 shownPosition = Vector2.zero;
    [SerializeField] private Vector2 hiddenPosition = new Vector2(0f, -1800f);
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private Ease slideEase = Ease.OutCubic;

    [Header("Area Tabs")]
    [SerializeField] private Button[] areaButtons = new Button[7];
    [SerializeField] private GameObject[] areaRootPrefabs = new GameObject[7];
    [SerializeField] private Transform areaContentRoot;
    [SerializeField] private float areaFadeDuration = 0.2f;
    [SerializeField] private Color areaSelectedColor = new Color(1f, 0.9f, 0.3f, 1f);
    [SerializeField] private Color areaDefaultColor = Color.white;
    [SerializeField] private Image areaBackgroundImage;
    [SerializeField] private Sprite[] areaBackgroundSprites = new Sprite[7];

    [Header("Prebattle")]
    [SerializeField] private PrebattlePopupUI prebattlePopup;

    [Header("Level Requirement Warning")]
    [SerializeField] private TextMeshProUGUI txtLevelRequirementWarning;
    [SerializeField] private float warningMoveY = 35f;
    [SerializeField] private float warningDuration = 3f;

    private int currentArea = 0;
    private Tween slideTween;
    private Tween[] areaFadeTweens;
    private GameObject[] spawnedAreaRoots;
    private CanvasGroup[] spawnedAreaCanvasGroups;
    private readonly Dictionary<int, MapEntryItemUI[]> areaMapItemsCache = new Dictionary<int, MapEntryItemUI[]>();
    private Coroutine dataRefreshCoroutine;
    private bool mapPreviewVisible = true;
    private CanvasGroup levelWarningCanvasGroup;
    private Tween levelWarningTween;
    private Vector2 levelWarningStartPos;

    public float PopupSlideDuration => Mathf.Max(0.01f, slideDuration);

    private void Start()
    {
        AdventureButtonStatic = adventureButton;
        BindButtons();
        EnsureReferences();
        SetupLevelRequirementWarning();

        if (popupRoot != null)
            popupRoot.SetActive(true);

        if (popupRect == null && popupRoot != null)
            popupRect = popupRoot.GetComponent<RectTransform>();

        if (areaContentRoot == null)
            areaContentRoot = transform;

        int areaCount = areaRootPrefabs != null ? areaRootPrefabs.Length : 0;
        spawnedAreaRoots = new GameObject[areaCount];
        spawnedAreaCanvasGroups = new CanvasGroup[areaCount];
        areaFadeTweens = new Tween[areaCount];

        EnsureAreaSpawned(0);
        ShowArea(0, true);
        UpdateAreaButtonVisuals(0);
        UpdateAreaBackground(0);

        SetMapPopupVisible(false, true);
        SetMapItemsPreviewVisible(false);

        RefreshAllMapItems();
        RequestDataRefresh();
    }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged += HandlePlayerDataChanged;
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged -= HandlePlayerDataChanged;

        if (dataRefreshCoroutine != null)
        {
            StopCoroutine(dataRefreshCoroutine);
            dataRefreshCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (slideTween != null)
            slideTween.Kill();

        if (areaFadeTweens != null)
        {
            for (int i = 0; i < areaFadeTweens.Length; i++)
            {
                if (areaFadeTweens[i] != null)
                    areaFadeTweens[i].Kill();
            }
        }

        levelWarningTween?.Kill();

        if (prebattlePopup != null)
        {
            prebattlePopup.OnOpened -= HandlePrebattleOpened;
            prebattlePopup.OnClosed -= HandlePrebattleClosed;
        }
    }

    private void BindButtons()
    {
        if (adventureButton != null)
        {
            adventureButton.onClick.RemoveAllListeners();
            adventureButton.onClick.AddListener(HandleAdventureButtonClicked);
        }

        if (closePopupButton != null)
        {
            closePopupButton.onClick.RemoveAllListeners();
            closePopupButton.onClick.AddListener(ClosePopup);
        }

        for (int i = 0; i < areaButtons.Length; i++)
        {
            Button btn = areaButtons[i];
            if (btn == null)
                continue;

            int areaIndex = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectArea(areaIndex));
        }
    }

    private void HandleAdventureButtonClicked()
    {
        if (TutorialProgressManager.Instance != null)
            TutorialProgressManager.Instance.NotifyAdventureButtonClicked();

        OpenPopup();
        CacheFirstMapButton();
    }

    public void OpenPopup()
    {
        EnsureReferences();
        SetMapPopupVisible(true);

        RefreshAllMapItems();
        SelectAreaInternal(0, true, true);
        SetMapItemsPreviewVisible(true);
        RefreshVisibleAreaMapItems();
        RequestDataRefresh();
    }

    public void ClosePopup()
    {
        SetMapPopupVisible(false);
        SetMapItemsPreviewVisible(false);
        HideLevelRequirementWarningImmediate();
    }

    public void SelectArea(int areaIndex)
    {
        SelectAreaInternal(areaIndex, false, false);
    }

    private void HandlePlayerDataChanged()
    {
        RefreshAllMapItems();
        RefreshVisibleAreaMapItems();
    }

    private void HandleMapClicked(MapDataAsset mapData)
    {
        if (mapData == null)
        {
            Debug.LogWarning("[MapPopupUI] Map click received but mapData is null.");
            return;
        }

        if (!MapLevelRequirementUIHelper.CanAccessMap(mapData))
        {
            ShowLevelRequirementWarning(mapData.reqUserLevel);
            return;
        }

        if (prebattlePopup == null)
            prebattlePopup = GetComponentInChildren<PrebattlePopupUI>(true);

        if (prebattlePopup != null)
        {
            CacheFirstMapButton();
            SetMapItemsPreviewVisible(false);
            SetMapPopupVisible(false);

            if (TutorialProgressManager.Instance != null)
                TutorialProgressManager.Instance.NotifyFirstMapClicked();

            prebattlePopup.Open(mapData);
        }
        else
            Debug.LogWarning("[MapPopupUI] Cannot open prebattle popup because PrebattlePopupUI reference is missing.");
    }

    private void CacheFirstMapButton()
    {
        if (!areaMapItemsCache.TryGetValue(currentArea, out MapEntryItemUI[] items) || items == null || items.Length == 0)
            return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null || items[i].MapButton == null)
                continue;

            FirstMapButtonStatic = items[i].MapButton;
            return;
        }
    }
    private void EnsureReferences()
    {
        if (prebattlePopup == null)
            prebattlePopup = GetComponentInChildren<PrebattlePopupUI>(true);

        if (prebattlePopup != null)
        {
            prebattlePopup.OnOpened -= HandlePrebattleOpened;
            prebattlePopup.OnOpened += HandlePrebattleOpened;
            prebattlePopup.OnClosed -= HandlePrebattleClosed;
            prebattlePopup.OnClosed += HandlePrebattleClosed;
        }
    }

    private void HandlePrebattleOpened(PrebattlePopupUI.PrebattleOpenSource source)
    {
        if (source != PrebattlePopupUI.PrebattleOpenSource.MapPopup)
            return;

        SetMapItemsPreviewVisible(false);
        HideLevelRequirementWarningImmediate();
    }

    private void HandlePrebattleClosed(PrebattlePopupUI.PrebattleOpenSource source)
    {
        if (source != PrebattlePopupUI.PrebattleOpenSource.MapPopup)
            return;

        SetMapItemsPreviewVisible(true);
        RefreshVisibleAreaMapItems();
    }

    private void SetupLevelRequirementWarning()
    {
        if (txtLevelRequirementWarning == null)
            return;

        levelWarningCanvasGroup = txtLevelRequirementWarning.GetComponent<CanvasGroup>();
        if (levelWarningCanvasGroup == null)
            levelWarningCanvasGroup = txtLevelRequirementWarning.gameObject.AddComponent<CanvasGroup>();

        RectTransform rectTransform = txtLevelRequirementWarning.rectTransform;
        if (rectTransform != null)
            levelWarningStartPos = rectTransform.anchoredPosition;

        HideLevelRequirementWarningImmediate();
    }

    private void ShowLevelRequirementWarning(int requiredLevel)
    {
        if (txtLevelRequirementWarning == null)
            return;

        if (levelWarningCanvasGroup == null)
            SetupLevelRequirementWarning();

        levelWarningTween?.Kill();

        txtLevelRequirementWarning.text = MapLevelRequirementUIHelper.GetLevelRequirementText(requiredLevel);
        txtLevelRequirementWarning.gameObject.SetActive(true);

        RectTransform rectTransform = txtLevelRequirementWarning.rectTransform;
        if (rectTransform != null)
            rectTransform.anchoredPosition = levelWarningStartPos;

        levelWarningCanvasGroup.alpha = 1f;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        if (rectTransform != null)
            sequence.Join(rectTransform.DOAnchorPosY(levelWarningStartPos.y + warningMoveY, warningDuration).SetEase(Ease.OutQuad));
        sequence.Join(levelWarningCanvasGroup.DOFade(0f, warningDuration).SetEase(Ease.OutQuad));
        sequence.OnComplete(() =>
        {
            if (txtLevelRequirementWarning != null)
                txtLevelRequirementWarning.gameObject.SetActive(false);
        });

        levelWarningTween = sequence;
    }

    private void HideLevelRequirementWarningImmediate()
    {
        levelWarningTween?.Kill();

        if (txtLevelRequirementWarning == null)
            return;

        if (levelWarningCanvasGroup == null)
            levelWarningCanvasGroup = txtLevelRequirementWarning.GetComponent<CanvasGroup>();

        RectTransform rectTransform = txtLevelRequirementWarning.rectTransform;
        if (rectTransform != null)
            rectTransform.anchoredPosition = levelWarningStartPos;

        if (levelWarningCanvasGroup != null)
            levelWarningCanvasGroup.alpha = 0f;

        txtLevelRequirementWarning.gameObject.SetActive(false);
    }

    private void SetMapPopupVisible(bool visible, bool instant = false)
    {
        if (popupRoot == null)
            return;

        if (popupRect == null)
            popupRect = popupRoot.GetComponent<RectTransform>();

        if (popupRect == null)
        {
            popupRoot.SetActive(visible);
            return;
        }

        popupRoot.SetActive(true);
        slideTween?.Kill();

        Vector2 targetPosition = visible ? shownPosition : hiddenPosition;
        if (instant)
        {
            popupRect.anchoredPosition = targetPosition;
            if (!visible)
                popupRoot.SetActive(false);
            return;
        }

        slideTween = popupRect.DOAnchorPos(targetPosition, Mathf.Max(0.01f, slideDuration)).SetEase(slideEase).SetUpdate(true);
        if (!visible)
        {
            slideTween.OnComplete(() =>
            {
                if (popupRoot != null)
                    popupRoot.SetActive(false);
            });
        }
    }

    private void SelectAreaInternal(int areaIndex, bool instant, bool forceRefresh)
    {
        if (areaRootPrefabs == null || areaRootPrefabs.Length == 0)
            return;

        int clampedIndex = Mathf.Clamp(areaIndex, 0, areaRootPrefabs.Length - 1);
        if (!forceRefresh && currentArea == clampedIndex && spawnedAreaRoots != null && spawnedAreaRoots[clampedIndex] != null)
            return;

        EnsureAreaSpawned(clampedIndex);

        for (int i = 0; i < spawnedAreaRoots.Length; i++)
        {
            if (spawnedAreaRoots[i] == null)
                continue;

            bool visible = i == clampedIndex;
            ShowArea(i, visible, instant);
            SetAreaPreviewVisible(i, visible && mapPreviewVisible);
        }

        currentArea = clampedIndex;
        UpdateAreaButtonVisuals(currentArea);
        UpdateAreaBackground(currentArea);

        if (forceRefresh)
            RefreshAreaMapItems(currentArea);
    }

    private void RequestDataRefresh()
    {
        if (dataRefreshCoroutine != null)
            StopCoroutine(dataRefreshCoroutine);

        dataRefreshCoroutine = StartCoroutine(CoRequestDataRefresh());
    }

    private System.Collections.IEnumerator CoRequestDataRefresh()
    {
        yield return null;
        RefreshAllMapItems();
        RefreshVisibleAreaMapItems();
        dataRefreshCoroutine = null;
    }

    private void RefreshAllMapItems()
    {
        if (spawnedAreaRoots == null)
            return;

        for (int i = 0; i < spawnedAreaRoots.Length; i++)
        {
            if (spawnedAreaRoots[i] == null)
                continue;

            RefreshAreaMapItems(i);
        }
    }

    private void SetMapItemsPreviewVisible(bool visible)
    {
        mapPreviewVisible = visible;

        if (spawnedAreaRoots == null)
            return;

        for (int i = 0; i < spawnedAreaRoots.Length; i++)
        {
            if (!areaMapItemsCache.TryGetValue(i, out MapEntryItemUI[] areaItems) || areaItems == null)
                continue;

            for (int j = 0; j < areaItems.Length; j++)
            {
                MapEntryItemUI item = areaItems[j];
                if (item != null)
                    item.SetPreviewVisible(visible);
            }
        }
    }

    private void EnsureAreaSpawned(int areaIndex)
    {
        if (areaIndex < 0 || areaIndex >= spawnedAreaRoots.Length)
            return;

        if (spawnedAreaRoots[areaIndex] != null)
            return;

        GameObject prefab = areaRootPrefabs[areaIndex];
        if (prefab == null)
            return;

        GameObject instance = Instantiate(prefab, areaContentRoot);
        instance.name = prefab.name;
        instance.SetActive(false);
        spawnedAreaRoots[areaIndex] = instance;

        CanvasGroup cg = instance.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = instance.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        spawnedAreaCanvasGroups[areaIndex] = cg;

        MapEntryItemUI[] areaItems = instance.GetComponentsInChildren<MapEntryItemUI>(true);
        areaMapItemsCache[areaIndex] = areaItems;

        for (int i = 0; i < areaItems.Length; i++)
        {
            MapEntryItemUI item = areaItems[i];
            if (item == null)
                continue;

            item.OnMapClicked -= HandleMapClicked;
            item.OnMapClicked += HandleMapClicked;
        }

        RefreshAreaMapItems(areaIndex);
        SetAreaPreviewVisible(areaIndex, false);
    }

    private void ShowArea(int areaIndex, bool visible, bool instant = false)
    {
        if (areaIndex < 0 || areaIndex >= spawnedAreaRoots.Length)
            return;

        GameObject root = spawnedAreaRoots[areaIndex];
        CanvasGroup cg = spawnedAreaCanvasGroups[areaIndex];
        if (root == null || cg == null)
            return;

        if (areaFadeTweens != null && areaFadeTweens[areaIndex] != null)
            areaFadeTweens[areaIndex].Kill();

        root.SetActive(true);

        float target = visible ? 1f : 0f;
        cg.interactable = visible;
        cg.blocksRaycasts = visible;

        if (instant)
        {
            cg.alpha = target;
            if (!visible)
                root.SetActive(false);
            return;
        }

        areaFadeTweens[areaIndex] = cg.DOFade(target, Mathf.Max(0.01f, areaFadeDuration)).SetUpdate(true);
        if (!visible)
        {
            areaFadeTweens[areaIndex].OnComplete(() =>
            {
                if (root != null)
                    root.SetActive(false);
            });
        }
    }

    private void UpdateAreaButtonVisuals(int selectedIndex)
    {
        for (int i = 0; i < areaButtons.Length; i++)
        {
            Button button = areaButtons[i];
            if (button == null)
                continue;

            Image image = button.GetComponent<Image>();
            if (image == null)
                continue;

            image.color = i == selectedIndex ? areaSelectedColor : areaDefaultColor;
        }
    }

    private void UpdateAreaBackground(int selectedIndex)
    {
        if (areaBackgroundImage == null || areaBackgroundSprites == null)
            return;

        if (selectedIndex < 0 || selectedIndex >= areaBackgroundSprites.Length)
            return;

        Sprite sprite = areaBackgroundSprites[selectedIndex];
        if (sprite == null)
            return;

        areaBackgroundImage.sprite = sprite;
    }

    private void RefreshAreaMapItems(int areaIndex)
    {
        if (!areaMapItemsCache.TryGetValue(areaIndex, out MapEntryItemUI[] areaItems) || areaItems == null)
            return;

        for (int i = 0; i < areaItems.Length; i++)
        {
            MapEntryItemUI item = areaItems[i];
            if (item == null)
                continue;

            try
            {
                item.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MapPopupUI] Error refreshing map item {i} in area {areaIndex}: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }

    private void RefreshVisibleAreaMapItems()
    {
        RefreshAreaMapItems(currentArea);
    }

    private void SetAreaPreviewVisible(int areaIndex, bool visible)
    {
        if (!areaMapItemsCache.TryGetValue(areaIndex, out MapEntryItemUI[] areaItems) || areaItems == null)
            return;

        for (int i = 0; i < areaItems.Length; i++)
        {
            MapEntryItemUI item = areaItems[i];
            if (item != null)
                item.SetPreviewVisible(visible);
        }
    }
}









