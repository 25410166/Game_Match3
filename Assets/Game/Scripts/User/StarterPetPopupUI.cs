using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarterPetPopupUI : MonoBehaviour
{
    [Header("Popup References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private PetDatabase petDatabase;

    [Header("Starter Setup")]
    [SerializeField] private int[] starterPetIds = new int[3] { 1, 2, 3 };
    [SerializeField] private Transform[] previewSlots = new Transform[3];
    [SerializeField] private Button[] chooseButtons = new Button[3];
    [SerializeField] private TextMeshProUGUI[] petNameTexts = new TextMeshProUGUI[3];
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Preview Sorting")]
    [SerializeField] private string previewSortingLayerName = "UI";
    [SerializeField] private int previewSortingOrder = 11;

    private GameObject[] spawnedPreviews;
    private int selectedPetId = -1;
    private string selectedPetName;

    private void Start()
    {
        spawnedPreviews = new GameObject[previewSlots != null ? previewSlots.Length : 0];

        for (int i = 0; i < chooseButtons.Length; i++)
        {
            if (chooseButtons[i] == null) continue;
            int index = i;
            chooseButtons[i].onClick.RemoveAllListeners();
            chooseButtons[i].onClick.AddListener(() => OnChooseStarter(index));
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnConfirmStarter);
        }

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged += RefreshState;

        RegisterLocalizationEvents();
        RefreshState();
    }

    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged -= RefreshState;

        UnregisterLocalizationEvents();
        ClearSpawnedPreviews();
    }

    private void RefreshState()
    {
        if (PlayerManager.Instance == null)
        {
            SetPopupVisible(false);
            return;
        }

        if (!PlayerManager.Instance.HasConfirmedPlayerName)
        {
            SetPopupVisible(false);
            return;
        }

        bool needStarterPick = !PlayerManager.Instance.HasSelectedStarterPet;
        SetPopupVisible(needStarterPick);

        if (!needStarterPick) return;

        selectedPetId = -1;
        selectedPetName = string.Empty;
        BuildPreviews();
        RefreshPetNameTexts();
        ShowError(string.Empty);
    }

    private void BuildPreviews()
    {
        if (petDatabase == null || previewSlots == null) return;

        for (int i = 0; i < previewSlots.Length; i++)
        {
            if (previewSlots[i] == null) continue;

            if (spawnedPreviews != null && i < spawnedPreviews.Length && spawnedPreviews[i] != null)
            {
                Destroy(spawnedPreviews[i]);
                spawnedPreviews[i] = null;
            }

            if (i >= starterPetIds.Length) continue;

            int petId = starterPetIds[i];
            PetDataAsset petData = petDatabase.GetPetById(petId);
            if (petData == null || petData.prefab == null) continue;

            GameObject preview = Instantiate(petData.prefab, previewSlots[i]);
            preview.transform.localPosition = Vector3.zero;
            preview.transform.localRotation = Quaternion.identity;
            preview.transform.localScale = Vector3.one;
            ForceIdle(preview);
            ApplyShortLayer(preview);

            if (spawnedPreviews != null && i < spawnedPreviews.Length)
                spawnedPreviews[i] = preview;
        }
    }

    private void RefreshPetNameTexts()
    {
        if (petNameTexts == null || petDatabase == null)
            return;

        for (int i = 0; i < petNameTexts.Length; i++)
        {
            TextMeshProUGUI targetText = petNameTexts[i];
            if (targetText == null)
                continue;

            if (i >= starterPetIds.Length)
            {
                targetText.text = string.Empty;
                continue;
            }

            PetDataAsset petData = petDatabase.GetPetById(starterPetIds[i]);
            string fallbackName = petData != null ? petData.petName : string.Empty;
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
                targetText.text = LocalizationManager.Instance.GetText(fallbackName, fallbackName);
            else
                targetText.text = fallbackName;
        }
    }

    private void RegisterLocalizationEvents()
    {
        if (LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged -= RefreshPetNameTexts;
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshPetNameTexts;
        LocalizationManager.Instance.OnLanguageChanged += RefreshPetNameTexts;
        LocalizationManager.Instance.OnLocalizationLoaded += RefreshPetNameTexts;
    }

    private void UnregisterLocalizationEvents()
    {
        if (LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged -= RefreshPetNameTexts;
        LocalizationManager.Instance.OnLocalizationLoaded -= RefreshPetNameTexts;
    }

    private void OnChooseStarter(int index)
    {
        if (petDatabase == null)
        {
            ShowError(GetLocalizedText("starter_pet_missing_database", "Missing PetDatabase reference."));
            return;
        }

        if (index < 0 || index >= starterPetIds.Length)
        {
            ShowError(GetLocalizedText("starter_pet_invalid_selection", "Invalid starter pet."));
            return;
        }

        int petId = starterPetIds[index];
        PetDataAsset petData = petDatabase.GetPetById(petId);
        if (petData == null)
        {
            ShowError(GetLocalizedText("starter_pet_data_not_found", "Pet data not found."));
            return;
        }

        selectedPetId = petId;
        selectedPetName = petData.petName;

        string localizedPetName = GetLocalizedPetName(selectedPetName);
        string selectionFormat = GetLocalizedText("c1", "Selected pet: $ . Press Continue to confirm.");
        ShowError(selectionFormat.Replace("$", localizedPetName));
    }

    private void OnConfirmStarter()
    {
        if (PlayerManager.Instance == null)
        {
            ShowError(GetLocalizedText("starter_pet_player_manager_not_found", "PlayerManager not found."));
            return;
        }

        if (selectedPetId < 0)
        {
            ShowError(GetLocalizedText("starter_pet_select_first", "Please select a starter pet first."));
            return;
        }

        bool saved = PlayerManager.Instance.ConfirmStarterPetSelection(selectedPetId, selectedPetName, 1);
        if (!saved)
        {
            ShowError(GetLocalizedText("starter_pet_save_failed", "Unable to save starter pet."));
            return;
        }

        PlayerManager.Instance.SaveData();

        if (TutorialProgressManager.Instance != null)
            TutorialProgressManager.Instance.NotifyStarterPetConfirmed();
        ClearSpawnedPreviews();
        ShowError(string.Empty);
        SetPopupVisible(false);
    }

    private void ForceIdle(GameObject go)
    {
        if (go == null) return;

        SkeletonAnimation skeletonAnim = go.GetComponentInChildren<SkeletonAnimation>(true);
        if (skeletonAnim != null && skeletonAnim.state != null)
            skeletonAnim.state.SetAnimation(0, "Idle", true);
    }

    private void ApplyShortLayer(GameObject go)
    {
        if (go == null) return;

        ShortLayer shortLayer = go.GetComponent<ShortLayer>();
        if (shortLayer == null)
            shortLayer = go.AddComponent<ShortLayer>();

        shortLayer.SortingLayerName = previewSortingLayerName;
        shortLayer.SortingOrder = previewSortingOrder;
        shortLayer.Apply();
    }

    public void ClearSpawnedPreviews()
    {
        if (spawnedPreviews == null) return;

        for (int i = 0; i < spawnedPreviews.Length; i++)
        {
            if (spawnedPreviews[i] != null)
            {
                Destroy(spawnedPreviews[i]);
                spawnedPreviews[i] = null;
            }
        }
    }

    private void SetPopupVisible(bool visible)
    {
        if (popupRoot != null) popupRoot.SetActive(visible);
        else gameObject.SetActive(visible);
    }

    private void ShowError(string message)
    {
        if (errorText == null) return;
        errorText.text = message;
        errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private string GetLocalizedPetName(string petNameKey)
    {
        if (string.IsNullOrWhiteSpace(petNameKey))
            return string.Empty;

        return GetLocalizedText(petNameKey, petNameKey);
    }

    private string GetLocalizedText(string key, string fallback)
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
            return LocalizationManager.Instance.GetText(key, fallback);

        return fallback;
    }
}






