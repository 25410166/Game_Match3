using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ShortLayer : MonoBehaviour
{
    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "UI";
    [SerializeField] private int sortingOrder = 11;
    [SerializeField] private bool includeInactive = true;
    [SerializeField] private bool applyDeferredOnEnable = true;
    [SerializeField] private bool applyToChildCanvases = true;
    [SerializeField] private bool overrideChildCanvasSorting = true;

    [Header("Canvas Self Handling")]
    [SerializeField] private bool ensureOwnCanvasWhenInsideCanvas = true;
    [SerializeField] private bool forceOwnCanvasOverrideSorting = true;

    [Header("Optional Unity Layer")]
    [SerializeField] private bool setUnityLayer;
    [SerializeField] private string unityLayerName = "Default";

    private Canvas ownCanvas;

    public string SortingLayerName
    {
        get { return sortingLayerName; }
        set { sortingLayerName = value; }
    }

    public int SortingOrder
    {
        get { return sortingOrder; }
        set { sortingOrder = value; }
    }

    public void Apply()
    {
        if (setUnityLayer)
        {
            int layer = LayerMask.NameToLayer(unityLayerName);
            if (layer >= 0)
                SetLayerRecursively(gameObject, layer);
        }

        int sortingLayerId = ResolveSortingLayerId(sortingLayerName);

        EnsureOwnCanvasIfNeeded(sortingLayerId);
        ApplySortingGroups(sortingLayerId);
        ApplyRenderers(sortingLayerId);
        ApplyCanvases(sortingLayerId);
        ApplyTextMeshSubObjects(sortingLayerId);
    }

    private void EnsureOwnCanvasIfNeeded(int sortingLayerId)
    {
        if (!ensureOwnCanvasWhenInsideCanvas)
            return;

        bool insideCanvas = GetComponentInParent<Canvas>(true) != null;
        bool shouldHaveOwnCanvas = insideCanvas && ShouldCreateOwnCanvas();
        if (!shouldHaveOwnCanvas)
            return;

        if (ownCanvas == null)
            ownCanvas = GetComponent<Canvas>();

        if (ownCanvas == null)
            ownCanvas = gameObject.AddComponent<Canvas>();

        ownCanvas.overrideSorting = forceOwnCanvasOverrideSorting;

        if (sortingLayerId >= 0)
            ownCanvas.sortingLayerID = sortingLayerId;

        ownCanvas.sortingOrder = sortingOrder;

        if (GetComponent<GraphicRaycaster>() == null && HasGraphicOnSelf())
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private bool ShouldCreateOwnCanvas()
    {
        if (GetComponent<RectTransform>() == null)
            return false;

        if (GetComponent<Graphic>() != null)
            return true;

        if (GetComponent<TMP_Text>() != null)
            return true;

        return GetComponent<Canvas>() != null;
    }

    private bool HasGraphicOnSelf()
    {
        return GetComponent<Graphic>() != null || GetComponent<TMP_Text>() != null;
    }

    private void ApplySortingGroups(int sortingLayerId)
    {
        SortingGroup[] groups = GetComponentsInChildren<SortingGroup>(includeInactive);
        for (int i = 0; i < groups.Length; i++)
        {
            SortingGroup group = groups[i];
            if (group == null)
                continue;

            if (sortingLayerId >= 0)
                group.sortingLayerID = sortingLayerId;

            group.sortingOrder = sortingOrder;
        }
    }

    private void ApplyRenderers(int sortingLayerId)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null)
                continue;

            if (sortingLayerId >= 0)
                targetRenderer.sortingLayerID = sortingLayerId;

            targetRenderer.sortingOrder = sortingOrder;
        }
    }

    private void ApplyCanvases(int sortingLayerId)
    {
        if (!applyToChildCanvases)
            return;

        Canvas[] canvases = GetComponentsInChildren<Canvas>(includeInactive);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
                continue;

            bool isOwnCanvas = canvas.transform == transform;
            if (isOwnCanvas)
            {
                if (forceOwnCanvasOverrideSorting)
                    canvas.overrideSorting = true;
            }
            else if (overrideChildCanvasSorting)
            {
                canvas.overrideSorting = true;
            }

            if (sortingLayerId >= 0)
                canvas.sortingLayerID = sortingLayerId;

            canvas.sortingOrder = sortingOrder;
        }
    }

    private void ApplyTextMeshSubObjects(int sortingLayerId)
    {
        TMP_SubMesh[] subMeshes = GetComponentsInChildren<TMP_SubMesh>(includeInactive);
        for (int i = 0; i < subMeshes.Length; i++)
        {
            TMP_SubMesh subMesh = subMeshes[i];
            if (subMesh == null)
                continue;

            Renderer subRenderer = subMesh.GetComponent<Renderer>();
            if (subRenderer == null)
                continue;

            if (sortingLayerId >= 0)
                subRenderer.sortingLayerID = sortingLayerId;

            subRenderer.sortingOrder = sortingOrder;
        }
    }

    private static int ResolveSortingLayerId(string layerName)
    {
        if (string.IsNullOrWhiteSpace(layerName))
            return -1;

        int sortingLayerId = SortingLayer.NameToID(layerName);
        return sortingLayerId != 0 || layerName == "Default" ? sortingLayerId : -1;
    }

    private void OnEnable()
    {
        Apply();

        if (applyDeferredOnEnable)
            StartCoroutine(ApplyDeferredRoutine());
    }

    private void OnTransformChildrenChanged()
    {
        Apply();
    }

    private IEnumerator ApplyDeferredRoutine()
    {
        yield return null;
        Apply();
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        for (int i = 0; i < obj.transform.childCount; i++)
            SetLayerRecursively(obj.transform.GetChild(i).gameObject, layer);
    }
}
