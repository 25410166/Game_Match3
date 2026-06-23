using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;

public class MapEntryItemUI : MonoBehaviour
{
    [Header("Map Binding")]
    [SerializeField] private string mapId;
    [SerializeField] private Button mapButton;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtMapName;
    [SerializeField] private TextMeshProUGUI txtWinsProgress;

    [Header("Pet Preview")]
    [SerializeField] private Transform petSpawnRoot;
    [SerializeField] private Vector3 petPreviewScale = Vector3.one;
    [SerializeField] private string previewSortingLayerName = "UI";
    [SerializeField] private int previewSortingOrder = 3;
    [SerializeField] private bool forceMapPreviewSortingOrder = true;

    private GameObject spawnedPetPreview;
    private MapDataAsset currentMapData;
    private bool isPreviewVisible = true;
    private bool isClaimingReward = false;  // Flag to prevent infinite loop during reward claim

    public event Action<MapDataAsset> OnMapClicked;

    public Button MapButton => mapButton;

    private void Awake()
    {
        if (mapButton == null)
            mapButton = GetComponent<Button>();

        if (mapButton == null)
            mapButton = GetComponentInChildren<Button>(true);

        if (mapButton != null)
        {
            mapButton.onClick.RemoveListener(HandleClick);
            mapButton.onClick.AddListener(HandleClick);

            if (AudioManager.Instance != null)
                AudioManager.Instance.RegisterButtonClick(mapButton);
        }
        else
        {
            Debug.LogWarning($"[MapEntryItemUI] Missing Button reference on {name}");
        }
    }

    private void OnEnable()
    {
        if (mapButton != null)
        {
            mapButton.onClick.RemoveListener(HandleClick);
            mapButton.onClick.AddListener(HandleClick);

            if (AudioManager.Instance != null)
                AudioManager.Instance.RegisterButtonClick(mapButton);
        }
    }

    private void OnDestroy()
    {
        if (mapButton != null)
            mapButton.onClick.RemoveListener(HandleClick);

        ClearPetPreview();
    }

    public void Refresh()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mapId) || GameDataManager.Instance == null)
                return;

            currentMapData = GameDataManager.Instance.GetMapData(mapId);
            if (currentMapData == null)
                return;

            if (txtMapName != null)
                txtMapName.text = GetLocalizedText(currentMapData.mapName, currentMapData.mapName);

            int wins = PlayerManager.Instance != null ? PlayerManager.Instance.GetMapWinCount(currentMapData.mapId) : 0;
            if (txtWinsProgress != null)
                txtWinsProgress.text = MapLevelRequirementUIHelper.BuildWinsProgressText(currentMapData, wins);

            // Skip pet reward if already claiming (prevents infinite loop)
            if (!isClaimingReward)
            {
                TryGrantPetReward(wins);
            }

            RefreshPetPreview(currentMapData.petIdSpawn);
        }
        catch (System.StackOverflowException ex)
        {
            Debug.LogError($"[MapEntryItemUI] StackOverflow in Refresh: {ex.Message}");
            // Do not retry - this is a fatal error
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MapEntryItemUI] Error in Refresh: {ex.Message}\n{ex.StackTrace}");
        }
    }
    private void HandleClick()
    {
        if (currentMapData == null)
            Refresh();

        if (currentMapData == null)
        {
            Debug.LogWarning($"[MapEntryItemUI] Click ignored because map data is null. mapId={mapId}, object={name}");
            return;
        }

        if (OnMapClicked != null)
            OnMapClicked.Invoke(currentMapData);
    }
    private void TryGrantPetReward(int wins)
    {
        if (currentMapData == null || PlayerManager.Instance == null)
            return;

        if (wins < currentMapData.reqWinsPet)
            return;

        if (PlayerManager.Instance.HasClaimedMapPetReward(currentMapData.mapId))
            return;

        // Prevent infinite loop: if already claiming, don't claim again
        if (isClaimingReward)
            return;

        isClaimingReward = true;

        try
        {
            int rewardPetId = currentMapData.rewardPetId >= 0 ? currentMapData.rewardPetId : currentMapData.petIdSpawn;
            if (rewardPetId < 0)
                return;

            string petName = rewardPetId.ToString();
            
            // Safe null check for GameDataManager
            if (GameDataManager.Instance != null)
            {
                try
                {
                    if (GameDataManager.Instance.TryGetPetStatSnapshot(rewardPetId, 1, out GameDataManager.PetStatSnapshot snapshot) &&
                        !string.IsNullOrWhiteSpace(snapshot.petName))
                    {
                        petName = snapshot.petName;
                    }
                }
                catch (System.StackOverflowException)
                {
                    // Skip getting pet name if overflow occurs
                    Debug.LogWarning($"[MapEntryItemUI] StackOverflow getting pet name, using ID instead");
                }
            }

            bool rewardClaimed = PlayerManager.Instance.TryClaimMapPetReward(currentMapData.mapId, rewardPetId, petName, 1);
            if (rewardClaimed)
                PlayerManager.Instance.SaveData();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MapEntryItemUI] Error in TryGrantPetReward: {ex.Message}");
        }
        finally
        {
            isClaimingReward = false;  // Always reset flag
        }
    }

    private void RefreshPetPreview(int petId)
    {
        ClearPetPreview();

        if (petSpawnRoot == null || GameDataManager.Instance == null)
            return;

        GameObject prefab = GameDataManager.Instance.GetPetPrefab(petId, string.Empty);
        if (prefab == null)
            return;

        spawnedPetPreview = Instantiate(prefab, petSpawnRoot);
        spawnedPetPreview.transform.localPosition = Vector3.zero;
        spawnedPetPreview.transform.localRotation = Quaternion.identity;
        spawnedPetPreview.transform.localScale = petPreviewScale;
        spawnedPetPreview.SetActive(isPreviewVisible);

        ForceIdle(spawnedPetPreview);
        ApplyShortLayer(spawnedPetPreview);
    }

    public void SetPreviewVisible(bool visible)
    {
        isPreviewVisible = visible;
        if (spawnedPetPreview != null)
            spawnedPetPreview.SetActive(visible);
    }

    private static void ForceIdle(GameObject go)
    {
        if (go == null)
            return;

        SkeletonAnimation skeletonAnimation = go.GetComponentInChildren<SkeletonAnimation>(true);
        if (skeletonAnimation != null && skeletonAnimation.state != null)
            skeletonAnimation.state.SetAnimation(0, "Idle", true);
    }

    private void ApplyShortLayer(GameObject go)
    {
        if (go == null)
            return;

        ShortLayer shortLayer = go.GetComponent<ShortLayer>();
        if (shortLayer == null)
            shortLayer = go.AddComponent<ShortLayer>();

        shortLayer.SortingLayerName = previewSortingLayerName;
        shortLayer.SortingOrder = forceMapPreviewSortingOrder ? 3 : previewSortingOrder;
        shortLayer.Apply();
    }

    private void ClearPetPreview()
    {
        if (spawnedPetPreview != null)
        {
            Destroy(spawnedPetPreview);
            spawnedPetPreview = null;
        }
    }

    private string GetLocalizedText(string id, string fallback)
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
            return LocalizationManager.Instance.GetText(id, fallback);

        return fallback;
    }
}







