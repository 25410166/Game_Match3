using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;

public class PrebattlePopupUI : MonoBehaviour
{
    public static Button StartButtonStatic { get; private set; }
    public enum PrebattleOpenSource
    {
        MapPopup,
        GuardianMap
    }
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI txtMapName;

    [Header("Pet")]
    [SerializeField] private Button changePetButton;
    [SerializeField] private PopupSelectPet popupSelectPet;
    [SerializeField] private Transform playerPetSpawnRoot;
    [SerializeField] private Transform enemyPetSpawnRoot;
    [SerializeField] private Vector3 playerPetScale = Vector3.one;
    [SerializeField] private Vector3 enemyPetScale = new Vector3(-1f, 1f, 1f);
    [SerializeField] private string petSortingLayerName = "UI";
    [SerializeField] private int playerPetSortingOrder = 12;
    [SerializeField] private int enemyPetSortingOrder = 11;

    [Header("Guardian")]
    [SerializeField] private Button guardianButton;
    [SerializeField] private Transform guardianPreviewRoot;
    [SerializeField] private Transform enemyGuardianPreviewRoot;
    [SerializeField] private Vector3 guardianPreviewScale = Vector3.one;
    [SerializeField] private Vector3 enemyGuardianPreviewScale = new Vector3(-1f, 1f, 1f);
    [SerializeField] private PrebattleGuardianPopupUI guardianPopup;
    [SerializeField] private Color guardianEnabledColor = Color.white;
    [SerializeField] private Color guardianDisabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private int playerGuardianSortingOrder = 12;
    [SerializeField] private int enemyGuardianSortingOrder = 11;

    [Header("Cards")]
    [SerializeField] private Button[] cardSlotButtons = new Button[5];
    [SerializeField] private Image[] cardSlotIcons = new Image[5];
    [SerializeField] private Transform cardListContent;
    [SerializeField] private PrebattleCardItemButton cardItemPrefab;
    [SerializeField] private ParticleSystem[] cardSlotSelectedFx = new ParticleSystem[5];

    [Header("Card Info Popup")]
    [SerializeField] private GameObject popupCardInfo;
    [SerializeField] private TextMeshProUGUI txtCardInfoDescription;
    [SerializeField] private RectTransform popupCardInfoRect;

    private readonly List<PrebattleCardItemButton> spawnedCardItems = new List<PrebattleCardItemButton>();
    private readonly PrebattleCardData[] selectedCards = new PrebattleCardData[5];

    private MapDataAsset currentMap;
    private int selectedPlayerPetId = -1;
    private int selectedCardSlot = 0;
    private int selectedGuardianId = -1;

    private GuardianDatabase guardianDatabase;

    private GameObject spawnedPlayerPet;
    private GameObject spawnedEnemyPet;
    private GameObject spawnedGuardianPreview;
    private GameObject spawnedEnemyGuardianPreview;
    private bool isPopupVisible;
    public PrebattleOpenSource LastOpenSource { get; private set; } = PrebattleOpenSource.MapPopup;

    public event Action<PrebattleOpenSource> OnOpened;
    public event Action<PrebattleOpenSource> OnClosed;

    private void Start()
    {
        BindButtons();

        if (popupRoot != null)
            popupRoot.SetActive(false);

        isPopupVisible = false;

        if (popupSelectPet != null)
            popupSelectPet.Close();

        if (guardianPopup != null)
            guardianPopup.Close();

        if (guardianPreviewRoot == null)
            guardianPreviewRoot = transform;

        RefreshCardSlotsUI();
        RefreshGuardianUI();
        UpdateCardSlotSelectionFx();
    }

    private void BindButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (startButton != null)
        {
            StartButtonStatic = startButton;
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnClickStart);
        }

        if (changePetButton != null)
        {
            changePetButton.onClick.RemoveAllListeners();
            changePetButton.onClick.AddListener(OnClickChangePet);
        }

        if (guardianButton != null)
        {
            guardianButton.onClick.RemoveAllListeners();
            guardianButton.onClick.AddListener(OnClickGuardianButton);
        }

        for (int i = 0; i < cardSlotButtons.Length; i++)
        {
            Button btn = cardSlotButtons[i];
            if (btn == null)
                continue;

            int slot = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnClickCardSlot(slot));
        }
    }

    public void Open(MapDataAsset map)
    {
        Open(map, PrebattleOpenSource.MapPopup);
    }

    public void Open(MapDataAsset map, PrebattleOpenSource source)
    {
        LastOpenSource = source;
        currentMap = map;
        if (currentMap == null)
            return;

        if (popupRoot != null)
            popupRoot.SetActive(true);

        isPopupVisible = true;

        if (txtMapName != null)
            txtMapName.text = GetLocalizedText(currentMap.mapName, currentMap.mapName);

        AreaBackgroundManager.SetAreaBackground(currentMap.area);
        SpawnEnemyPet(ResolveEnemyPetId(currentMap));
        SpawnEnemyGuardianPreviewFromMap();

        LoadSavedSelectionFromPlayer();
        AutoSelectFirstOwnedPetIfNeeded();
        AutoSelectOwnedGuardianIfNeeded();
        RebuildCardList();
        RefreshCardSlotsUI();
        RefreshGuardianUI();
        UpdateCardSlotSelectionFx();

        if (OnOpened != null)
            OnOpened.Invoke(LastOpenSource);

        if (TutorialProgressManager.Instance != null)
            TutorialProgressManager.Instance.NotifyPrebattleOpened();
    }

    public void Close()
    {
        isPopupVisible = false;

        if (popupRoot != null)
            popupRoot.SetActive(false);

        if (popupSelectPet != null)
            popupSelectPet.Close();

        if (guardianPopup != null)
            guardianPopup.Close();

        ClearSpawnedPets();
        ClearGuardianPreview();
        ClearEnemyGuardianPreview();
        UpdateCardSlotSelectionFx();

        if (OnClosed != null)
            OnClosed.Invoke(LastOpenSource);
    }

    private void OnDestroy()
    {
        ClearSpawnedPets();
    }

    private void OnClickChangePet()
    {
        EnsurePrebattlePetsSortingBelowSelectPopup();

        if (popupSelectPet != null)
            popupSelectPet.Open(OnSelectOwnedPet);
    }

    private void OnClickGuardianButton()
    {
        if (guardianPopup == null)
            return;

        if (!HasOwnedGuardian())
            return;

        guardianPopup.Open(selectedGuardianId, OnSelectGuardian);
    }

    private void OnSelectGuardian(int guardianId)
    {
        selectedGuardianId = guardianId;
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.TryEquipGuardian(guardianId);
        RefreshGuardianUI();
    }

    private void OnSelectOwnedPet(int petId)
    {
        selectedPlayerPetId = petId;
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.TrySetLastSelectedPetId(petId);
        SpawnPlayerPet(petId);
    }

    private void LoadSavedSelectionFromPlayer()
    {
        if (PlayerManager.Instance == null)
            return;

        int savedPetId = PlayerManager.Instance.GetLastSelectedPetId();
        if (savedPetId >= 0 && PlayerManager.Instance.GetOwnedPet(savedPetId) != null)
            selectedPlayerPetId = savedPetId;

        int savedGuardianId = PlayerManager.Instance.GetEquippedGuardianId();
        if (savedGuardianId >= 0 && PlayerManager.Instance.IsGuardianOwned(savedGuardianId))
            selectedGuardianId = savedGuardianId;
    }

    private void AutoSelectOwnedGuardianIfNeeded()
    {
        if (selectedGuardianId >= 0 && PlayerManager.Instance != null && PlayerManager.Instance.IsGuardianOwned(selectedGuardianId))
            return;

        selectedGuardianId = GetDefaultOwnedGuardianId();
    }

    private void AutoSelectFirstOwnedPetIfNeeded()
    {
        if (selectedPlayerPetId >= 0)
        {
            SpawnPlayerPet(selectedPlayerPetId);
            return;
        }

        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null || PlayerManager.Instance.Data.ownedPets == null)
            return;

        if (PlayerManager.Instance.Data.ownedPets.Count <= 0)
            return;

        selectedPlayerPetId = PlayerManager.Instance.Data.ownedPets[0].petId;
        SpawnPlayerPet(selectedPlayerPetId);
    }

    private int GetDefaultOwnedGuardianId()
    {
        if (PlayerManager.Instance != null)
        {
            int equippedId = PlayerManager.Instance.GetEquippedGuardianId();
            if (equippedId >= 0 && PlayerManager.Instance.IsGuardianOwned(equippedId))
                return equippedId;
        }

        return GetFirstOwnedGuardianId();
    }

    private int GetFirstOwnedGuardianId()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null || PlayerManager.Instance.Data.ownedGuardians == null)
            return -1;

        List<OwnedGuardianData> owned = PlayerManager.Instance.Data.ownedGuardians;
        for (int i = 0; i < owned.Count; i++)
        {
            OwnedGuardianData data = owned[i];
            if (data != null)
                return data.guardianId;
        }

        return -1;
    }

    private bool HasOwnedGuardian()
    {
        return GetFirstOwnedGuardianId() >= 0;
    }

    private void RefreshGuardianUI()
    {
        if (!isPopupVisible)
        {
            ClearGuardianPreview();
            return;
        }

        TryResolveGuardianDatabase();
        bool hasGuardian = HasOwnedGuardian();

        if (!hasGuardian)
        {
            selectedGuardianId = -1;
            SetGuardianButtonInteractable(false);
            ClearGuardianPreview();
            return;
        }

        if (selectedGuardianId < 0)
            selectedGuardianId = GetDefaultOwnedGuardianId();

        GuardianDataAsset guardian = guardianDatabase != null
            ? guardianDatabase.GetGuardianById(selectedGuardianId)
            : null;

        if (guardian == null)
        {
            selectedGuardianId = GetDefaultOwnedGuardianId();
            guardian = guardianDatabase != null
                ? guardianDatabase.GetGuardianById(selectedGuardianId)
                : null;
        }

        SpawnGuardianPreview(guardian != null ? guardian.guardianPrefab : null);

        SetGuardianButtonInteractable(true);
    }

    private void TryResolveGuardianDatabase()
    {
        if (guardianDatabase != null)
            return;

        if (GameDataManager.Instance != null)
            guardianDatabase = GameDataManager.Instance.GuardianDatabase;
    }

    private void SetGuardianButtonInteractable(bool interactable)
    {
        if (guardianButton != null)
            guardianButton.interactable = interactable;

        Image targetImage = guardianButton != null ? guardianButton.targetGraphic as Image : null;
        if (targetImage != null)
            targetImage.color = interactable ? guardianEnabledColor : guardianDisabledColor;
    }

    private void SpawnPlayerPet(int petId)
    {
        if (GameDataManager.Instance == null)
            return;

        OwnedPetData owned = PlayerManager.Instance != null ? PlayerManager.Instance.GetOwnedPet(petId) : null;
        int petLevel = owned != null ? Mathf.Max(1, owned.petLevel) : 1;
        string fallbackName = owned != null ? owned.petName : string.Empty;

        GameObject prefab = GameDataManager.Instance.GetPetPrefabForLevel(petId, petLevel, fallbackName);
        if (prefab == null)
            return;

        if (spawnedPlayerPet != null)
            Destroy(spawnedPlayerPet);

        spawnedPlayerPet = Instantiate(prefab, playerPetSpawnRoot);
        spawnedPlayerPet.transform.localPosition = Vector3.zero;
        spawnedPlayerPet.transform.localRotation = Quaternion.identity;
        spawnedPlayerPet.transform.localScale = playerPetScale;

        ForceIdle(spawnedPlayerPet);
        ApplyShortLayer(spawnedPlayerPet, playerPetSortingOrder);
    }

    private void SpawnEnemyPet(int petId)
    {
        if (GameDataManager.Instance == null)
            return;

        int petLevel = currentMap != null ? Mathf.Max(1, currentMap.petLevelSpawn) : 1;
        GameObject prefab = GameDataManager.Instance.GetPetPrefabForLevel(petId, petLevel, string.Empty);
        if (prefab == null)
            return;

        if (spawnedEnemyPet != null)
            Destroy(spawnedEnemyPet);

        spawnedEnemyPet = Instantiate(prefab, enemyPetSpawnRoot);
        spawnedEnemyPet.transform.localPosition = Vector3.zero;
        spawnedEnemyPet.transform.localRotation = Quaternion.identity;
        spawnedEnemyPet.transform.localScale = enemyPetScale;

        ForceIdle(spawnedEnemyPet);
        ApplyShortLayer(spawnedEnemyPet, enemyPetSortingOrder);
    }

    private static void ForceIdle(GameObject go)
    {
        if (go == null)
            return;

        SkeletonAnimation skel = go.GetComponentInChildren<SkeletonAnimation>(true);
        if (skel != null && skel.state != null)
            skel.state.SetAnimation(0, "Idle", true);
    }

    private void ApplyShortLayer(GameObject go, int sortingOrder)
    {
        if (go == null)
            return;

        ShortLayer shortLayer = go.GetComponent<ShortLayer>();
        if (shortLayer == null)
            shortLayer = go.AddComponent<ShortLayer>();

        shortLayer.SortingLayerName = petSortingLayerName;
        shortLayer.SortingOrder = Mathf.Min(sortingOrder, 19);
        shortLayer.Apply();
    }

    private void EnsurePrebattlePetsSortingBelowSelectPopup()
    {
        if (spawnedPlayerPet != null)
            ApplyShortLayer(spawnedPlayerPet, playerPetSortingOrder);

        if (spawnedEnemyPet != null)
            ApplyShortLayer(spawnedEnemyPet, enemyPetSortingOrder);

        if (spawnedGuardianPreview != null)
            ApplyShortLayer(spawnedGuardianPreview, playerGuardianSortingOrder);

        if (spawnedEnemyGuardianPreview != null)
            ApplyShortLayer(spawnedEnemyGuardianPreview, enemyGuardianSortingOrder);
    }

    private void OnClickCardSlot(int slot)
    {
        selectedCardSlot = Mathf.Clamp(slot, 0, selectedCards.Length - 1);
        UpdateCardSlotSelectionFx();
    }

    private void RebuildCardList()
    {
        ClearCardList();

        if (cardListContent == null || cardItemPrefab == null || PlayerManager.Instance == null || GameDataManager.Instance == null)
            return;

        CardDatabase cardDb = GameDataManager.Instance.CardDatabaseObject as CardDatabase;
        if (cardDb == null || PlayerManager.Instance.Data == null || PlayerManager.Instance.Data.ownedCards == null)
            return;

        List<OwnedCardData> owned = PlayerManager.Instance.Data.ownedCards;
        for (int i = 0; i < owned.Count; i++)
        {
            OwnedCardData item = owned[i];
            if (item == null || item.quantity <= 0)
                continue;

            CardDatabase.Card card = cardDb.GetCardById(item.cardId);
            if (card == null)
                continue;

            CardDatabase.CardLevel levelData = card.GetLevel(item.cardLevel);
            Sprite icon = levelData != null && levelData.sprite != null ? levelData.sprite : card.mainSprite;
            string displayName = card.cardName + " Lv" + item.cardLevel;

            PrebattleCardItemButton cardItem = Instantiate(cardItemPrefab, cardListContent);
            cardItem.Setup(item.cardId, item.cardLevel, displayName, item.quantity, icon, OnSelectCardFromList, OnShowCardInfo);
            spawnedCardItems.Add(cardItem);
        }
    }

    private void OnSelectCardFromList(int cardId, int cardLevel)
    {
        // Kiểm tra xem card này đã được chọn bao nhiêu lần rồi
        int usageCount = CountCardUsage(cardId);

        // Lấy original quantity của card này
        int originalQuantity = GetCardQuantity(cardId, cardLevel);
        if (originalQuantity <= 0)
            return;

        // Nếu đã chọn card này đủ số lượng rồi, không cho chọn thêm
        if (usageCount >= originalQuantity)
        {
            Debug.LogWarning($"Card {cardId} Lv{cardLevel} đã được chọn hết số lượng ({usageCount}/{originalQuantity}). Không thể chọn thêm!");
            return;
        }

        selectedCards[selectedCardSlot] = new PrebattleCardData
        {
            cardId = cardId,
            cardLevel = cardLevel
        };

        RefreshCardSlotsUI();
    }

    private void RefreshCardSlotsUI()
    {
        CardDatabase cardDb = GameDataManager.Instance != null ? GameDataManager.Instance.CardDatabaseObject as CardDatabase : null;

        for (int i = 0; i < selectedCards.Length; i++)
        {
            PrebattleCardData data = selectedCards[i];

            if (i < cardSlotIcons.Length && cardSlotIcons[i] != null)
            {
                if (data != null && cardDb != null)
                {
                    CardDatabase.Card card = cardDb.GetCardById(data.cardId);
                    CardDatabase.CardLevel lv = card != null ? card.GetLevel(data.cardLevel) : null;
                    cardSlotIcons[i].sprite = lv != null && lv.sprite != null ? lv.sprite : (card != null ? card.mainSprite : null);
                    cardSlotIcons[i].enabled = cardSlotIcons[i].sprite != null;
                }
                else
                {
                    cardSlotIcons[i].sprite = null;
                    cardSlotIcons[i].enabled = false;
                }
            }

            
        }

        UpdateCardSlotSelectionFx();
    }

    private void UpdateCardSlotSelectionFx()
    {
        if (cardSlotSelectedFx == null || cardSlotSelectedFx.Length == 0)
            return;

        bool isVisible = isPopupVisible;
        int selected = Mathf.Clamp(selectedCardSlot, 0, selectedCards.Length - 1);

        for (int i = 0; i < cardSlotSelectedFx.Length; i++)
        {
            ParticleSystem fx = cardSlotSelectedFx[i];
            if (fx == null)
                continue;

            bool shouldPlay = isVisible && i == selected;
            if (shouldPlay)
            {
                if (!fx.gameObject.activeSelf)
                    fx.gameObject.SetActive(true);
                if (!fx.isPlaying)
                    fx.Play();
            }
            else
            {
                if (fx.isPlaying)
                    fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                if (fx.gameObject.activeSelf)
                    fx.gameObject.SetActive(false);
            }
        }
    }

    private void OnClickStart()
    {
        if (selectedPlayerPetId < 0 || currentMap == null || GameDataManager.Instance == null)
            return;

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.TrySetLastSelectedPetId(selectedPlayerPetId);
            if (selectedGuardianId >= 0)
                PlayerManager.Instance.TryEquipGuardian(selectedGuardianId);
        }

        PrebattleSelectionData.Clear();
        PrebattleSelectionData.MapId = currentMap.mapId;
        PrebattleSelectionData.MapArea = currentMap.area;
        PrebattleSelectionData.PlayerPetId = selectedPlayerPetId;
        PrebattleSelectionData.PlayerPetLevel = ResolveOwnedPetLevel(selectedPlayerPetId);
        PrebattleSelectionData.EnemyPetId = ResolveEnemyPetId(currentMap);
        PrebattleSelectionData.EnemyPetLevel = Mathf.Max(1, currentMap.petLevelSpawn);
        PrebattleSelectionData.EnemyGuardianId = currentMap.idGuadiant;
        PrebattleSelectionData.EnemyGuardianLevel = Mathf.Max(1, currentMap.levelGuadiant);
        PrebattleSelectionData.GuardianId = selectedGuardianId;
        PrebattleSelectionData.GuardianLevel = ResolveOwnedGuardianLevel(selectedGuardianId);

        Debug.Log($"[PrebattlePopupUI] Start battle mapId={PrebattleSelectionData.MapId}, area={PrebattleSelectionData.MapArea}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetBattleData(
                PrebattleSelectionData.PlayerPetId,
                PrebattleSelectionData.PlayerPetLevel,
                PrebattleSelectionData.EnemyPetId,
                PrebattleSelectionData.EnemyPetLevel,
                PrebattleSelectionData.EnemyGuardianId,
                PrebattleSelectionData.EnemyGuardianLevel);
        }

        for (int i = 0; i < selectedCards.Length; i++)
        {
            if (selectedCards[i] == null)
                continue;

            PrebattleSelectionData.SelectedCards.Add(new PrebattleCardData
            {
                cardId = selectedCards[i].cardId,
                cardLevel = selectedCards[i].cardLevel
            });
        }

        OwnedPetData ownedPet = PlayerManager.Instance != null ? PlayerManager.Instance.GetOwnedPet(selectedPlayerPetId) : null;
        int playerLevel = ownedPet != null ? Mathf.Max(1, ownedPet.petLevel) : 1;
        string playerFallbackName = ownedPet != null ? ownedPet.petName : string.Empty;
        int enemyLevel = currentMap != null ? Mathf.Max(1, currentMap.petLevelSpawn) : 1;

        GameObject playerPrefab = GameDataManager.Instance.GetPetPrefabForLevel(selectedPlayerPetId, playerLevel, playerFallbackName);
        GameObject enemyPrefab = GameDataManager.Instance.GetPetPrefabForLevel(PrebattleSelectionData.EnemyPetId, enemyLevel, string.Empty);

        if (playerPrefab != null)
            PlayerPrefs.SetString("PlayerPet", playerPrefab.name);
        if (enemyPrefab != null)
            PlayerPrefs.SetString("EnemyPet", enemyPrefab.name);
        PlayerPrefs.Save();

        if (TutorialProgressManager.Instance != null)
            TutorialProgressManager.Instance.NotifyPrebattleStartClicked();

        GameSceneManager gsm = FindObjectOfType<GameSceneManager>();
        if (gsm != null)
            gsm.LoadBattle();
    }

    private void ClearCardList()
    {
        for (int i = 0; i < spawnedCardItems.Count; i++)
        {
            PrebattleCardItemButton item = spawnedCardItems[i];
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedCardItems.Clear();
    }

    private void ClearSpawnedPets()
    {
        if (spawnedPlayerPet != null)
        {
            Destroy(spawnedPlayerPet);
            spawnedPlayerPet = null;
        }

        if (spawnedEnemyPet != null)
        {
            Destroy(spawnedEnemyPet);
            spawnedEnemyPet = null;
        }

        ClearGuardianPreview();
        ClearEnemyGuardianPreview();
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

        ForceIdle(spawnedGuardianPreview);
        ApplyShortLayer(spawnedGuardianPreview, playerGuardianSortingOrder);
    }

    private void ClearGuardianPreview()
    {
        if (spawnedGuardianPreview != null)
            Destroy(spawnedGuardianPreview);

        spawnedGuardianPreview = null;
    }

    private void SpawnEnemyGuardianPreviewFromMap()
    {
        ClearEnemyGuardianPreview();
        if (currentMap == null || currentMap.idGuadiant <= 0)
            return;

        TryResolveGuardianDatabase();
        GuardianDataAsset guardian = guardianDatabase != null
            ? guardianDatabase.GetGuardianById(currentMap.idGuadiant)
            : null;

        if (guardian == null || guardian.guardianPrefab == null || enemyGuardianPreviewRoot == null)
            return;

        spawnedEnemyGuardianPreview = Instantiate(guardian.guardianPrefab, enemyGuardianPreviewRoot);
        spawnedEnemyGuardianPreview.transform.localPosition = Vector3.zero;
        spawnedEnemyGuardianPreview.transform.localRotation = Quaternion.identity;
        spawnedEnemyGuardianPreview.transform.localScale = new Vector3(-Mathf.Abs(enemyGuardianPreviewScale.x), enemyGuardianPreviewScale.y, enemyGuardianPreviewScale.z);

        ForceIdle(spawnedEnemyGuardianPreview);
        ApplyShortLayer(spawnedEnemyGuardianPreview, enemyGuardianSortingOrder);
    }

    private void ClearEnemyGuardianPreview()
    {
        if (spawnedEnemyGuardianPreview != null)
            Destroy(spawnedEnemyGuardianPreview);

        spawnedEnemyGuardianPreview = null;
    }

    private int ResolveOwnedPetLevel(int petId)
    {
        if (PlayerManager.Instance == null)
            return 1;

        OwnedPetData owned = PlayerManager.Instance.GetOwnedPet(petId);
        return owned != null ? Mathf.Max(1, owned.petLevel) : 1;
    }

    private int ResolveOwnedGuardianLevel(int guardianId)
    {
        if (guardianId < 0 || PlayerManager.Instance == null)
            return 1;

        OwnedGuardianData owned = PlayerManager.Instance.GetOwnedGuardian(guardianId);
        return owned != null ? Mathf.Max(1, owned.level) : 1;
    }

    private int CountCardUsage(int cardId)
    {
        int count = 0;
        for (int i = 0; i < selectedCards.Length; i++)
        {
            if (selectedCards[i] != null && selectedCards[i].cardId == cardId)
                count++;
        }
        return count;
    }

    private int GetCardQuantity(int cardId, int cardLevel)
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null)
            return 0;

        OwnedCardData ownedCard = PlayerManager.Instance.Data.ownedCards?.Find(c => c.cardId == cardId && c.cardLevel == cardLevel);
        return ownedCard != null ? ownedCard.quantity : 0;
    }

    private void OnShowCardInfo(int cardId, int cardLevel)
    {
        if (GameDataManager.Instance == null)
            return;

        CardDatabase cardDb = GameDataManager.Instance.CardDatabaseObject as CardDatabase;
        if (cardDb == null)
            return;

        CardDatabase.Card card = cardDb.GetCardById(cardId);
        if (card == null)
            return;

        CardDatabase.CardLevel levelData = card.GetLevel(cardLevel);
        // Localize description từ key trước khi format coloring
        string localizedDesc = GetLocalizedText(card.description, card.description ?? "No description");
        string formattedDesc = FormatCardDescription(localizedDesc, levelData, card.type);
        ShowCardInfoPopup(formattedDesc);
    }

    private void ShowCardInfoPopup(string description)
    {
        if (popupCardInfo == null || txtCardInfoDescription == null)
            return;

        txtCardInfoDescription.text = description;
        popupCardInfo.SetActive(true);
    }

    private string FormatCardDescription(string description, CardDatabase.CardLevel levelData, CardDatabase.CardType cardType)
    {
        if (string.IsNullOrEmpty(description))
            return "No description";

        string value = levelData != null ? levelData.value.ToString() : "0";
        string colorHex = GetColorForCardType(cardType);

        // Thay thế $ bằng value với màu
        string formatted = description.Replace("$", $"<color={colorHex}>{value}</color>");
        return formatted;
    }

    private string GetColorForCardType(CardDatabase.CardType cardType)
    {
        return cardType switch
        {
            CardDatabase.CardType.Heal => "#6FFF00",   // Xanh lá
            CardDatabase.CardType.Mana => "#4080FF",   // Xanh dương
            CardDatabase.CardType.Rage => "#FF0000",   // Đỏ
            CardDatabase.CardType.Attack => "#FFFF00", // Vàng
            _ => "#FFFFFF"                             // Trắng (default)
        };
    }

    private static int ResolveEnemyPetId(MapDataAsset map)
    {
        if (map == null)
            return -1;

        if (map.petIdSpawn > 0)
            return map.petIdSpawn;

        return map.rewardPetId >= 0 ? map.rewardPetId : -1;
    }

    private void OnEnable()
    {
        if (popupCardInfo != null)
        {
            if (popupCardInfoRect == null)
                popupCardInfoRect = popupCardInfo.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        if (popupCardInfo == null || !popupCardInfo.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (popupCardInfoRect != null && RectTransformUtility.RectangleContainsScreenPoint(popupCardInfoRect, Input.mousePosition, null))
                return;

            popupCardInfo.SetActive(false);
        }
    }

    private string GetLocalizedText(string id, string fallback)
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
            return LocalizationManager.Instance.GetText(id, fallback);

        return fallback;
    }
}


