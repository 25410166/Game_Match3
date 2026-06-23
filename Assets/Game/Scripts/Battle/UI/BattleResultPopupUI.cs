using System;
using System.Collections.Generic;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultPopupUI : MonoBehaviour
{
    [Serializable]
    public class GemRewardViewData
    {
        public string displayName;
        public int amount;
        public Sprite gemSprite; // Icon của gem
    }

    [Serializable]
    public class WinResultViewData
    {
        public List<GemRewardViewData> gemRewards = new List<GemRewardViewData>();
        public int gold;
        public int exp;
        public int diamond;
        public bool hasPetReward;
        public int petId;
        public int petLevel;
        public string petName;
        public bool hasGuardianReward;
        public int guardianId;
        public int guardianLevel;
        public string guardianName;
    }

    [Header("Roots")]
    [SerializeField] private GameObject failRoot;
    [SerializeField] private GameObject winRoot;

    [Header("Fail")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button failHomeButton;

    [Header("Win")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI diamondText;
    [SerializeField] private Transform gemListRoot;
    [SerializeField] private WinRewardItemUI gemItemPrefab;
    [SerializeField] private Button continueButton;

    [Header("Pet Reward")]
    [SerializeField] private GameObject petRewardRoot;
    [SerializeField] private Transform petSpawnRoot;
    [SerializeField] private TextMeshProUGUI petNameText;
    [SerializeField] private Vector3 petScale = Vector3.one;

    private readonly List<WinRewardItemUI> spawnedGemItems = new List<WinRewardItemUI>();
    private GameObject spawnedPet;
    private GameObject spawnedGuardian;

    private void Awake()
    {
        HideAll();
    }

    public void HideAll()
    {
        if (failRoot != null) failRoot.SetActive(false);
        if (winRoot != null) winRoot.SetActive(false);
        ClearGemItems();
        ClearPetPreview();
        ClearGuardianPreview();
    }

    public void ShowFail(Action onRetry, Action onHome)
    {
        HideAll();

        if (failRoot != null)
            failRoot.SetActive(true);

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => onRetry?.Invoke());
        }

        if (failHomeButton != null)
        {
            failHomeButton.onClick.RemoveAllListeners();
            failHomeButton.onClick.AddListener(() => onHome?.Invoke());
        }
    }

    public void ShowWin(WinResultViewData data, Action onContinue)
    {
        HideAll();

        if (winRoot != null)
            winRoot.SetActive(true);

        if (goldText != null)
            goldText.text = Mathf.Max(0, data != null ? data.gold : 0).ToString();

        if (expText != null)
            expText.text = Mathf.Max(0, data != null ? data.exp : 0).ToString();

        if (diamondText != null)
            diamondText.text = Mathf.Max(0, data != null ? data.diamond : 0).ToString();

        Debug.Log($"[BattleResultPopupUI] RefreshWinResult: data.gemRewards count = {data?.gemRewards?.Count ?? 0}");
        if (data?.gemRewards != null)
        {
            for (int i = 0; i < data.gemRewards.Count; i++)
            {
                Debug.Log($"[BattleResultPopupUI] GemReward {i}: name='{data.gemRewards[i].displayName}', amount={data.gemRewards[i].amount}, sprite={data.gemRewards[i].gemSprite?.name ?? "NULL"}");
            }
        }

        BuildGemItems(data != null ? data.gemRewards : null);
        RefreshRewardPreview(data);

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() => onContinue?.Invoke());
        }
    }

    private void BuildGemItems(List<GemRewardViewData> gems)
    {
        ClearGemItems();

        if (gemListRoot == null || gemItemPrefab == null || gems == null)
        {
            Debug.LogWarning($"[BattleResultPopupUI] BuildGemItems: gemListRoot={gemListRoot}, gemItemPrefab={gemItemPrefab}, gems={gems}");
            return;
        }

        Debug.Log($"[BattleResultPopupUI] BuildGemItems: Processing {gems.Count} gem rewards");

        for (int i = 0; i < gems.Count; i++)
        {
            GemRewardViewData item = gems[i];
            if (item == null || item.amount <= 0)
            {
                Debug.LogWarning($"[BattleResultPopupUI] Gem {i}: item is null or amount <= 0");
                continue;
            }

            Debug.Log($"[BattleResultPopupUI] Gem {i}: displayName='{item.displayName}', amount={item.amount}, sprite={item.gemSprite?.name ?? "NULL"}");

            WinRewardItemUI row = Instantiate(gemItemPrefab, gemListRoot);
            row.Setup(item.displayName, item.amount, item.gemSprite);
            spawnedGemItems.Add(row);
        }

        Debug.Log($"[BattleResultPopupUI] BuildGemItems completed: {spawnedGemItems.Count} gem UI items created");
    }

    private void RefreshRewardPreview(WinResultViewData data)
    {
        bool showGuardian = data != null && data.hasGuardianReward && data.guardianId >= 0;
        bool showPet = !showGuardian && data != null && data.hasPetReward && data.petId >= 0;

        if (petRewardRoot != null)
            petRewardRoot.SetActive(showGuardian || showPet);

        ClearPetPreview();
        ClearGuardianPreview();
        if (!showGuardian && !showPet)
            return;

        if (GameDataManager.Instance == null || petSpawnRoot == null)
            return;

        if (showGuardian)
        {
            if (petNameText != null)
                petNameText.text = string.IsNullOrWhiteSpace(data.guardianName) ? ("Guardian ID " + data.guardianId) : data.guardianName;

            if (GameDataManager.Instance.GuardianDatabase == null)
                return;

            GuardianDataAsset guardianData = GameDataManager.Instance.GuardianDatabase.GetGuardianById(data.guardianId);
            if (guardianData == null || guardianData.guardianPrefab == null)
                return;

            spawnedGuardian = Instantiate(guardianData.guardianPrefab, petSpawnRoot);
            spawnedGuardian.transform.localPosition = Vector3.zero;
            spawnedGuardian.transform.localRotation = Quaternion.identity;
            spawnedGuardian.transform.localScale = petScale;

            SkeletonAnimation guardianSkel = spawnedGuardian.GetComponentInChildren<SkeletonAnimation>(true);
            if (guardianSkel != null && guardianSkel.state != null)
                guardianSkel.state.SetAnimation(0, "Idle", true);
            return;
        }

        if (petNameText != null)
            petNameText.text = string.IsNullOrWhiteSpace(data.petName) ? ("Pet ID " + data.petId) : data.petName;

        GameObject prefab = GameDataManager.Instance.GetPetPrefab(data.petId, data.petName);
        if (prefab == null)
            return;

        spawnedPet = Instantiate(prefab, petSpawnRoot);
        spawnedPet.transform.localPosition = Vector3.zero;
        spawnedPet.transform.localRotation = Quaternion.identity;
        spawnedPet.transform.localScale = petScale;

        SkeletonAnimation skel = spawnedPet.GetComponentInChildren<SkeletonAnimation>(true);
        if (skel != null && skel.state != null)
            skel.state.SetAnimation(0, "Idle", true);
    }

    private void ClearGemItems()
    {
        for (int i = 0; i < spawnedGemItems.Count; i++)
        {
            if (spawnedGemItems[i] != null)
                Destroy(spawnedGemItems[i].gameObject);
        }

        spawnedGemItems.Clear();
    }

    private void ClearPetPreview()
    {
        if (spawnedPet != null)
        {
            Destroy(spawnedPet);
            spawnedPet = null;
        }
    }

    private void ClearGuardianPreview()
    {
        if (spawnedGuardian != null)
        {
            Destroy(spawnedGuardian);
            spawnedGuardian = null;
        }
    }
}
