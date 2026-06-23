using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GemUpdate : MonoBehaviour
{
    [Header("Slot UI")]
    [SerializeField] private Button slotButton1;
    [SerializeField] private Button slotButton2;
    [SerializeField] private Button slotButton3;
    [SerializeField] private Image slotIcon1;
    [SerializeField] private Image slotIcon2;
    [SerializeField] private Image slotIcon3;

    [Header("Popup Select Gem")]
    [SerializeField] private GameObject popupGemRoot;
    [SerializeField] private Transform popupGemContentRoot;
    [SerializeField] private GemInventoryItemButton popupGemItemPrefab;
    [SerializeField] private Button clearSlotsButton;

    [Header("Rate UI")]
    [SerializeField] private TextMeshProUGUI txtPercent;

    [Header("Upgrade FX")]
    [SerializeField] private GameObject upgradeFxPrefab;
    [SerializeField] private Vector3 upgradeFxLocalOffset = Vector3.zero;
    [SerializeField] private float upgradeFxSeconds = 3f;

    [Header("Data")]
    [SerializeField] private GemCollection gemCollection;

    private const int MAX_LEVEL = 10;

    private readonly int[] selectedGemLevels = new int[3];
    private readonly int[] selectedGemElements = new int[3];
    private readonly List<GemInventoryItemButton> spawnedPopupItems = new List<GemInventoryItemButton>();
    private readonly List<GameObject> spawnedUpgradeFx = new List<GameObject>();

    private int currentPetLevel = 1;
    private int currentElementIndex = -1;
    private int activeSlotIndex = -1;
    private bool isInteractionLocked = false;

    public bool HasUpgradeFxPrefab => upgradeFxPrefab != null;
    public float UpgradeFxSeconds => Mathf.Max(0f, upgradeFxSeconds);
    public bool IsInteractionLocked => isInteractionLocked;

    private void Start()
    {
        EnsureGemCollectionReference();

        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            selectedGemLevels[i] = 0;
            selectedGemElements[i] = -1;
        }

        BindSlotButtons();

        if (clearSlotsButton != null)
        {
            clearSlotsButton.onClick.RemoveAllListeners();
            clearSlotsButton.onClick.AddListener(ClearSelectedGems);
        }

        if (popupGemRoot != null)
            popupGemRoot.SetActive(false);

        UpdateGemCells();
        UpdatePercentDisplay();
    }

    private void OnDisable()
    {
        ClearSpawnedUpgradeFx();
    }

    public IEnumerator PlayUpgradeHammerEffectRoutine()
    {
        ClearSpawnedUpgradeFx();

        if (upgradeFxPrefab != null)
        {
            for (int i = 0; i < selectedGemLevels.Length; i++)
            {
                if (selectedGemLevels[i] <= 0)
                    continue;

                Transform root = GetSlotVisualRoot(i);
                if (root == null)
                    continue;

                GameObject fxInstance = Instantiate(upgradeFxPrefab, root);
                fxInstance.transform.localPosition = upgradeFxLocalOffset;
                fxInstance.transform.localRotation = Quaternion.identity;
                fxInstance.SetActive(true);
                spawnedUpgradeFx.Add(fxInstance);
            }
        }

        float wait = Mathf.Max(0.1f, upgradeFxSeconds);
        yield return new WaitForSeconds(wait);

        ClearSpawnedUpgradeFx();
    }

    public void SelectPet(string id)
    {
        if (!int.TryParse(id, out int elementIndex))
            elementIndex = -1;

        currentElementIndex = elementIndex;
        ForceClearSelectedGems();
        RebuildGemPopupItems();
    }

    public void SetCurrentPetLevel(int level)
    {
        currentPetLevel = Mathf.Clamp(level, 1, MAX_LEVEL);
        UpdatePercentDisplay();
    }

    public bool TryGetSelectedGems(out int elementId, out int[] gemLevels)
    {
        elementId = currentElementIndex;
        gemLevels = null;

        if (currentElementIndex < 0)
            return false;

        List<int> levels = GetSelectedGemLevels();
        if (levels.Count <= 0 || levels.Count > 3)
            return false;

        gemLevels = levels.ToArray();
        return true;
    }

    public void ClearSelectedGems()
    {
        if (isInteractionLocked)
            return;

        ClearSelectedGemsInternal();
    }

    public void ForceClearSelectedGems()
    {
        ClearSelectedGemsInternal();
    }

    public void SetInteractionLocked(bool isLocked)
    {
        isInteractionLocked = isLocked;

        SetButtonInteractable(slotButton1, !isLocked);
        SetButtonInteractable(slotButton2, !isLocked);
        SetButtonInteractable(slotButton3, !isLocked);
        SetButtonInteractable(clearSlotsButton, !isLocked);

        if (isLocked)
            CloseGemPopup();
    }

    private void ClearSelectedGemsInternal()
    {
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            selectedGemLevels[i] = 0;
            selectedGemElements[i] = -1;
        }

        UpdateGemCells();
        UpdatePercentDisplay();
        RebuildGemPopupItems();
    }

    public bool UpgradePet()
    {
        if (currentPetLevel >= MAX_LEVEL)
        {
            return false;
        }

        List<int> levels = GetSelectedGemLevels();
        if (levels.Count <= 0 || levels.Count > 3)
        {
            return false;
        }

        bool result = PetUpgradeService.TryUpgrade(currentPetLevel, levels, out float successRate, out float roll);

        if (result)
            currentPetLevel = Mathf.Min(currentPetLevel + 1, MAX_LEVEL);

        return result;
    }

    private void BindSlotButtons()
    {
        BindSlotButton(slotButton1, 0);
        BindSlotButton(slotButton2, 1);
        BindSlotButton(slotButton3, 2);
    }

    private void BindSlotButton(Button button, int slotIndex)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSlotClick(slotIndex));
    }

    private void OnSlotClick(int slotIndex)
    {
        if (isInteractionLocked)
            return;

        if (slotIndex < 0 || slotIndex >= selectedGemLevels.Length)
            return;

        activeSlotIndex = slotIndex;
        OpenGemPopup();
    }

    private void OpenGemPopup()
    {
        RebuildGemPopupItems();
        if (popupGemRoot != null)
            popupGemRoot.SetActive(true);
    }

    private void CloseGemPopup()
    {
        if (popupGemRoot != null)
            popupGemRoot.SetActive(false);
        activeSlotIndex = -1;
    }

    private void RebuildGemPopupItems()
    {
        ClearPopupItems();

        if (popupGemContentRoot == null || popupGemItemPrefab == null)
            return;

        if (currentElementIndex < 0)
            return;

        EnsureGemCollectionReference();
        if (gemCollection == null || gemCollection.elements == null || currentElementIndex >= gemCollection.elements.Length)
            return;

        List<OwnedGemData> gems = GetOwnedElementGems(currentElementIndex);

        if (gems.Count == 0)
            return;

        gems.Sort((a, b) => a.gemLevel.CompareTo(b.gemLevel));

        for (int i = 0; i < gems.Count; i++)
        {
            OwnedGemData owned = gems[i];
            if (owned == null || owned.quantity <= 0)
                continue;

            int availableQuantity = GetAvailableGemQuantity(owned.gemLevel, activeSlotIndex);
            if (availableQuantity <= 0)
                continue;

            Sprite icon = GetGemSprite(currentElementIndex, owned.gemLevel);
            GemInventoryItemButton item = Instantiate(popupGemItemPrefab, popupGemContentRoot);
            item.Setup(owned.gemLevel, availableQuantity, icon, OnGemSelectedFromPopup);
            spawnedPopupItems.Add(item);
        }
    }

    private List<OwnedGemData> GetOwnedElementGems(int elementIndex)
    {
        List<OwnedGemData> result = new List<OwnedGemData>();
        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null || PlayerManager.Instance.Data.ownedGems == null)
            return result;

        List<OwnedGemData> source = PlayerManager.Instance.Data.ownedGems;
        for (int i = 0; i < source.Count; i++)
        {
            OwnedGemData gem = source[i];
            if (gem == null)
                continue;

            if (gem.elementId == elementIndex && gem.quantity > 0)
                result.Add(gem);
        }
        return result;
    }

    private void OnGemSelectedFromPopup(int gemLevel)
    {
        if (isInteractionLocked)
            return;

        int slot = activeSlotIndex;
        if (slot < 0 || slot >= selectedGemLevels.Length)
            slot = GetFirstEmptySlot();

        if (slot < 0)
            slot = 0;

        if (GetAvailableGemQuantity(gemLevel, slot) <= 0)
            return;

        selectedGemLevels[slot] = gemLevel;
        selectedGemElements[slot] = currentElementIndex;

        UpdateGemCells();
        UpdatePercentDisplay();
        CloseGemPopup();
    }

    private int GetFirstEmptySlot()
    {
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            if (selectedGemLevels[i] <= 0)
                return i;
        }
        return -1;
    }

    private void UpdateGemCells()
    {
        UpdateCell(slotIcon1, 0);
        UpdateCell(slotIcon2, 1);
        UpdateCell(slotIcon3, 2);
    }

    private void UpdateCell(Image image, int slotIndex)
    {
        if (image == null)
            return;

        int gemLevel = selectedGemLevels[slotIndex];
        int element = selectedGemElements[slotIndex] >= 0 ? selectedGemElements[slotIndex] : currentElementIndex;

        if (gemLevel <= 0 || element < 0)
        {
            image.sprite = null;
            image.enabled = false;
            return;
        }

        image.sprite = GetGemSprite(element, gemLevel);
        image.enabled = image.sprite != null;
    }

    private Sprite GetGemSprite(int elementIndex, int gemLevel)
    {
        EnsureGemCollectionReference();
        if (gemCollection == null || gemCollection.elements == null)
            return null;

        if (elementIndex < 0 || elementIndex >= gemCollection.elements.Length)
            return null;

        GemCollection.GemElementData element = gemCollection.elements[elementIndex];
        if (element == null || element.gemLevels == null)
            return null;

        int levelIndex = gemLevel - 1;
        if (levelIndex < 0 || levelIndex >= element.gemLevels.Length)
            return null;

        GemCollection.GemLevelData data = element.gemLevels[levelIndex];
        return data != null ? data.sprite : null;
    }

    private void UpdatePercentDisplay()
    {
        if (txtPercent == null)
            return;
        List<int> levels = GetSelectedGemLevels();

        if (levels.Count == 0)
        {
            txtPercent.text = "0%";
            return;
        }

        float successRate = PetUpgradeService.CalculateSuccessRate(currentPetLevel, levels);
        txtPercent.text = $"{successRate * 100f:F0}%";
    }

    private List<int> GetSelectedGemLevels()
    {
        List<int> levels = new List<int>();
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            if (selectedGemLevels[i] >= 1 && selectedGemLevels[i] <= 5)
                levels.Add(selectedGemLevels[i]);
        }
        return levels;
    }

    private int GetAvailableGemQuantity(int gemLevel, int excludeSlotIndex = -1)
    {
        if (PlayerManager.Instance == null)
            return 0;

        int ownedQuantity = PlayerManager.Instance.GetOwnedGemQuantity(currentElementIndex, gemLevel);
        int reservedQuantity = GetReservedGemCount(gemLevel, excludeSlotIndex);
        return Mathf.Max(0, ownedQuantity - reservedQuantity);
    }

    private int GetReservedGemCount(int gemLevel, int excludeSlotIndex = -1)
    {
        int reserved = 0;
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            if (i == excludeSlotIndex)
                continue;

            if (selectedGemElements[i] == currentElementIndex && selectedGemLevels[i] == gemLevel)
                reserved++;
        }

        return reserved;
    }

    private void EnsureGemCollectionReference()
    {
        if (gemCollection != null)
            return;

        if (GameDataManager.Instance != null)
            gemCollection = GameDataManager.Instance.GemCollectionObject as GemCollection;
    }

    private void ClearPopupItems()
    {
        for (int i = 0; i < spawnedPopupItems.Count; i++)
        {
            GemInventoryItemButton item = spawnedPopupItems[i];
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedPopupItems.Clear();
    }

    private Transform GetSlotVisualRoot(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0:
                if (slotIcon1 != null) return slotIcon1.transform;
                if (slotButton1 != null) return slotButton1.transform;
                break;
            case 1:
                if (slotIcon2 != null) return slotIcon2.transform;
                if (slotButton2 != null) return slotButton2.transform;
                break;
            case 2:
                if (slotIcon3 != null) return slotIcon3.transform;
                if (slotButton3 != null) return slotButton3.transform;
                break;
        }

        return transform;
    }

    private void ClearSpawnedUpgradeFx()
    {
        for (int i = 0; i < spawnedUpgradeFx.Count; i++)
        {
            if (spawnedUpgradeFx[i] != null)
                Destroy(spawnedUpgradeFx[i]);
        }

        spawnedUpgradeFx.Clear();
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }
}






