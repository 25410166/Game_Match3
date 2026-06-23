using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TutorialFocusTarget : MonoBehaviour
{
    [SerializeField] private int boostedSortingOrder = 30;
    [SerializeField] private string fallbackSortingLayerName = "UI";

    private Canvas canvas;
    private GraphicRaycaster raycaster;
    private int originalSiblingIndex;
    private bool isApplied;

    private bool hadCanvas;
    private bool originalCanvasOverrideSorting;
    private int originalCanvasSortingOrder;
    private string originalCanvasSortingLayerName;

    private bool hadRaycaster;

    public RectTransform RectTransform => transform as RectTransform;

    public void ApplyFocus()
    {
        if (isApplied)
            return;

        isApplied = true;
        originalSiblingIndex = transform.GetSiblingIndex();

        canvas = GetComponent<Canvas>();
        hadCanvas = canvas != null;
        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        originalCanvasOverrideSorting = canvas.overrideSorting;
        originalCanvasSortingOrder = canvas.sortingOrder;
        originalCanvasSortingLayerName = canvas.sortingLayerName;

        Canvas parentCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>(true) : null;
        string targetSortingLayer = parentCanvas != null && !string.IsNullOrWhiteSpace(parentCanvas.sortingLayerName)
            ? parentCanvas.sortingLayerName
            : fallbackSortingLayerName;

        canvas.overrideSorting = true;
        canvas.sortingLayerName = targetSortingLayer;
        canvas.sortingOrder = boostedSortingOrder;

        raycaster = GetComponent<GraphicRaycaster>();
        hadRaycaster = raycaster != null;
        if (raycaster == null)
            raycaster = gameObject.AddComponent<GraphicRaycaster>();

        transform.SetAsLastSibling();
    }

    public void ClearFocus()
    {
        if (!isApplied)
            return;

        isApplied = false;

        if (canvas != null)
        {
            canvas.overrideSorting = originalCanvasOverrideSorting;
            canvas.sortingOrder = originalCanvasSortingOrder;
            if (!string.IsNullOrWhiteSpace(originalCanvasSortingLayerName))
                canvas.sortingLayerName = originalCanvasSortingLayerName;
        }

        if (transform.parent != null)
            transform.SetSiblingIndex(Mathf.Clamp(originalSiblingIndex, 0, transform.parent.childCount - 1));

        if (!hadRaycaster && raycaster != null)
            Destroy(raycaster);

        if (!hadCanvas && canvas != null)
            Destroy(canvas);

        raycaster = null;
        canvas = null;
    }
}

