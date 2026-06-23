using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuardianMapPopupUI : MonoBehaviour
{
    private static readonly string[] SpecialMapIds = { "149", "150", "151", "152", "153", "154", "155" };

    [Header("Home -> Popup")]
    [SerializeField] private Button openButton;
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button backButton;

    [Header("Map Buttons")]
    [SerializeField] private GuardianMapEntryItemUI[] mapButtons = new GuardianMapEntryItemUI[7];

    [Header("Prebattle")]
    [SerializeField] private PrebattlePopupUI prebattlePopup;

    [Header("Level Requirement Warning")]
    [SerializeField] private TextMeshProUGUI txtLevelRequirementWarning;
    [SerializeField] private float warningMoveY = 35f;
    [SerializeField] private float warningDuration = 3f;

    private CanvasGroup levelWarningCanvasGroup;
    private Tween levelWarningTween;
    private Vector2 levelWarningStartPos;

    private void Start()
    {
        BindButtons();
        EnsureReferences();
        SetupMapButtons();
        SetupLevelRequirementWarning();
        Close();
    }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged += RefreshMapButtons;

        EnsureReferences();
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged -= RefreshMapButtons;

        if (prebattlePopup != null)
        {
            prebattlePopup.OnOpened -= HandlePrebattleOpened;
            prebattlePopup.OnClosed -= HandlePrebattleClosed;
        }
    }

    private void OnDestroy()
    {
        levelWarningTween?.Kill();
        UnbindMapButtons();
    }

    public void Open()
    {
        EnsureReferences();
        if (popupRoot != null)
            popupRoot.SetActive(true);

        RefreshMapButtons();
    }

    public void Close()
    {
        HideLevelRequirementWarningImmediate();

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void BindButtons()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(Open);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Close);
        }
    }

    private void SetupMapButtons()
    {
        if (mapButtons == null)
            return;

        for (int i = 0; i < mapButtons.Length && i < SpecialMapIds.Length; i++)
        {
            GuardianMapEntryItemUI item = mapButtons[i];
            if (item == null)
                continue;

            item.OnMapClicked -= HandleMapClicked;
            item.OnMapClicked += HandleMapClicked;
            item.Setup(SpecialMapIds[i]);
        }
    }

    private void UnbindMapButtons()
    {
        if (mapButtons == null)
            return;

        for (int i = 0; i < mapButtons.Length; i++)
        {
            if (mapButtons[i] != null)
                mapButtons[i].OnMapClicked -= HandleMapClicked;
        }
    }

    private void RefreshMapButtons()
    {
        if (mapButtons == null)
            return;

        for (int i = 0; i < mapButtons.Length; i++)
        {
            if (mapButtons[i] != null)
                mapButtons[i].Refresh();
        }
    }

    private void HandleMapClicked(MapDataAsset map)
    {
        if (map == null)
            return;

        if (!MapLevelRequirementUIHelper.CanAccessMap(map))
        {
            ShowLevelRequirementWarning(map.reqUserLevel);
            return;
        }

        if (prebattlePopup == null)
            prebattlePopup = GetComponentInChildren<PrebattlePopupUI>(true);

        if (prebattlePopup != null)
            prebattlePopup.Open(map, PrebattlePopupUI.PrebattleOpenSource.GuardianMap);
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
        if (source != PrebattlePopupUI.PrebattleOpenSource.GuardianMap)
            return;

        HideLevelRequirementWarningImmediate();

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void HandlePrebattleClosed(PrebattlePopupUI.PrebattleOpenSource source)
    {
        if (source != PrebattlePopupUI.PrebattleOpenSource.GuardianMap)
            return;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        RefreshMapButtons();
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
}
