using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrebattleGuardianPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button closeButton;

    [Header("List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private PrebattleGuardianItemButton itemPrefab;

    [Header("Data")]
    [SerializeField] private GuardianDatabase guardianDatabase;

    private readonly List<PrebattleGuardianItemButton> spawnedItems = new List<PrebattleGuardianItemButton>();
    private Action<int> onGuardianSelected;
    private int selectedGuardianId = -1;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        SetVisible(false);
    }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged += RebuildOwnedGuardians;

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLocalizationLoaded += RebuildOwnedGuardians;
            LocalizationManager.Instance.OnLanguageChanged += RebuildOwnedGuardians;
        }
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged -= RebuildOwnedGuardians;

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLocalizationLoaded -= RebuildOwnedGuardians;
            LocalizationManager.Instance.OnLanguageChanged -= RebuildOwnedGuardians;
        }
    }

    public void Open(int currentGuardianId, Action<int> onSelected)
    {
        selectedGuardianId = currentGuardianId;
        onGuardianSelected = onSelected;
        TryResolveDatabase();
        RebuildOwnedGuardians();
        SetVisible(true);
    }

    public void Close()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (popupRoot != null)
            popupRoot.SetActive(visible);
    }

    private void TryResolveDatabase()
    {
        if (guardianDatabase != null)
            return;

        if (GameDataManager.Instance != null)
            guardianDatabase = GameDataManager.Instance.GuardianDatabase;
    }

    private void RebuildOwnedGuardians()
    {
        ClearItems();

        if (contentRoot == null || itemPrefab == null)
            return;

        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null || PlayerManager.Instance.Data.ownedGuardians == null)
            return;

        if (guardianDatabase == null)
            return;

        List<OwnedGuardianData> owned = PlayerManager.Instance.Data.ownedGuardians;
        for (int i = 0; i < owned.Count; i++)
        {
            OwnedGuardianData ownedGuardian = owned[i];
            if (ownedGuardian == null)
                continue;

            GuardianDataAsset guardian = guardianDatabase.GetGuardianById(ownedGuardian.guardianId);
            if (guardian == null)
                continue;

            bool isSelected = guardian.guardianId == selectedGuardianId;
            string displayName = GetLocalizedText(guardian.guardianName, guardian.guardianName);
            Sprite avatar = guardian.avatarIcon;

            PrebattleGuardianItemButton item = Instantiate(itemPrefab, contentRoot);
            item.Setup(guardian.guardianId, displayName, avatar, isSelected, OnSelectGuardian);
            spawnedItems.Add(item);
        }
    }

    private void OnSelectGuardian(int guardianId)
    {
        selectedGuardianId = guardianId;
        UpdateSelectionIndicators();
        if (onGuardianSelected != null)
            onGuardianSelected.Invoke(guardianId);
    }

    private void UpdateSelectionIndicators()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            PrebattleGuardianItemButton item = spawnedItems[i];
            if (item == null)
                continue;

            item.SetSelected(item.GuardianId == selectedGuardianId);
        }
    }

    private void ClearItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
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
}
