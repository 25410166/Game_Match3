using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoosePet : MonoBehaviour
{
    [Header("UI - C?p hi?n t?i")]
    [SerializeField] private TextMeshProUGUI _txtLevel;
    [SerializeField] private TextMeshProUGUI _txtHP;
    [SerializeField] private TextMeshProUGUI _txtAtk;
    [SerializeField] private TextMeshProUGUI _txtMana;
    [SerializeField] private TextMeshProUGUI _txtRage;
    [SerializeField] private TextMeshProUGUI _txtArmor;
    [SerializeField] private TextMeshProUGUI _txtCrit; // Crit Rate
    [SerializeField] private TextMeshProUGUI _txtCritDamage;

    [Header("UI - C?p k? ti?p")]
    [SerializeField] private TextMeshProUGUI _txtLevelNext;
    [SerializeField] private TextMeshProUGUI _txtHPNext;
    [SerializeField] private TextMeshProUGUI _txtAtkNext;
    [SerializeField] private TextMeshProUGUI _txtManaNext;
    [SerializeField] private TextMeshProUGUI _txtRageNext;
    [SerializeField] private TextMeshProUGUI _txtArmorNext;
    [SerializeField] private TextMeshProUGUI _txtCritNext; // Crit Rate
    [SerializeField] private TextMeshProUGUI _txtCritDamageNext;

    [Header("UI - Thông tin Pet")]
    [SerializeField] private TextMeshProUGUI _txtPetName;
    [SerializeField] private Image _elementIcon;
    [SerializeField] private Sprite[] _elementSprite;

    [Header("UI - Skill")]
    [SerializeField] private GameObject _skillInfoRoot;
    [SerializeField] private TextMeshProUGUI _txtSkillName;
    [SerializeField] private TextMeshProUGUI _txtSkillDescription;

    [Header("Popup Training")]
    [SerializeField] private GameObject _trainingPopupRoot;
    [SerializeField] private Button _openPopupButton;
    [SerializeField] private Button _closePopupButton;
    [SerializeField] private Button _changePetButton;

    [Header("Popup Select Pet")]
    [SerializeField] private PopupSelectPet _popupSelectPet;

    [Header("V? trí spawn Pet")]
    [SerializeField] private Transform _petSpawnPoint;
    [SerializeField] private string _petSortingLayerName = "UI";
    [SerializeField] private int _petSortingOrder = 10;

    [Header("Nâng c?p")]
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private GemUpdate gemUpdate;
    [SerializeField] private Transform _upgradeFxSpawnPoint;
    [SerializeField] private GameObject _upgradeInProgressFxPrefab;
    [SerializeField] private GameObject _upgradeSuccessFxPrefab;
    [SerializeField] private GameObject _upgradeFailFxPrefab;
    [SerializeField] private float _upgradeFxWaitSeconds = 2f;
    [SerializeField] private float _resultFxLifetimeSeconds = 2f;

    [Header("Popup k?t qu?")]
    [SerializeField] private GameObject _upgradeSuccessPopup;
    [SerializeField] private GameObject _upgradeFailPopup;
    [SerializeField] private TextMeshProUGUI _txtUpgradeSuccess;
    [SerializeField] private TextMeshProUGUI _txtUpgradeFail;
    [SerializeField] private float _resultPopupAutoCloseSeconds = 3f;

    private GameObject currentPetInstance;
    private string currentPetName = string.Empty;
    private string currentPetElement = string.Empty;
    private int currentPetMaxLevel = 1;
    private int currentLevel = 1;
    private int currentPetId = -1;
    private Coroutine _autoCloseResultCoroutine;
    private Coroutine _upgradeFlowCoroutine;
    private bool _selectPetPopupOpen = false;
    private bool _isUpgradeInProgress = false;

    private void Awake()
    {
        if (_openPopupButton != null)
            _openPopupButton.onClick.AddListener(OpenPopup);
        if (_closePopupButton != null)
            _closePopupButton.onClick.AddListener(ClosePopup);
        if (_changePetButton != null)
            _changePetButton.onClick.AddListener(OpenSelectPetPopup);
        if (_upgradeButton != null)
            _upgradeButton.onClick.AddListener(OnUpgradeButtonClick);
    }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged += OnPlayerDataChanged;

        RegisterLocalizationEvents();
    }

    private void Start()
    {
        if (_upgradeButton != null)
            _upgradeButton.gameObject.SetActive(false);

        ResetStatTextsToDefault();
        HideUpgradeResultPopups();
        ClosePopup();
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged -= OnPlayerDataChanged;

        UnregisterLocalizationEvents();
    }

    private void RegisterLocalizationEvents()
    {
        if (LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLocalizationLoaded -= HandleLocalizationChanged;
        LocalizationManager.Instance.OnLanguageChanged -= HandleLocalizationChanged;
        LocalizationManager.Instance.OnLocalizationLoaded += HandleLocalizationChanged;
        LocalizationManager.Instance.OnLanguageChanged += HandleLocalizationChanged;
    }

    private void UnregisterLocalizationEvents()
    {
        if (LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLocalizationLoaded -= HandleLocalizationChanged;
        LocalizationManager.Instance.OnLanguageChanged -= HandleLocalizationChanged;
    }

    private void HandleLocalizationChanged()
    {
        UpdatePetUI();
    }

    public void OpenPopup()
    {
        GameObject root = _trainingPopupRoot != null ? _trainingPopupRoot : gameObject;
        root.SetActive(true);

        HideUpgradeResultPopups();
        AutoSelectFirstOwnedPetIfNeeded();
    }

    public void ClosePopup()
    {
        HideUpgradeResultPopups();
        CloseSelectPetPopup();

        if (currentPetInstance != null)
        {
            Destroy(currentPetInstance);
            currentPetInstance = null;
        }

        currentPetId = -1;
        currentPetName = string.Empty;
        currentPetElement = string.Empty;
        currentPetMaxLevel = 1;
        currentLevel = 1;
        ResetStatTextsToDefault();

        if (_upgradeButton != null)
            _upgradeButton.gameObject.SetActive(false);

        GameObject root = _trainingPopupRoot != null ? _trainingPopupRoot : gameObject;
        root.SetActive(false);
    }

    public void OpenSelectPetPopup()
    {
        if (_popupSelectPet != null)
        {
            EnsureTrainingPetSorting();

            if (!_selectPetPopupOpen)
            {
                _popupSelectPet.Open(OnSelectOwnedPet);
                _selectPetPopupOpen = true;
            }
            else
            {
                _popupSelectPet.Close();
                _selectPetPopupOpen = false;
            }
        }
    }

    public void CloseSelectPetPopup()
    {
        if (_popupSelectPet != null)
            _popupSelectPet.Close();
        _selectPetPopupOpen = false;
    }

    public void OnSelectOwnedPet(int petId)
    {
        if (PlayerManager.Instance == null)
            return;

        OwnedPetData ownedPet = PlayerManager.Instance.GetOwnedPet(petId);
        if (ownedPet == null)
            return;

        GameObject prefab = ResolvePetPrefabForLevel(petId, ownedPet.petLevel, ownedPet.petName);
        if (prefab == null)
            return;

        ShowPetInternal(petId, prefab);
        CloseSelectPetPopup();
    }

    public void ShowPet(GameObject petPrefab)
    {
        ShowPetInternal(-1, petPrefab);
    }

    private GameObject ResolvePetPrefabForLevel(int petId, int level, string fallbackName)
    {
        GameDataManager data = GameDataManager.Instance;
        if (data == null)
            return null;

        return data.GetPetPrefabForLevel(petId, level, fallbackName);
    }

    private void ShowPetInternal(int ownedPetId, GameObject petPrefab)
    {
        if (petPrefab == null)
        {
            Debug.LogError("[ChoosePet] Pet prefab is null.");
            ResetStatTextsToDefault();
            return;
        }

        if (currentPetInstance != null)
            Destroy(currentPetInstance);

        currentPetInstance = Instantiate(petPrefab, _petSpawnPoint.position, Quaternion.identity, _petSpawnPoint);
        currentPetInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0f);

        ShortLayer shortLayer = currentPetInstance.GetComponent<ShortLayer>();
        if (shortLayer == null)
            shortLayer = currentPetInstance.AddComponent<ShortLayer>();

        shortLayer.SortingLayerName = _petSortingLayerName;
        shortLayer.SortingOrder = Mathf.Min(_petSortingOrder, 19);
        shortLayer.Apply();

        currentPetId = ownedPetId;
        OwnedPetData ownedPet = PlayerManager.Instance != null ? PlayerManager.Instance.GetOwnedPet(currentPetId) : null;

        if (ownedPet == null)
        {
            Debug.LogWarning($"[ChoosePet] Player does not own pet id: {currentPetId}");
            ResetStatTextsToDefault();
            return;
        }

        if (GameDataManager.Instance == null)
        {
            ResetStatTextsToDefault();
            return;
        }

        currentPetMaxLevel = Mathf.Max(1, GameDataManager.Instance.GetPetMaxLevel(currentPetId));
        currentLevel = Mathf.Clamp(ownedPet.petLevel, 1, currentPetMaxLevel);

        if (!GameDataManager.Instance.TryGetPetStatSnapshot(currentPetId, currentLevel, out GameDataManager.PetStatSnapshot currentData))
        {
            Debug.LogWarning($"[ChoosePet] Missing pet stats for petId={currentPetId}, level={currentLevel}");
            ResetStatTextsToDefault();
            return;
        }

        currentPetName = string.IsNullOrWhiteSpace(currentData.petName) ? ownedPet.petName : currentData.petName;
        currentPetElement = currentData.element;

        UpdatePetUI();

        if (gemUpdate != null)
        {
            int elementId = GetElementIndexFromElementOrName(currentPetElement, currentPetName);
            if (elementId >= 0)
            {
                gemUpdate.SelectPet(elementId.ToString());
                gemUpdate.SetCurrentPetLevel(currentLevel);
                gemUpdate.ForceClearSelectedGems();
            }
        }

        if (_upgradeButton != null)
            _upgradeButton.gameObject.SetActive(currentLevel < currentPetMaxLevel);
    }

    private void EnsureTrainingPetSorting()
    {
        if (currentPetInstance == null)
            return;

        ShortLayer shortLayer = currentPetInstance.GetComponent<ShortLayer>();
        if (shortLayer == null)
            shortLayer = currentPetInstance.AddComponent<ShortLayer>();

        shortLayer.SortingLayerName = _petSortingLayerName;
        shortLayer.SortingOrder = Mathf.Min(_petSortingOrder, 19);
        shortLayer.Apply();
    }

    private void UpdatePetUI()
    {
        if (GameDataManager.Instance == null || currentPetId < 0)
            return;

        if (!GameDataManager.Instance.TryGetPetStatSnapshot(currentPetId, currentLevel, out GameDataManager.PetStatSnapshot data))
            return;

        if (_txtPetName != null)
        {
            // data.petName is a localization key - get localized text for it
            string petDisplayName = GetLocalizedText(data.petName, data.petName ?? "Unknown");
            _txtPetName.text = petDisplayName;
        }

        int elementIndex = GetElementIndexFromElementOrName(data.element, data.petName);
        if (_elementIcon != null && _elementSprite != null && elementIndex >= 0 && elementIndex < _elementSprite.Length)
            _elementIcon.sprite = _elementSprite[elementIndex];

        string levelFormat = GetLocalizedText("pet_training_level_format", "Lv. {0}");

        if (_txtLevel != null) _txtLevel.text = string.Format(levelFormat, data.level);
        if (_txtHP != null) _txtHP.text = data.baseHP.ToString();
        if (_txtAtk != null) _txtAtk.text = data.baseAttack.ToString();
        if (_txtMana != null) _txtMana.text = data.baseMana.ToString();
        if (_txtRage != null) _txtRage.text = data.baseRage.ToString();
        if (_txtArmor != null) _txtArmor.text = data.armor.ToString();
        if (_txtCrit != null) _txtCrit.text = $"{data.critRate}%";
        if (_txtCritDamage != null) _txtCritDamage.text = $"{data.critDamage}%";

        if (currentLevel < currentPetMaxLevel &&
            GameDataManager.Instance.TryGetPetStatSnapshot(currentPetId, currentLevel + 1, out GameDataManager.PetStatSnapshot next))
        {
            if (_txtLevelNext != null) _txtLevelNext.text = string.Format(levelFormat, next.level);
            if (_txtHPNext != null) _txtHPNext.text = next.baseHP.ToString();
            if (_txtAtkNext != null) _txtAtkNext.text = next.baseAttack.ToString();
            if (_txtManaNext != null) _txtManaNext.text = next.baseMana.ToString();
            if (_txtRageNext != null) _txtRageNext.text = next.baseRage.ToString();
            if (_txtArmorNext != null) _txtArmorNext.text = next.armor.ToString();
            if (_txtCritNext != null) _txtCritNext.text = $"{next.critRate}%";
            if (_txtCritDamageNext != null) _txtCritDamageNext.text = $"{next.critDamage}%";
        }
        else
        {
            string maxText = GetLocalizedText("pet_training_max", "Max");
            if (_txtLevelNext != null) _txtLevelNext.text = maxText;
            if (_txtHPNext != null) _txtHPNext.text = "-";
            if (_txtAtkNext != null) _txtAtkNext.text = "-";
            if (_txtManaNext != null) _txtManaNext.text = "-";
            if (_txtRageNext != null) _txtRageNext.text = "-";
            if (_txtArmorNext != null) _txtArmorNext.text = "-";
            if (_txtCritNext != null) _txtCritNext.text = "-";
            if (_txtCritDamageNext != null) _txtCritDamageNext.text = "-";
        }

        UpdateSkillInfo(data.skillId);

        if (_upgradeButton != null)
            _upgradeButton.gameObject.SetActive(currentLevel < currentPetMaxLevel);
    }

    private void UpdateSkillInfo(int skillId)
    {
        if (_skillInfoRoot != null)
            _skillInfoRoot.SetActive(false);

        if (skillId <= 0 || GameDataManager.Instance == null)
        {
            ClearSkillInfoTexts();
            return;
        }

        SkillData skill = GameDataManager.Instance.GetSkillData(skillId);
        if (skill == null)
        {
            ClearSkillInfoTexts();
            return;
        }

        if (_skillInfoRoot != null)
            _skillInfoRoot.SetActive(true);

        if (_txtSkillName != null)
            _txtSkillName.text = GetLocalizedText(skill.skillName, skill.skillName ?? string.Empty);

        if (_txtSkillDescription != null)
            _txtSkillDescription.text = GetLocalizedText(skill.desSkill, skill.desSkill ?? string.Empty);
    }

    private void ClearSkillInfoTexts()
    {
        if (_txtSkillName != null)
            _txtSkillName.text = string.Empty;
        if (_txtSkillDescription != null)
            _txtSkillDescription.text = string.Empty;
    }

    private GameObject SpawnUpgradeFx(GameObject prefab)
    {
        Transform spawnPoint = _upgradeFxSpawnPoint != null ? _upgradeFxSpawnPoint : _petSpawnPoint;
        if (prefab == null || spawnPoint == null)
            return null;

        return Instantiate(prefab, spawnPoint.position, Quaternion.identity, spawnPoint);
    }

    private IEnumerator DestroyFxAfterSeconds(GameObject fxInstance, float seconds)
    {
        float wait = Mathf.Max(0f, seconds);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        if (fxInstance != null)
            Destroy(fxInstance);
    }

    private void OnUpgradeButtonClick()
    {
        if (_upgradeFlowCoroutine != null || _isUpgradeInProgress)
            return;

        _upgradeFlowCoroutine = StartCoroutine(UpgradePetFlowRoutine());
    }

    private IEnumerator UpgradePetFlowRoutine()
    {
        SetUpgradeInteractionLocked(true);
        if (currentPetId < 0)
        {
            EndUpgradeFlow();
            yield break;
        }

        if (currentLevel >= currentPetMaxLevel)
        {
            ShowUpgradeResult(false, GetLocalizedText("pet_training_upgrade_max", "Pet da dat cap toi da."));
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUpgradePetFailedSound();

            EndUpgradeFlow();
            yield break;
        }

        if (PlayerManager.Instance == null)
        {
            ShowUpgradeResult(false, GetLocalizedText("pet_training_no_player_data", "Khong co du lieu nguoi choi."));
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUpgradePetFailedSound();

            EndUpgradeFlow();
            yield break;
        }

        if (gemUpdate == null)
        {
            ShowUpgradeResult(false, GetLocalizedText("pet_training_missing_gem_system", "Thieu he thong gem nang cap."));
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUpgradePetFailedSound();

            EndUpgradeFlow();
            yield break;
        }

        gemUpdate.SetCurrentPetLevel(currentLevel);

        if (!gemUpdate.TryGetSelectedGems(out int elementId, out int[] selectedGemLevels))
        {
            ShowUpgradeResult(false, GetLocalizedText("pet_training_need_gem", "Can chon it nhat 1 gem de nang cap."));
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUpgradePetFailedSound();

            EndUpgradeFlow();
            yield break;
        }

        if (!PlayerManager.Instance.ConsumeOwnedGems(elementId, selectedGemLevels))
        {
            ShowUpgradeResult(false, GetLocalizedText("pet_training_not_enough_gems", "Khong du gem trong tui do."));
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUpgradePetFailedSound();

            EndUpgradeFlow();
            yield break;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUpgradeGemProcessingSound();

        GameObject upgradeFx = SpawnUpgradeFx(_upgradeInProgressFxPrefab);
        float wait = Mathf.Max(0f, _upgradeFxWaitSeconds);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);
        if (upgradeFx != null)
            Destroy(upgradeFx);

        int previousLevel = currentLevel;
        bool isSuccess = gemUpdate.UpgradePet();

        if (isSuccess)
        {
            currentLevel = Mathf.Min(currentLevel + 1, currentPetMaxLevel);
            ShowUpgradeResult(true, GetLocalizedText("pet_training_upgrade_success", "Nang cap thanh cong!"));

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUpgradeGemSuccessSound();
                AudioManager.Instance.PlayUpgradePetSuccessSound();
            }
        }
        else
        {
            currentLevel = Mathf.Max(1, currentLevel - 1);
            ShowUpgradeResult(false, GetLocalizedText("pet_training_upgrade_fail", "Nang cap that bai!"));

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayUpgradeGemFailedSound();
                AudioManager.Instance.PlayUpgradePetFailedSound();
            }
        }

        GameObject resultFx = SpawnUpgradeFx(isSuccess ? _upgradeSuccessFxPrefab : _upgradeFailFxPrefab);
        if (resultFx != null)
            StartCoroutine(DestroyFxAfterSeconds(resultFx, _resultFxLifetimeSeconds));

        PlayerManager.Instance.TrySetOwnedPetLevel(currentPetId, currentLevel);
        if (isSuccess)
            SteamManager.Instance.ReportPetUpgraded(currentPetId, currentLevel);
        PlayerManager.Instance.SaveData();

        gemUpdate.ForceClearSelectedGems();
        gemUpdate.SetCurrentPetLevel(currentLevel);

        bool didReloadPrefab = false;
        if (ShouldReloadPrefabForLevelChange(previousLevel, currentLevel))
            didReloadPrefab = ReloadCurrentPetPrefabForCurrentLevel();

        if (!didReloadPrefab)
            UpdatePetUI();

        if (_popupSelectPet != null)
            _popupSelectPet.RefreshSelectedPetItem(currentPetId);

        EndUpgradeFlow();
    }

    private void SetUpgradeInteractionLocked(bool isLocked)
    {
        _isUpgradeInProgress = isLocked;

        if (_upgradeButton != null)
            _upgradeButton.interactable = !isLocked;

        if (_changePetButton != null)
            _changePetButton.interactable = !isLocked;

        if (_openPopupButton != null)
            _openPopupButton.interactable = !isLocked;

        if (_closePopupButton != null)
            _closePopupButton.interactable = !isLocked;

        if (gemUpdate != null)
            gemUpdate.SetInteractionLocked(isLocked);
    }

    private void EndUpgradeFlow()
    {
        SetUpgradeInteractionLocked(false);
        _upgradeFlowCoroutine = null;
    }

    private void ShowUpgradeResult(bool isSuccess, string message)
    {
        HideUpgradeResultPopups();

        if (isSuccess)
        {
            if (_upgradeSuccessPopup != null)
                _upgradeSuccessPopup.SetActive(true);
            if (_txtUpgradeSuccess != null)
                _txtUpgradeSuccess.text = message;
            StartAutoCloseResultPopup();
            return;
        }

        if (_upgradeFailPopup != null)
            _upgradeFailPopup.SetActive(true);
        if (_txtUpgradeFail != null)
            _txtUpgradeFail.text = message;

        StartAutoCloseResultPopup();
    }

    public void HideUpgradeResultPopups()
    {
        if (_autoCloseResultCoroutine != null)
        {
            StopCoroutine(_autoCloseResultCoroutine);
            _autoCloseResultCoroutine = null;
        }

        if (_upgradeSuccessPopup != null)
            _upgradeSuccessPopup.SetActive(false);
        if (_upgradeFailPopup != null)
            _upgradeFailPopup.SetActive(false);
    }

    private void StartAutoCloseResultPopup()
    {
        if (_autoCloseResultCoroutine != null)
            StopCoroutine(_autoCloseResultCoroutine);

        _autoCloseResultCoroutine = StartCoroutine(AutoCloseResultPopupRoutine());
    }

    private IEnumerator AutoCloseResultPopupRoutine()
    {
        float waitSeconds = Mathf.Max(0f, _resultPopupAutoCloseSeconds);
        if (waitSeconds > 0f)
            yield return new WaitForSeconds(waitSeconds);

        HideUpgradeResultPopups();
        _autoCloseResultCoroutine = null;
    }

    private void OnPlayerDataChanged()
    {
        if (currentPetId < 0 || PlayerManager.Instance == null)
            return;

        OwnedPetData ownedPet = PlayerManager.Instance.GetOwnedPet(currentPetId);
        if (ownedPet == null)
            return;

        currentPetMaxLevel = Mathf.Max(1, GameDataManager.Instance != null ? GameDataManager.Instance.GetPetMaxLevel(currentPetId) : 1);
        currentLevel = Mathf.Clamp(ownedPet.petLevel, 1, currentPetMaxLevel);
        UpdatePetUI();
    }

    private void AutoSelectFirstOwnedPetIfNeeded()
    {
        if (currentPetId >= 0)
            return;

        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null || PlayerManager.Instance.Data.ownedPets == null)
            return;

        if (PlayerManager.Instance.Data.ownedPets.Count <= 0)
            return;

        OnSelectOwnedPet(PlayerManager.Instance.Data.ownedPets[0].petId);
    }

    private void ResetStatTextsToDefault()
    {
        if (_txtLevel != null) _txtLevel.text = "0";
        if (_txtHP != null) _txtHP.text = "0";
        if (_txtAtk != null) _txtAtk.text = "0";
        if (_txtMana != null) _txtMana.text = "0";
        if (_txtRage != null) _txtRage.text = "0";
        if (_txtArmor != null) _txtArmor.text = "0";
        if (_txtCrit != null) _txtCrit.text = "0";
        if (_txtCritDamage != null) _txtCritDamage.text = "0";

        if (_txtLevelNext != null) _txtLevelNext.text = "0";
        if (_txtHPNext != null) _txtHPNext.text = "0";
        if (_txtAtkNext != null) _txtAtkNext.text = "0";
        if (_txtManaNext != null) _txtManaNext.text = "0";
        if (_txtRageNext != null) _txtRageNext.text = "0";
        if (_txtArmorNext != null) _txtArmorNext.text = "0";
        if (_txtCritNext != null) _txtCritNext.text = "0";
        if (_txtCritDamageNext != null) _txtCritDamageNext.text = "0";

        ClearSkillInfoTexts();
        if (_skillInfoRoot != null)
            _skillInfoRoot.SetActive(false);
    }

    private string GetLocalizedText(string id, string fallback)
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
            return LocalizationManager.Instance.GetText(id, fallback);

        return fallback;
    }

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

        string[] elements = { "Dark", "Earth", "Fire", "Light", "Metal", "Water", "Wood" };
        for (int i = 0; i < elements.Length; i++)
        {
            if (string.Equals(rawElement, elements[i], System.StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private bool ShouldReloadPrefabForLevelChange(int previousLevel, int newLevel)
    {
        if (newLevel == 3 || newLevel == 6)
            return true;

        return (previousLevel == 3 || previousLevel == 6) && newLevel < previousLevel;
    }

    private bool ReloadCurrentPetPrefabForCurrentLevel()
    {
        if (currentPetId < 0 || PlayerManager.Instance == null)
            return false;

        OwnedPetData ownedPet = PlayerManager.Instance.GetOwnedPet(currentPetId);
        if (ownedPet == null)
            return false;

        GameObject prefab = ResolvePetPrefabForLevel(currentPetId, currentLevel, ownedPet.petName);
        if (prefab == null)
            return false;

        ShowPetInternal(currentPetId, prefab);
        return true;
    }
}


