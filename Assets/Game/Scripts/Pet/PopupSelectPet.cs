using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupSelectPet : MonoBehaviour
{
    [System.Serializable]
    public class PetOptionData
    {
        public int petId;
        public int petLevel;
        public GameObject previewPrefab;
    }

    [Header("Popup Root")]
    [SerializeField] private RectTransform popupRect;
    [SerializeField] private Button closeButton;
    [SerializeField] private Vector2 shownPosition = Vector2.zero;
    [SerializeField] private Vector2 hiddenPosition = new Vector2(2000f, 0f);
    [SerializeField] private float slideDuration = 0.3f;

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int popupSortingOrder = 200;
    [SerializeField] private int petPreviewSortingOrder = 201;
    [SerializeField] private Canvas popupOverrideCanvas;

    [Header("List")]
    [SerializeField] private Transform contentRoot;
    [SerializeField] private PetItemButton itemPrefab;

    private readonly List<PetItemButton> spawnedItems = new List<PetItemButton>();
    private Action<int> onPetSelected;
    private Coroutine slideCoroutine;
    private int lastSelectedPetId = -1;

    private void Awake()
    {
        if (popupRect == null)
            popupRect = GetComponent<RectTransform>();

        if (popupRect != null)
            popupRect.anchoredPosition = hiddenPosition;

        ApplyPopupSorting();
        BindButtons();
    }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged += RebuildOwnedPets;
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged -= RebuildOwnedPets;
    }

    private void Start()
    {
        // Auto-load owned pets on start
        RebuildOwnedPets();
    }

    private void OnDestroy()
    {
        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);
    }

    private void BindButtons()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(Close);
    }

    public void Open(Action<int> onSelected)
    {
        onPetSelected = onSelected;
        ApplyPopupSorting();
        SlideTo(shownPosition);
    }

    public void Open(IList<PetOptionData> options, Action<int> onSelected)
    {
        onPetSelected = onSelected;
        Rebuild(options);
        SlideTo(shownPosition);
    }

    public void Close()
    {
        SlideTo(hiddenPosition);
    }

    public void RebuildOwnedPets()
    {
        List<PetOptionData> options = BuildOwnedPetOptions();
        Rebuild(options);
    }

    public void Rebuild(IList<PetOptionData> options)
    {
        ClearItems();

        if (contentRoot == null || itemPrefab == null || options == null)
        {
            Debug.LogWarning("[PopupSelectPet.Rebuild] contentRoot, itemPrefab or options is null!");
            return;
        }

        if (options.Count == 0)
        {
            Debug.LogWarning("[PopupSelectPet.Rebuild] No pet options available!");
            return;
        }

        Debug.Log($"[PopupSelectPet.Rebuild] Building {options.Count} pet items.");

        for (int i = 0; i < options.Count; i++)
        {
            PetOptionData option = options[i];
            if (option == null)
            {
                Debug.LogWarning($"[PopupSelectPet.Rebuild] Option at index {i} is null!");
                continue;
            }

            if (option.previewPrefab == null)
            {
                Debug.LogWarning($"[PopupSelectPet.Rebuild] Pet {option.petId} has no preview prefab!");
                continue;
            }

            Debug.Log($"[PopupSelectPet.Rebuild] Creating item for pet {option.petId} (level {option.petLevel})");

            PetItemButton item = Instantiate(itemPrefab, contentRoot);
            item.ConfigurePreviewSorting(sortingLayerName, petPreviewSortingOrder);
            item.Setup(option.petId, option.petLevel, option.previewPrefab, OnSelectPetItem);
            spawnedItems.Add(item);
        }

        Debug.Log($"[PopupSelectPet.Rebuild] Successfully created {spawnedItems.Count} items.");
    }

    private List<PetOptionData> BuildOwnedPetOptions()
    {
        List<PetOptionData> options = new List<PetOptionData>();

        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null || PlayerManager.Instance.Data.ownedPets == null)
        {
            Debug.LogWarning("[PopupSelectPet.BuildOwnedPetOptions] PlayerManager or ownedPets is null!");
            return options;
        }

        List<OwnedPetData> ownedPets = PlayerManager.Instance.Data.ownedPets;
        Debug.Log($"[PopupSelectPet.BuildOwnedPetOptions] Found {ownedPets.Count} owned pets");

        for (int i = 0; i < ownedPets.Count; i++)
        {
            OwnedPetData ownedPet = ownedPets[i];
            if (ownedPet == null)
            {
                Debug.LogWarning($"[PopupSelectPet.BuildOwnedPetOptions] OwnedPet at index {i} is null!");
                continue;
            }

            GameObject petPrefab = ResolveOwnedPetPrefab(ownedPet);
            if (petPrefab == null)
            {
                Debug.LogWarning($"[PopupSelectPet.BuildOwnedPetOptions] Could not resolve prefab for pet {ownedPet.petId} ({ownedPet.petName})!");
                continue;
            }

            PetOptionData option = new PetOptionData();
            option.petId = ownedPet.petId;
            option.petLevel = Mathf.Max(1, ownedPet.petLevel);
            option.previewPrefab = petPrefab;
            options.Add(option);

            Debug.Log($"[PopupSelectPet.BuildOwnedPetOptions] Added pet option: ID={option.petId}, Level={option.petLevel}, Prefab={petPrefab.name}");
        }

        Debug.Log($"[PopupSelectPet.BuildOwnedPetOptions] Total options built: {options.Count}");
        return options;
    }

    private void OnSelectPetItem(int petId)
    {
        lastSelectedPetId = petId;
        if (onPetSelected != null)
            onPetSelected.Invoke(petId);

        Close();
    }

    private void SlideTo(Vector2 targetPosition)
    {
        if (popupRect == null)
            return;

        if (slideCoroutine != null)
            StopCoroutine(slideCoroutine);

        slideCoroutine = StartCoroutine(SlideRoutine(targetPosition));
    }

    private IEnumerator SlideRoutine(Vector2 targetPosition)
    {
        Vector2 start = popupRect.anchoredPosition;
        float duration = Mathf.Max(0.01f, slideDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            p = p * p * (3f - 2f * p);
            popupRect.anchoredPosition = Vector2.Lerp(start, targetPosition, p);
            yield return null;
        }

        popupRect.anchoredPosition = targetPosition;
        slideCoroutine = null;
    }

    private void ClearItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            PetItemButton item = spawnedItems[i];
            if (item != null)
                Destroy(item.gameObject);
        }

        spawnedItems.Clear();
    }

    private GameObject ResolveOwnedPetPrefab(OwnedPetData ownedPet)
    {
        if (ownedPet == null)
            return null;

        GameDataManager data = GameDataManager.Instance;
        if (data == null)
            return null;

        return data.GetPetPrefabForLevel(ownedPet.petId, ownedPet.petLevel, ownedPet.petName);
    }

    public void RefreshSelectedPetItem(int petId)
    {
        if (petId < 0)
            return;

        OwnedPetData ownedPet = PlayerManager.Instance != null ? PlayerManager.Instance.GetOwnedPet(petId) : null;
        if (ownedPet == null)
        {
            Debug.LogWarning($"[PopupSelectPet.RefreshSelectedPetItem] Pet {petId} not found");
            return;
        }

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            PetItemButton item = spawnedItems[i];
            if (item == null)
                continue;

            if (item.GetOwnedPetId() == petId)
            {
                GameObject prefab = ResolveOwnedPetPrefab(ownedPet);
                item.RefreshLevel(Mathf.Max(1, ownedPet.petLevel));
                item.RefreshPreview(prefab);
                Debug.Log($"[PopupSelectPet.RefreshSelectedPetItem] Refreshed pet {petId} to level {ownedPet.petLevel}");
                return;
            }
        }

        Debug.LogWarning($"[PopupSelectPet.RefreshSelectedPetItem] Pet item {petId} not found in spawned items");
    }

    private void ApplyPopupSorting()
    {
        if (popupOverrideCanvas == null)
            popupOverrideCanvas = GetComponent<Canvas>();

        if (popupOverrideCanvas == null)
            popupOverrideCanvas = gameObject.AddComponent<Canvas>();

        popupOverrideCanvas.overrideSorting = true;
        if (!string.IsNullOrWhiteSpace(sortingLayerName))
            popupOverrideCanvas.sortingLayerName = sortingLayerName;
        popupOverrideCanvas.sortingOrder = popupSortingOrder;

        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        Canvas[] canvases = GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;

            canvas.overrideSorting = true;
            if (!string.IsNullOrWhiteSpace(sortingLayerName))
                canvas.sortingLayerName = sortingLayerName;

            canvas.sortingOrder = popupSortingOrder;
        }
    }
}
