using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DG.Tweening;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuardianPopupUI : MonoBehaviour
{
    [Serializable]
    public class GuardianButtonSlot
    {
        public Button button;
        public Image targetImage;
        public Sprite selectedSprite;
        public Sprite unselectedSprite;
        public Color selectedColor = new Color(0.2f, 0.9f, 0.2f, 1f);
        public Color unselectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    }

    [Header("Popup Root")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private float fadeDuration = 0.25f;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Guardian List")]
    [SerializeField] private GuardianButtonSlot[] guardianButtons;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private TextMeshProUGUI txtDescription;
    [SerializeField] private TextMeshProUGUI txtStory;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtRecruitCost;
    [SerializeField] private TextMeshProUGUI txtUpgradeCost;

    [Header("Guardian Preview")]
    [SerializeField] private Transform guardianPreviewRoot;
    [SerializeField] private Vector3 guardianPreviewScale = Vector3.one;

    [Header("Actions")]
    [SerializeField] private Button btnRecruit;
    [SerializeField] private Button btnUpgrade;

    [Header("Notify")]
    [SerializeField] private TextMeshProUGUI txtNotify;
    [SerializeField] private float notifyMoveY = 60f;
    [SerializeField] private float notifyDuration = 2.5f;

    [Header("Data")]
    [SerializeField] private GuardianDatabase guardianDatabase;
    [SerializeField] private int defaultGuardianId = -1;

    private readonly List<GuardianDataAsset> guardians = new List<GuardianDataAsset>();
    private int selectedGuardianId = -1;
    private Tween popupTween;
    private Sequence notifySequence;
    private Vector3 notifyStartPos;
    private GameObject spawnedGuardianPreview;
    private bool isPopupVisible;

    private const string NotEnoughDiamondKey = "guardian_not_enough_diamond";
    private const string UpgradeCapKey = "guardian_upgrade_level_cap";
    private const string UpgradeMaxKey = "guardian_upgrade_max_level";
    private const string GuardianNotOwnedKey = "guardian_not_owned";

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (popupCanvasGroup == null && popupRoot != null)
            popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();

        if (popupCanvasGroup == null && popupRoot != null)
            popupCanvasGroup = popupRoot.AddComponent<CanvasGroup>();

        if (openButton != null)
        {
            openButton.onClick.RemoveAllListeners();
            openButton.onClick.AddListener(OpenPopup);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePopup);
        }

        if (btnRecruit != null)
        {
            btnRecruit.onClick.RemoveAllListeners();
        }

        if (btnUpgrade != null)
        {
            btnUpgrade.onClick.RemoveAllListeners();
            btnUpgrade.onClick.AddListener(OnUpgradeClicked);
        }

        if (txtNotify != null)
            notifyStartPos = txtNotify.rectTransform.localPosition;

        if (guardianPreviewRoot == null)
            guardianPreviewRoot = transform;

        SetPopupVisible(false, true);
    }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged += RefreshSelectedGuardian;

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLocalizationLoaded += RefreshSelectedGuardian;
            LocalizationManager.Instance.OnLanguageChanged += RefreshSelectedGuardian;
        }
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged -= RefreshSelectedGuardian;

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLocalizationLoaded -= RefreshSelectedGuardian;
            LocalizationManager.Instance.OnLanguageChanged -= RefreshSelectedGuardian;
        }
    }

    private void Start()
    {
        TryResolveDatabase();
        BuildGuardianButtons();
        UpdateButtonVisuals();
    }

    private void TryResolveDatabase()
    {
        if (guardianDatabase != null)
            return;

        if (GameDataManager.Instance != null)
            guardianDatabase = GameDataManager.Instance.GuardianDatabase;
    }

    private void BuildGuardianButtons()
    {
        guardians.Clear();
        if (guardianDatabase != null && guardianDatabase.guardians != null)
            guardians.AddRange(guardianDatabase.guardians.FindAll(g => g != null));

        if (guardianButtons == null || guardianButtons.Length == 0)
            return;

        for (int i = 0; i < guardianButtons.Length; i++)
        {
            GuardianButtonSlot slot = guardianButtons[i];
            if (slot == null || slot.button == null)
                continue;

            slot.button.onClick.RemoveAllListeners();

            GuardianDataAsset guardian = i < guardians.Count ? guardians[i] : null;
            if (guardian == null)
            {
                slot.button.gameObject.SetActive(false);
                continue;
            }

            slot.button.gameObject.SetActive(true);
            int guardianId = guardian.guardianId;
            slot.button.onClick.AddListener(() => SelectGuardian(guardianId));

        }

        UpdateButtonVisuals();
    }

    private void SelectDefaultGuardian()
    {
        if (defaultGuardianId > 0)
        {
            SelectGuardian(defaultGuardianId);
            return;
        }

        if (guardians.Count > 0)
            SelectGuardian(guardians[0].guardianId);
    }

    private void SelectGuardian(int guardianId)
    {
        selectedGuardianId = guardianId;
        RefreshSelectedGuardian();
        UpdateButtonVisuals();
    }

    private void UpdateButtonVisuals()
    {
        if (guardianButtons == null || guardianButtons.Length == 0)
            return;

        for (int i = 0; i < guardianButtons.Length; i++)
        {
            GuardianButtonSlot slot = guardianButtons[i];
            if (slot == null || slot.button == null)
                continue;

            GuardianDataAsset guardian = i < guardians.Count ? guardians[i] : null;
            if (guardian == null)
                continue;

            bool isSelected = guardian.guardianId == selectedGuardianId;
            ApplyButtonVisual(slot, isSelected);
        }
    }

    private void ApplyButtonVisual(GuardianButtonSlot slot, bool isSelected)
    {
        Image targetImage = slot.targetImage != null
            ? slot.targetImage
            : slot.button.targetGraphic as Image;

        if (targetImage == null)
            return;

        targetImage.color = isSelected ? slot.selectedColor : slot.unselectedColor;
        Sprite sprite = isSelected ? slot.selectedSprite : slot.unselectedSprite;
        if (sprite != null)
            targetImage.sprite = sprite;
    }

    public void OpenPopup()
    {
        SetPopupVisible(true, false);
        EnsureGuardianSelection();
    }

    public void ClosePopup()
    {
        SetPopupVisible(false, false);
        ClearGuardianPreview();
    }

    private void SetPopupVisible(bool visible, bool instant)
    {
        if (popupRoot == null)
            return;

        isPopupVisible = visible;

        if (popupTween != null)
            popupTween.Kill();

        if (visible)
        {
            popupRoot.SetActive(true);
            popupCanvasGroup.alpha = instant ? 1f : 0f;
            popupCanvasGroup.blocksRaycasts = true;
            popupCanvasGroup.interactable = true;

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

    private void RefreshSelectedGuardian()
    {
        if (!isPopupVisible)
            return;

        GuardianDataAsset guardian = GetSelectedGuardian();
        if (guardian == null)
        {
            SetText(txtName, string.Empty);
            SetText(txtDescription, string.Empty);
            SetText(txtStory, string.Empty);
            SetText(txtLevel, string.Empty);
            SetText(txtRecruitCost, string.Empty);
            SetText(txtUpgradeCost, string.Empty);
            ClearGuardianPreview();
            return;
        }

        int currentLevel = GetCurrentGuardianLevel(guardian.guardianId);
        GuardianLevelData levelData = guardian.GetLevelData(currentLevel);

        string nameText = GetLocalizedText(guardian.guardianName, guardian.guardianName);
        string descText = GetLocalizedText(guardian.description, guardian.description);
        string storyText = GetLocalizedText(guardian.story, guardian.story);

        SetText(txtName, nameText);
        SetText(txtDescription, FormatDescription(descText, levelData, guardian.element));
        SetText(txtStory, storyText);

        string levelLabel = GetLocalizedText("level", "Level");
        SetText(txtLevel, levelLabel + " " + currentLevel);

        SpawnGuardianPreview(guardian.guardianPrefab);

        UpdateActionButtons(guardian, currentLevel);
    }

    private void UpdateActionButtons(GuardianDataAsset guardian, int currentLevel)
    {
        bool isOwned = PlayerManager.Instance != null && PlayerManager.Instance.IsGuardianOwned(guardian.guardianId);

        if (btnRecruit != null)
            btnRecruit.gameObject.SetActive(false);
        if (txtRecruitCost != null)
            txtRecruitCost.gameObject.SetActive(false);

        if (btnUpgrade != null)
            btnUpgrade.gameObject.SetActive(isOwned);
        if (txtUpgradeCost != null)
            txtUpgradeCost.gameObject.SetActive(isOwned);

        if (!isOwned)
        {
            SetText(txtUpgradeCost, string.Empty);
            return;
        }

        int playerLevel = PlayerManager.Instance != null && PlayerManager.Instance.Data != null
            ? Mathf.Max(1, PlayerManager.Instance.Data.level)
            : 1;

        int guardianMax = guardian.GetMaxLevel();
        int allowedMax = Mathf.Min(guardianMax, playerLevel);
        int upgradeCost = guardian.GetUpgradeCost(currentLevel);

        if (currentLevel >= guardianMax)
        {
            SetText(txtUpgradeCost, GetLocalizedText("guardian_max_level", "MAX"));
        }
        else if (currentLevel >= playerLevel)
        {
            SetText(txtUpgradeCost, GetLocalizedText("guardian_level_cap", "LOCK"));
        }
        else
        {
            SetText(txtUpgradeCost, upgradeCost.ToString());
        }
    }

    private GuardianDataAsset GetSelectedGuardian()
    {
        if (guardianDatabase == null || guardianDatabase.guardians == null)
            return null;

        return guardianDatabase.GetGuardianById(selectedGuardianId);
    }

    private int GetCurrentGuardianLevel(int guardianId)
    {
        if (PlayerManager.Instance == null)
            return 1;

        if (!PlayerManager.Instance.IsGuardianOwned(guardianId))
            return 1;

        return Mathf.Max(1, PlayerManager.Instance.GetGuardianLevel(guardianId));
    }

    private void OnRecruitClicked()
    {
        // Recruit is disabled; guardians are earned from special maps.
        ShowNotify(GetLocalizedText("guardian_unlock_map", "Unlock by conquering special maps."));
    }

    private void OnUpgradeClicked()
    {
        GuardianDataAsset guardian = GetSelectedGuardian();
        if (guardian == null || PlayerManager.Instance == null)
            return;

        if (!PlayerManager.Instance.IsGuardianOwned(guardian.guardianId))
        {
            ShowNotify(GetLocalizedText(GuardianNotOwnedKey, "Guardian not owned."));
            return;
        }

        int currentLevel = PlayerManager.Instance.GetGuardianLevel(guardian.guardianId);
        int guardianMax = guardian.GetMaxLevel();
        int playerLevel = PlayerManager.Instance.Data != null ? Mathf.Max(1, PlayerManager.Instance.Data.level) : 1;

        if (currentLevel >= guardianMax)
        {
            ShowNotify(GetLocalizedText(UpgradeMaxKey, "Guardian is at max level."));
            return;
        }

        if (currentLevel >= playerLevel)
        {
            ShowNotify(GetLocalizedText(UpgradeCapKey, "Cannot exceed player level."));
            return;
        }

        int allowedMax = Mathf.Min(guardianMax, playerLevel);
        int cost = guardian.GetUpgradeCost(currentLevel);

        if (!PlayerManager.Instance.TryUpgradeGuardian(guardian.guardianId, cost, allowedMax))
        {
            ShowNotify(GetLocalizedText(NotEnoughDiamondKey, "Not enough diamond."));
            return;
        }

        PlayerManager.Instance.SaveData();
        RefreshSelectedGuardian();
    }

    private void ShowNotify(string message)
    {
        if (txtNotify == null)
            return;

        txtNotify.gameObject.SetActive(true);
        txtNotify.rectTransform.localPosition = notifyStartPos;
        txtNotify.text = message;

        CanvasGroup cg = txtNotify.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = txtNotify.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        if (notifySequence != null)
            notifySequence.Kill();

        notifySequence = DOTween.Sequence();
        notifySequence.Join(txtNotify.rectTransform.DOLocalMoveY(notifyStartPos.y + notifyMoveY, notifyDuration).SetEase(Ease.OutCubic));
        notifySequence.Join(cg.DOFade(0f, notifyDuration));
        notifySequence.SetUpdate(true);
        notifySequence.OnComplete(() =>
        {
            if (txtNotify != null)
                txtNotify.gameObject.SetActive(false);
        });
    }

    private string FormatDescription(string template, GuardianLevelData levelData, GuardianElement element)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        string colorHex = GetColorHex(element);
        string[] values =
        {
            FormatValue(levelData != null ? levelData.value1 : 0f),
            FormatValue(levelData != null ? levelData.value2 : 0f),
            FormatValue(levelData != null ? levelData.value3 : 0f)
        };

        int valueIndex = 0;
        StringBuilder sb = new StringBuilder(template.Length + 32);

        for (int i = 0; i < template.Length; i++)
        {
            char ch = template[i];
            if (ch == '$')
            {
                string value = values[Mathf.Clamp(valueIndex, 0, values.Length - 1)];
                sb.Append("<color=").Append(colorHex).Append(">").Append(value).Append("</color>");
                valueIndex++;
            }
            else
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    private string GetColorHex(GuardianElement element)
    {
        switch (element)
        {
            case GuardianElement.Leaf:
                return "#6FFF00";
            case GuardianElement.Fire:
                return "#FF5A3C";
            case GuardianElement.Metal:
                return "#FFD24D";
            case GuardianElement.Earth:
                return "#C28B4E";
            case GuardianElement.Water:
                return "#3FB7FF";
            case GuardianElement.Dark:
                return "#8E5BFF";
            case GuardianElement.Light:
                return "#FFF2A3";
            default:
                return "#FFFFFF";
        }
    }

    private string FormatValue(float value)
    {
        float rounded = Mathf.Round(value);
        if (Mathf.Abs(value - rounded) < 0.001f)
            return rounded.ToString(CultureInfo.InvariantCulture);

        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private string GetLocalizedText(string keyOrText, string fallback)
    {
        if (string.IsNullOrWhiteSpace(keyOrText))
            return fallback;

        LocalizationManager lm = LocalizationManager.Instance;
        if (lm != null && lm.IsLoaded)
            return lm.GetText(keyOrText, fallback);

        return fallback;
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target == null)
            return;

        target.text = value ?? string.Empty;
    }

    private void SpawnGuardianPreview(GameObject prefab)
    {
        ClearGuardianPreview();
        if (prefab == null || guardianPreviewRoot == null)
            return;

        spawnedGuardianPreview = Instantiate(prefab, guardianPreviewRoot);
        spawnedGuardianPreview.transform.localPosition = Vector3.zero;
        spawnedGuardianPreview.transform.localRotation = Quaternion.identity;
        spawnedGuardianPreview.transform.localScale = guardianPreviewScale;

        PlayIdle(spawnedGuardianPreview);
    }

    private void ClearGuardianPreview()
    {
        if (spawnedGuardianPreview != null)
            Destroy(spawnedGuardianPreview);

        spawnedGuardianPreview = null;
    }

    private void PlayIdle(GameObject target)
    {
        if (target == null)
            return;

        Animator animator = target.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            if (animator.HasState(0, Animator.StringToHash("Idle")))
                animator.Play("Idle", 0, 0f);
            return;
        }

        SkeletonAnimation skeleton = target.GetComponentInChildren<SkeletonAnimation>(true);
        if (skeleton != null && skeleton.state != null)
            skeleton.state.SetAnimation(0, "Idle", true);
    }

    private void EnsureGuardianSelection()
    {
        if (selectedGuardianId > 0)
        {
            RefreshSelectedGuardian();
            UpdateButtonVisuals();
            return;
        }

        if (defaultGuardianId > 0)
        {
            SelectGuardian(defaultGuardianId);
            return;
        }

        if (guardians.Count > 0)
            SelectGuardian(guardians[0].guardianId);
    }
}

