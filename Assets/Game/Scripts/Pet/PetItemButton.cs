using UnityEngine;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class PetItemButton : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Transform previewRoot;
    [SerializeField] private TextMeshProUGUI txtPetLevel;

    [Header("Preview")]
    [SerializeField] private Vector3 previewLocalScale = Vector3.one;
    [SerializeField] private string idleAnimationName = "Idle";
    [SerializeField] private string previewSortingLayerName = "UI";
    [SerializeField] private int previewSortingOrder = 21;

    private Action<int> onSelectPet;
    private int ownedPetId = -1;
    private GameObject spawnedPreview;
    private int currentLevel = 1;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (AudioManager.Instance != null)
            AudioManager.Instance.RegisterButtonClick(button);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClickButton);

        ClearPreview();
    }

    public void Setup(OwnedPetData ownedPet, GameObject prefab, Action<int> onSelected)
    {
        onSelectPet = onSelected;
        ownedPetId = ownedPet != null ? ownedPet.petId : -1;

        if (button != null)
        {
            button.onClick.RemoveListener(OnClickButton);
            button.onClick.AddListener(OnClickButton);
            if (AudioManager.Instance != null)
                AudioManager.Instance.RegisterButtonClick(button);
        }

        if (txtPetLevel != null)
            txtPetLevel.text = ownedPet != null ? $"Lv. {Mathf.Max(1, ownedPet.petLevel)}" : string.Empty;

        BuildPreview(prefab);
    }

    public void Setup(int petId, int petLevel, GameObject prefab, Action<int> onSelected)
    {
        onSelectPet = onSelected;
        ownedPetId = petId;
        currentLevel = Mathf.Max(1, petLevel);

        if (button != null)
        {
            button.onClick.RemoveListener(OnClickButton);
            button.onClick.AddListener(OnClickButton);
            if (AudioManager.Instance != null)
                AudioManager.Instance.RegisterButtonClick(button);
        }

        if (txtPetLevel != null)
            txtPetLevel.text = $"Lv. {currentLevel}";

        BuildPreview(prefab);
    }

    private void OnClickButton()
    {
        if (ownedPetId >= 0 && onSelectPet != null)
            onSelectPet.Invoke(ownedPetId);
    }

    private void BuildPreview(GameObject previewPrefab)
    {
        ClearPreview();

        if (previewPrefab == null)
            return;

        Transform root = previewRoot != null ? previewRoot : transform;
        spawnedPreview = Instantiate(previewPrefab, root);
        spawnedPreview.transform.localPosition = Vector3.zero;
        spawnedPreview.transform.localRotation = Quaternion.identity;
        spawnedPreview.transform.localScale = previewLocalScale;

        ForceIdle(spawnedPreview);
        ApplyShortLayer(spawnedPreview);
    }

    private void ClearPreview()
    {
        if (spawnedPreview != null)
        {
            Destroy(spawnedPreview);
            spawnedPreview = null;
        }
    }

    private void ForceIdle(GameObject go)
    {
        if (go == null)
            return;

        SkeletonAnimation skeletonAnimation = go.GetComponentInChildren<SkeletonAnimation>(true);
        if (skeletonAnimation != null && skeletonAnimation.state != null)
            skeletonAnimation.state.SetAnimation(0, idleAnimationName, true);
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

     public void ConfigurePreviewSorting(string sortingLayerName, int sortingOrder)
     {
         if (!string.IsNullOrWhiteSpace(sortingLayerName))
             previewSortingLayerName = sortingLayerName;

         previewSortingOrder = sortingOrder;

         if (spawnedPreview != null)
             ApplyShortLayer(spawnedPreview);
     }

         public int GetOwnedPetId() => ownedPetId;

         public void RefreshLevel(int newLevel)
         {
             currentLevel = Mathf.Max(1, newLevel);
             if (txtPetLevel != null)
                 txtPetLevel.text = $"Lv. {currentLevel}";

             Debug.Log($"[PetItemButton.RefreshLevel] Pet {ownedPetId} level updated to {currentLevel}");
         }

         public void RefreshPreview(GameObject newPrefab)
         {
             BuildPreview(newPrefab);
             Debug.Log($"[PetItemButton.RefreshPreview] Pet {ownedPetId} preview refreshed");
         }
    }
