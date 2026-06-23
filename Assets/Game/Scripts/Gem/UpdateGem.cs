using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpdateGem : MonoBehaviour
{
    [Header("Home -> Forge Popup")]
    [SerializeField] private Button openFromHomeButton;
    [SerializeField] private GameObject forgePopupRoot;
    [SerializeField] private Button closeForgePopupButton;

    [Header("Forge Tabs")]
    [SerializeField] private Button tabUpgradeGemButton;
    [SerializeField] private GameObject tabUpgradeGemRoot;

    [Header("Element Tabs (7)")]
    [SerializeField] private Button[] elementButtons = new Button[7];

    [Header("Gem Inventory List")]
    [SerializeField] private Transform gemListContent;
    [SerializeField] private GemInventoryItemButton gemItemPrefab;

    [Header("Slots")]
    [SerializeField] private Button[] slotButtons = new Button[3];
    [SerializeField] private Image[] slotIcons = new Image[3];
    [SerializeField] private TextMeshProUGUI[] slotLevelTexts = new TextMeshProUGUI[3];
    [SerializeField] private Button clearSlotsButton;

    [Header("Actions")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI txtSuccessRate;
    [SerializeField] private TextMeshProUGUI txtResult;

    [Header("Visual")]
    [SerializeField] private Color slotNormalColor = Color.white;
    [SerializeField] private Color slotSelectedColor = Color.green;
    [SerializeField] private float resultFloatY = 40f;
    [SerializeField] private float resultAnimDuration = 3f;
    [SerializeField] private GameObject upgradeFxPrefab;
    [SerializeField] private Vector3 upgradeFxLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 upgradeFxLocalScale = Vector3.one;
    [SerializeField] private float upgradeFxSeconds = 2f;

    [Header("Data")]
    [SerializeField] private GemCollection gemCollection;

    private readonly List<GemInventoryItemButton> spawnedItems = new List<GemInventoryItemButton>();
    private readonly SlotGem[] selectedSlots = new SlotGem[3];
    private readonly List<GameObject> spawnedUpgradeFx = new List<GameObject>();

    private int activeElementTab = 0;
    private int lockedElement = -1;
    private int selectedSlotIndex = 0;
    private Vector2 resultStartAnchoredPos;
    private Tween resultMoveTween;
    private Tween resultFadeTween;
    private CanvasGroup resultCanvasGroup;
    private Coroutine upgradeRoutine;

    private struct SlotGem
    {
        public int elementId;
        public int level;
        public bool IsValid => elementId >= 0 && level >= 1 && level <= 5;
    }

    private void Start()
    {
        EnsureGemCollectionReference();
        InitializeResultFeedback();
        BindButtons();
        ResetSlots();
        RefreshElementButtonIcons();
        UpdateSlotSelectionVisual();

        if (forgePopupRoot != null)
            forgePopupRoot.SetActive(false);

        if (tabUpgradeGemRoot != null)
            tabUpgradeGemRoot.SetActive(true);

        UpdateRateText();
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

        KillResultTweens();
        ClearSpawnedUpgradeFx();
    }

    private void BindButtons()
    {
        if (openFromHomeButton != null)
        {
            openFromHomeButton.onClick.RemoveAllListeners();
            openFromHomeButton.onClick.AddListener(OpenForgePopup);
        }

        if (closeForgePopupButton != null)
        {
            closeForgePopupButton.onClick.RemoveAllListeners();
            closeForgePopupButton.onClick.AddListener(CloseForgePopup);
        }

        if (tabUpgradeGemButton != null)
        {
            tabUpgradeGemButton.onClick.RemoveAllListeners();
            tabUpgradeGemButton.onClick.AddListener(OpenUpgradeGemTab);
        }

        if (clearSlotsButton != null)
        {
            clearSlotsButton.onClick.RemoveAllListeners();
            clearSlotsButton.onClick.AddListener(OnClickClearSlots);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveAllListeners();
            upgradeButton.onClick.AddListener(OnClickUpgrade);
        }

        for (int i = 0; i < elementButtons.Length; i++)
        {
            Button btn = elementButtons[i];
            if (btn == null)
                continue;

            int elementIndex = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnClickElementTab(elementIndex));
        }

        for (int i = 0; i < slotButtons.Length; i++)
        {
            Button btn = slotButtons[i];
            if (btn == null)
                continue;

            int slotIndex = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnClickSlot(slotIndex));
        }
    }

    private void OpenForgePopup()
    {
        if (forgePopupRoot != null)
            forgePopupRoot.SetActive(true);

        OpenUpgradeGemTab();
    }

    private void CloseForgePopup()
    {
        if (forgePopupRoot != null)
            forgePopupRoot.SetActive(false);
    }

    private void OpenUpgradeGemTab()
    {
        if (tabUpgradeGemRoot != null)
            tabUpgradeGemRoot.SetActive(true);

        RebuildGemList();
    }

    private void OnClickElementTab(int elementIndex)
    {
        if (elementIndex < 0)
            return;

        if (lockedElement >= 0 && lockedElement != elementIndex)
        {
            SetResultLocalized("forge_gem_element_locked", "Element b? kh�a theo gem d?u ti�n.");
            return;
        }

        activeElementTab = elementIndex;
        RebuildGemList();
    }

    private void OnClickSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= selectedSlots.Length)
            return;

        if (!CanSelectSlot(slotIndex))
        {
            SetResultLocalized("forge_gem_select_slot_order", "H�y ch?n gem ? slot tru?c tru?c khi m? slot n�y.");
            return;
        }

        selectedSlotIndex = slotIndex;
        UpdateSlotSelectionVisual();
    }

    private void OnClickClearSlots()
    {
        ResetSlots();
        RebuildGemList();
        UpdateRateText();
        SetResult(string.Empty);
    }

    private void OnClickUpgrade()
    {
        if (upgradeRoutine != null)
            return;

        if (clearSlotsButton != null)
            clearSlotsButton.interactable = false;

        upgradeRoutine = StartCoroutine(UpgradeRoutine());
    }

    private IEnumerator UpgradeRoutine()
    {
        List<SlotGem> selected = GetSelectedGems();
        if (selected.Count < 2 || selected.Count > 3)
        {
            SetResultLocalized("forge_gem_need_two_or_three", "C?n ch?n 2-3 gem.");
            if (clearSlotsButton != null)
                clearSlotsButton.interactable = true;
            upgradeRoutine = null;
            yield break;
        }

        int element = selected[0].elementId;
        int[] levels = new int[selected.Count];
        List<GemDataV2> input = new List<GemDataV2>();

        for (int i = 0; i < selected.Count; i++)
        {
            levels[i] = selected[i].level;
            input.Add(new GemDataV2 { Level = selected[i].level });
        }

        if (PlayerManager.Instance == null || !PlayerManager.Instance.ConsumeOwnedGems(element, levels))
        {
            SetResultLocalized("forge_gem_not_enough", "Kh�ng d? gem d? upgrade.");
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUpgradeGemFailedSound();

            if (clearSlotsButton != null)
                clearSlotsButton.interactable = true;
            upgradeRoutine = null;
            yield break;
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayUpgradeGemProcessingSound();

        yield return StartCoroutine(PlayHammerEffectRoutine());

        GemUpgradeResultV2 result = GemUpgradeServiceV2.Upgrade(input);

        if (result.Success)
        {
            PlayerManager.Instance.AddOrUpdateOwnedGem(element, result.NewGemLevel, 1);
            string criticalFormat = GetLocalizedText("forge_gem_upgrade_critical", "Th�nh c�ng ch� m?ng! Gem Lv.{0}");
            string successFormat = GetLocalizedText("forge_gem_upgrade_success", "N�ng c?p th�nh c�ng! Gem Lv.{0}");
            SetResult(string.Format(result.IsCritical ? criticalFormat : successFormat, result.NewGemLevel));

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUpgradeGemSuccessSound();
        }
        else
        {
            // Check what type of failure and show appropriate message
            if (result.Message == "Target gem is max level")
            {
                SetResultLocalized("forge_gem_target_max_level", "Gem d� ? m?c t?i da (Lv.5).");
            }
            else if (result.Message == "Invalid input")
            {
                SetResultLocalized("forge_gem_invalid_input", "�?u v�o kh�ng h?p l?.");
            }
            else
            {
                // Regular failure - return gems
                for (int i = 0; i < result.RemainGems.Count; i++)
                {
                    GemDataV2 gem = result.RemainGems[i];
                    if (gem == null) continue;
                    PlayerManager.Instance.AddOrUpdateOwnedGem(element, gem.Level, 1);
                }
                SetResultLocalized("forge_gem_upgrade_fail_return", "N�ng c?p th?t b?i - tr? l?i ng?u nhi�n 1-2 gem.");
            }

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUpgradeGemFailedSound();
        }

        PlayerManager.Instance.SaveData();

        ResetSlots();
        RebuildGemList();
        UpdateRateText();

        if (clearSlotsButton != null)
            clearSlotsButton.interactable = true;

        upgradeRoutine = null;
    }

    private IEnumerator PlayHammerEffectRoutine()
    {
        ClearSpawnedUpgradeFx();

        if (upgradeFxPrefab != null)
        {
            for (int i = 0; i < selectedSlots.Length; i++)
            {
                if (!selectedSlots[i].IsValid)
                    continue;

                Transform slotRoot = GetSlotVisualRoot(i);
                if (slotRoot == null)
                    continue;

                GameObject fxInstance = Instantiate(upgradeFxPrefab, slotRoot);
                fxInstance.transform.localPosition = upgradeFxLocalOffset;
                fxInstance.transform.localRotation = Quaternion.identity;
                fxInstance.transform.localScale = upgradeFxLocalScale;
                fxInstance.SetActive(true);
                spawnedUpgradeFx.Add(fxInstance);
            }
        }

        float wait = Mathf.Max(0.1f, upgradeFxSeconds);
        yield return new WaitForSeconds(wait);

        ClearSpawnedUpgradeFx();
    }

    private Transform GetSlotVisualRoot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotIcons.Length)
            return transform;

        Image icon = slotIcons[slotIndex];
        if (icon != null)
            return icon.transform;

        Button btn = slotButtons[slotIndex];
        if (btn != null)
            return btn.transform;

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

    private void OnClickGemInventoryItem(int level)
    {
        int element = lockedElement >= 0 ? lockedElement : activeElementTab;
        if (element < 0)
            return;

        int slot = selectedSlotIndex;
        if (slot < 0 || slot >= selectedSlots.Length)
            slot = 0;

        if (!CanSelectSlot(slot))
        {
            SetResultLocalized("forge_gem_select_slot_order", "H�y ch?n gem ? slot tru?c tru?c khi m? slot n�y.");
            return;
        }

        int owned = PlayerManager.Instance != null ? PlayerManager.Instance.GetOwnedGemQuantity(element, level) : 0;
        int used = CountUsedGem(element, level);
        if (selectedSlots[slot].IsValid && selectedSlots[slot].elementId == element && selectedSlots[slot].level == level)
            used = Mathf.Max(0, used - 1);

        if (used >= owned)
        {
            SetResultLocalized("forge_gem_not_enough_level_qty", "Kh�ng d? s? lu?ng gem level n�y.");
            return;
        }

        selectedSlots[slot] = new SlotGem { elementId = element, level = level };
        if (lockedElement < 0)
            lockedElement = element;

        if (slot < selectedSlots.Length - 1 && CanSelectSlot(slot + 1))
            selectedSlotIndex = slot + 1;

        RefreshSlotsUI();
        UpdateSlotSelectionVisual();
        RebuildGemList();
        UpdateRateText();
        SetResult(string.Empty);
    }

    private void RebuildGemList()
    {
        ClearGemListItems();

        if (gemListContent == null || gemItemPrefab == null || PlayerManager.Instance == null)
            return;

        EnsureGemCollectionReference();
        if (gemCollection == null || gemCollection.elements == null)
            return;

        int element = lockedElement >= 0 ? lockedElement : activeElementTab;
        if (element < 0 || element >= gemCollection.elements.Length)
            return;

        for (int level = 1; level <= 5; level++)
        {
            int ownedQty = PlayerManager.Instance.GetOwnedGemQuantity(element, level);
            int usedQty = CountUsedGem(element, level);
            int availableQty = Mathf.Max(0, ownedQty - usedQty);
            if (availableQty <= 0)
                continue;

            Sprite icon = GetGemSprite(element, level);
            GemInventoryItemButton item = Instantiate(gemItemPrefab, gemListContent);
            item.Setup(level, availableQty, icon, OnClickGemInventoryItem);
            spawnedItems.Add(item);
        }
    }

    private void RefreshSlotsUI()
    {
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            SlotGem slot = selectedSlots[i];

            if (i < slotIcons.Length && slotIcons[i] != null)
            {
                if (slot.IsValid)
                {
                    slotIcons[i].sprite = GetGemSprite(slot.elementId, slot.level);
                    slotIcons[i].enabled = slotIcons[i].sprite != null;
                }
                else
                {
                    slotIcons[i].sprite = null;
                    slotIcons[i].enabled = false;
                }
            }

            if (i < slotLevelTexts.Length && slotLevelTexts[i] != null)
                slotLevelTexts[i].text = slot.IsValid ? ("Lv. " + slot.level) : string.Empty;
        }
    }

    private void UpdateRateText()
    {
        if (txtSuccessRate == null)
            return;

        List<SlotGem> selected = GetSelectedGems();
        if (selected.Count < 2)
        {
            txtSuccessRate.text = "0%";
            return;
        }

        List<GemDataV2> input = new List<GemDataV2>();
        for (int i = 0; i < selected.Count; i++)
            input.Add(new GemDataV2 { Level = selected[i].level });

        float rate = GemUpgradeServiceV2.CalculateSuccessRate(input);
        txtSuccessRate.text = $"{rate * 100f:F0}%";
    }

    private int CountUsedGem(int element, int level)
    {
        int used = 0;
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            SlotGem slot = selectedSlots[i];
            if (!slot.IsValid)
                continue;

            if (slot.elementId == element && slot.level == level)
                used++;
        }

        return used;
    }

    private int GetFirstEmptySlot()
    {
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (!selectedSlots[i].IsValid)
                return i;
        }

        return -1;
    }

    private int GetSelectedCount()
    {
        int count = 0;
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (selectedSlots[i].IsValid)
                count++;
        }

        return count;
    }

    private List<SlotGem> GetSelectedGems()
    {
        List<SlotGem> result = new List<SlotGem>();
        for (int i = 0; i < selectedSlots.Length; i++)
        {
            if (selectedSlots[i].IsValid)
                result.Add(selectedSlots[i]);
        }

        return result;
    }

    private void ResetSlots()
    {
        for (int i = 0; i < selectedSlots.Length; i++)
            selectedSlots[i] = default(SlotGem);

        lockedElement = -1;
        selectedSlotIndex = 0;
        RefreshSlotsUI();
        UpdateSlotSelectionVisual();
    }

    private void HandlePlayerDataChanged()
    {
        RebuildGemList();
        UpdateRateText();
        RefreshElementButtonIcons();
    }

    private Sprite GetGemSprite(int element, int level)
    {
        EnsureGemCollectionReference();
        if (gemCollection == null || gemCollection.elements == null)
            return null;

        if (element < 0 || element >= gemCollection.elements.Length)
            return null;

        GemCollection.GemElementData elementData = gemCollection.elements[element];
        if (elementData == null || elementData.gemLevels == null)
            return null;

        int index = level - 1;
        if (index < 0 || index >= elementData.gemLevels.Length)
            return null;

        GemCollection.GemLevelData levelData = elementData.gemLevels[index];
        return levelData != null ? levelData.sprite : null;
    }

    private void EnsureGemCollectionReference()
    {
        if (gemCollection != null)
            return;

        if (GameDataManager.Instance != null)
            gemCollection = GameDataManager.Instance.GemCollectionObject as GemCollection;
    }

    private void ClearGemListItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            GemInventoryItemButton item = spawnedItems[i];
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }

    private void SetResult(string text)
    {
        if (txtResult == null)
            return;

        txtResult.text = text;

        if (string.IsNullOrEmpty(text))
        {
            if (resultCanvasGroup != null)
                resultCanvasGroup.alpha = 0f;
            return;
        }

        PlayResultAnimation();
    }

    private void SetResultLocalized(string id, string fallbackVi)
    {
        SetResult(GetLocalizedText(id, fallbackVi));
    }

    private bool CanSelectSlot(int slotIndex)
    {
        if (slotIndex <= 0)
            return true;

        for (int i = 0; i < slotIndex; i++)
        {
            if (!selectedSlots[i].IsValid)
                return false;
        }

        return true;
    }

    private void UpdateSlotSelectionVisual()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            Button btn = slotButtons[i];
            if (btn == null)
                continue;

            Image img = btn.GetComponent<Image>();
            if (img != null)
                img.color = (i == selectedSlotIndex) ? slotSelectedColor : slotNormalColor;
        }
    }

    private void RefreshElementButtonIcons()
    {
        EnsureGemCollectionReference();
        if (gemCollection == null || gemCollection.elements == null)
            return;

        for (int i = 0; i < elementButtons.Length; i++)
        {
            Button btn = elementButtons[i];
            if (btn == null)
                continue;

            if (i < 0 || i >= gemCollection.elements.Length)
                continue;

            GemCollection.GemElementData elementData = gemCollection.elements[i];
            if (elementData == null || elementData.gemLevels == null || elementData.gemLevels.Length == 0)
                continue;

            Sprite icon = elementData.gemLevels[0] != null ? elementData.gemLevels[0].sprite : null;
            Image buttonImage = btn.GetComponent<Image>();
            if (buttonImage != null && icon != null)
                buttonImage.sprite = icon;
        }
    }

    private void InitializeResultFeedback()
    {
        if (txtResult == null)
            return;

        RectTransform rt = txtResult.GetComponent<RectTransform>();
        if (rt != null)
            resultStartAnchoredPos = rt.anchoredPosition;

        resultCanvasGroup = txtResult.GetComponent<CanvasGroup>();
        if (resultCanvasGroup == null)
            resultCanvasGroup = txtResult.gameObject.AddComponent<CanvasGroup>();

        resultCanvasGroup.alpha = 0f;
    }

    private void PlayResultAnimation()
    {
        if (txtResult == null)
            return;

        RectTransform rt = txtResult.GetComponent<RectTransform>();
        if (rt == null)
            return;

        KillResultTweens();

        rt.anchoredPosition = resultStartAnchoredPos;
        if (resultCanvasGroup != null)
            resultCanvasGroup.alpha = 1f;

        float duration = Mathf.Max(0.1f, resultAnimDuration);
        resultMoveTween = rt.DOAnchorPosY(resultStartAnchoredPos.y + resultFloatY, duration).SetEase(Ease.OutQuad);
        if (resultCanvasGroup != null)
        {
            resultFadeTween = resultCanvasGroup.DOFade(0f, duration).SetEase(Ease.OutQuad);
            resultFadeTween.OnComplete(() =>
            {
                rt.anchoredPosition = resultStartAnchoredPos;
            });
        }
    }

    private void KillResultTweens()
    {
        if (resultMoveTween != null)
        {
            resultMoveTween.Kill();
            resultMoveTween = null;
        }

        if (resultFadeTween != null)
        {
            resultFadeTween.Kill();
            resultFadeTween = null;
        }
    }

    private string GetLocalizedText(string id, string fallback)
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
            return LocalizationManager.Instance.GetText(id, fallback);

        return fallback;
    }
}

public class GemDataV2
{
    public int Level;
}

public class GemUpgradeResultV2
{
    public bool Success;
    public bool IsCritical;
    public float SuccessRate;
    public float Roll;
    public int TargetLevel;
    public int NewGemLevel;
    public string Message;
    public readonly List<GemDataV2> RemainGems = new List<GemDataV2>();
}

public static class GemUpgradeConfigV2
{
    public static float GetBaseRate(int level)
    {
        switch (level)
        {
            case 1: return 0.80f;
            case 2: return 0.70f;
            case 3: return 0.55f;
            case 4: return 0.40f;
            default: return 0f;
        }
    }

    public const float MAX_RATE = 0.90f;
    public const float MIN_RATE = 0.05f;
    public const float CRIT_RATE = 0.05f;
}

public static class GemUpgradeServiceV2
{
    public static float CalculateSuccessRate(List<GemDataV2> gems)
    {
        if (!ValidateInput(gems))
            return 0f;

        int targetIndex = GetTargetIndex(gems, false);
        if (targetIndex < 0)
            return 0f;

        GemDataV2 target = gems[targetIndex];
        if (target.Level >= 5)
            return 0f;

        float baseRate = GemUpgradeConfigV2.GetBaseRate(target.Level);
        float bonus = 0f;

        for (int i = 0; i < gems.Count; i++)
        {
            if (i == targetIndex)
                continue;

            int delta = gems[i].Level - target.Level;
            if (delta >= 0) bonus += 0.10f;
            else if (delta == -1) bonus += 0.05f;
            else if (delta == -2) bonus += 0f;
            else bonus -= 0.10f;
        }

        bool sameLevel = gems.All(g => g.Level == target.Level);
        if (sameLevel)
            bonus += 0.10f;

        if (gems.Count == 3)
            bonus += 0.05f;

        return Mathf.Clamp(baseRate + bonus, GemUpgradeConfigV2.MIN_RATE, GemUpgradeConfigV2.MAX_RATE);
    }

    public static GemUpgradeResultV2 Upgrade(List<GemDataV2> gems)
    {
        GemUpgradeResultV2 result = new GemUpgradeResultV2();
        if (!ValidateInput(gems))
        {
            result.Success = false;
            result.Message = "Invalid input";
            return result;
        }

        int targetIndex = GetTargetIndex(gems, true);
        GemDataV2 target = gems[targetIndex];
        result.TargetLevel = target.Level;

        if (target.Level >= 5)
        {
            result.Success = false;
            result.Message = "Target gem is max level";
            return result;
        }

        result.SuccessRate = CalculateSuccessRate(gems);
        result.Roll = Random.value;

        if (result.Roll <= result.SuccessRate)
        {
            int newLevel = target.Level + 1;
            if (Random.value <= GemUpgradeConfigV2.CRIT_RATE)
            {
                newLevel += 1;
                result.IsCritical = true;
            }

            result.NewGemLevel = Mathf.Min(newLevel, 5);
            result.Success = true;
            result.Message = result.IsCritical ? "Critical success" : "Success";
            return result;
        }

        result.Success = false;
        result.Message = "Fail";

        int returnCount = Random.Range(1, 3);
        List<GemDataV2> shuffled = gems.OrderBy(x => Random.value).ToList();
        for (int i = 0; i < returnCount && i < shuffled.Count; i++)
            result.RemainGems.Add(new GemDataV2 { Level = shuffled[i].Level });

        return result;
    }

    private static bool ValidateInput(List<GemDataV2> gems)
    {
        if (gems == null || gems.Count < 2 || gems.Count > 3)
            return false;

        for (int i = 0; i < gems.Count; i++)
        {
            if (gems[i] == null || gems[i].Level < 1 || gems[i].Level > 5)
                return false;
        }

        return true;
    }

    private static int GetTargetIndex(List<GemDataV2> gems, bool randomOnTie)
    {
        int maxLevel = gems.Max(g => g.Level);
        List<int> candidates = new List<int>();
        for (int i = 0; i < gems.Count; i++)
        {
            if (gems[i].Level == maxLevel)
                candidates.Add(i);
        }

        if (candidates.Count == 0)
            return -1;

        if (!randomOnTie || candidates.Count == 1)
            return candidates[0];

        return candidates[Random.Range(0, candidates.Count)];
    }
}






