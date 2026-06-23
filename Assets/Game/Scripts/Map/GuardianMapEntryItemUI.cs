using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;

public class GuardianMapEntryItemUI : MonoBehaviour
{
    [Header("Map Binding")]
    [SerializeField] private string mapId;
    [SerializeField] private Button mapButton;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtMapName;
    [SerializeField] private TextMeshProUGUI txtWinsProgress;

    [Header("Guardian Preview")]
    [SerializeField] private Transform guardianSpawnRoot;
    [SerializeField] private Vector3 guardianPreviewScale = Vector3.one;
    [SerializeField] private string previewSortingLayerName = "UI";
    [SerializeField] private int previewSortingOrder = 3;

    private MapDataAsset currentMapData;
    private GameObject spawnedGuardianPreview;

    public event Action<MapDataAsset> OnMapClicked;

    private void Awake()
    {
        BindButton();
    }

    private void OnEnable()
    {
        BindButton();
        Refresh();
    }

    private void OnDestroy()
    {
        if (mapButton != null)
            mapButton.onClick.RemoveListener(HandleClick);

        ClearGuardianPreview();
    }

    public void Setup(string inMapId)
    {
        mapId = inMapId;
        Refresh();
    }

    public void Refresh()
    {
        if (string.IsNullOrWhiteSpace(mapId) || GameDataManager.Instance == null)
            return;

        currentMapData = GameDataManager.Instance.GetMapData(mapId);
        if (currentMapData == null)
            return;

        if (txtMapName != null)
            txtMapName.text = GetLocalizedText(currentMapData.mapName, currentMapData.mapName);

        if (txtWinsProgress != null)
        {
            int wins = PlayerManager.Instance != null ? PlayerManager.Instance.GetMapWinCount(currentMapData.mapId) : 0;
            txtWinsProgress.text = MapLevelRequirementUIHelper.BuildWinsProgressText(currentMapData, wins);
        }

        RefreshGuardianPreview(ResolveRewardGuardianId(currentMapData));
    }

    private void BindButton()
    {
        if (mapButton == null)
            mapButton = GetComponent<Button>();

        if (mapButton == null)
            mapButton = GetComponentInChildren<Button>(true);

        if (mapButton == null)
            return;

        mapButton.onClick.RemoveListener(HandleClick);
        mapButton.onClick.AddListener(HandleClick);

        if (AudioManager.Instance != null)
            AudioManager.Instance.RegisterButtonClick(mapButton);
    }
    private void HandleClick()
    {
        if (currentMapData == null)
            Refresh();

        if (currentMapData == null)
            return;

        if (OnMapClicked != null)
            OnMapClicked.Invoke(currentMapData);
    }
    private void RefreshGuardianPreview(int guardianId)
    {
        ClearGuardianPreview();

        if (guardianId < 0 || guardianSpawnRoot == null || GameDataManager.Instance == null || GameDataManager.Instance.GuardianDatabase == null)
            return;

        GuardianDataAsset guardian = GameDataManager.Instance.GuardianDatabase.GetGuardianById(guardianId);
        if (guardian == null || guardian.guardianPrefab == null)
            return;

        spawnedGuardianPreview = Instantiate(guardian.guardianPrefab, guardianSpawnRoot);
        spawnedGuardianPreview.transform.localPosition = Vector3.zero;
        spawnedGuardianPreview.transform.localRotation = Quaternion.identity;
        spawnedGuardianPreview.transform.localScale = guardianPreviewScale;

        ForceIdle(spawnedGuardianPreview);
        ApplyShortLayer(spawnedGuardianPreview);
    }

    private static int ResolveRewardGuardianId(MapDataAsset map)
    {
        if (map == null)
            return -1;

        return map.rewardGuardiantId >= 0 ? map.rewardGuardiantId : map.idGuadiant;
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
        shortLayer.SortingOrder = previewSortingOrder;
        shortLayer.Apply();
    }

    private void ClearGuardianPreview()
    {
        if (spawnedGuardianPreview != null)
        {
            Destroy(spawnedGuardianPreview);
            spawnedGuardianPreview = null;
        }
    }

    private string GetLocalizedText(string id, string fallback)
    {
        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
            return LocalizationManager.Instance.GetText(id, fallback);

        return fallback;
    }
}






