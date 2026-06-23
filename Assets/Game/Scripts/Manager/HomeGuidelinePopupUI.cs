using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HomeGuidelinePopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float pageDuration = 0.2f;
    [SerializeField] private float pageOffsetX = 40f;

    [Header("Pages")]
    [SerializeField] private List<GameObject> pageObjects = new List<GameObject>();

    private readonly List<CanvasGroup> pageCanvasGroups = new List<CanvasGroup>();
    private Tween popupTween;
    private Tween pageTween;
    private int currentPageIndex;
    private bool isVisible;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (popupCanvasGroup == null && popupRoot != null)
            popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();

        if (popupCanvasGroup == null && popupRoot != null)
            popupCanvasGroup = popupRoot.AddComponent<CanvasGroup>();

        CachePageCanvasGroups();
        BindButtons();
        SetPopupVisible(false, true);
        ShowPageImmediate(0);
    }

    private void OnDestroy()
    {
        popupTween?.Kill();
        pageTween?.Kill();
    }

    private void BindButtons()
    {
        if (openButton != null)
        {
            openButton.onClick.RemoveListener(OpenPopup);
            openButton.onClick.AddListener(OpenPopup);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePopup);
            closeButton.onClick.AddListener(ClosePopup);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(ShowNextPage);
            nextButton.onClick.AddListener(ShowNextPage);
        }

        if (previousButton != null)
        {
            previousButton.onClick.RemoveListener(ShowPreviousPage);
            previousButton.onClick.AddListener(ShowPreviousPage);
        }
    }

    private void CachePageCanvasGroups()
    {
        pageCanvasGroups.Clear();
        for (int i = 0; i < pageObjects.Count; i++)
        {
            GameObject page = pageObjects[i];
            if (page == null)
            {
                pageCanvasGroups.Add(null);
                continue;
            }

            CanvasGroup canvasGroup = page.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = page.AddComponent<CanvasGroup>();

            pageCanvasGroups.Add(canvasGroup);
        }
    }

    public void OpenPopup()
    {
        SetPopupVisible(true, false);
        ShowPageImmediate(Mathf.Clamp(currentPageIndex, 0, Mathf.Max(0, pageObjects.Count - 1)));
    }

    public void ClosePopup()
    {
        SetPopupVisible(false, false);
    }

    public void ShowNextPage()
    {
        if (currentPageIndex >= pageObjects.Count - 1)
            return;

        ShowPageAnimated(currentPageIndex + 1, true);
    }

    public void ShowPreviousPage()
    {
        if (currentPageIndex <= 0)
            return;

        ShowPageAnimated(currentPageIndex - 1, false);
    }

    private void ShowPageImmediate(int pageIndex)
    {
        currentPageIndex = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, pageObjects.Count - 1));

        for (int i = 0; i < pageObjects.Count; i++)
        {
            GameObject page = pageObjects[i];
            CanvasGroup canvasGroup = i < pageCanvasGroups.Count ? pageCanvasGroups[i] : null;
            bool active = i == currentPageIndex;

            if (page != null)
                page.SetActive(active);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = active ? 1f : 0f;
                canvasGroup.blocksRaycasts = active;
                canvasGroup.interactable = active;
            }

            RectTransform rectTransform = page != null ? page.GetComponent<RectTransform>() : null;
            if (rectTransform != null)
                rectTransform.anchoredPosition = Vector2.zero;
        }

        RefreshNavigationButtons();
    }

    private void ShowPageAnimated(int nextPageIndex, bool forward)
    {
        if (nextPageIndex < 0 || nextPageIndex >= pageObjects.Count || nextPageIndex == currentPageIndex)
            return;

        GameObject currentPage = currentPageIndex >= 0 && currentPageIndex < pageObjects.Count ? pageObjects[currentPageIndex] : null;
        GameObject nextPage = pageObjects[nextPageIndex];
        CanvasGroup currentCanvas = currentPageIndex >= 0 && currentPageIndex < pageCanvasGroups.Count ? pageCanvasGroups[currentPageIndex] : null;
        CanvasGroup nextCanvas = nextPageIndex >= 0 && nextPageIndex < pageCanvasGroups.Count ? pageCanvasGroups[nextPageIndex] : null;

        pageTween?.Kill();

        if (nextPage != null)
            nextPage.SetActive(true);

        RectTransform currentRect = currentPage != null ? currentPage.GetComponent<RectTransform>() : null;
        RectTransform nextRect = nextPage != null ? nextPage.GetComponent<RectTransform>() : null;
        float direction = forward ? 1f : -1f;

        if (nextRect != null)
            nextRect.anchoredPosition = new Vector2(direction * pageOffsetX, 0f);
        if (nextCanvas != null)
        {
            nextCanvas.alpha = 0f;
            nextCanvas.blocksRaycasts = true;
            nextCanvas.interactable = true;
        }

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        if (currentRect != null)
            sequence.Join(currentRect.DOAnchorPosX(-direction * pageOffsetX, pageDuration));
        if (currentCanvas != null)
            sequence.Join(currentCanvas.DOFade(0f, pageDuration));
        if (nextRect != null)
            sequence.Join(nextRect.DOAnchorPosX(0f, pageDuration));
        if (nextCanvas != null)
            sequence.Join(nextCanvas.DOFade(1f, pageDuration));

        sequence.OnComplete(() =>
        {
            if (currentPage != null)
                currentPage.SetActive(false);
            if (currentRect != null)
                currentRect.anchoredPosition = Vector2.zero;
            if (nextRect != null)
                nextRect.anchoredPosition = Vector2.zero;

            if (currentCanvas != null)
            {
                currentCanvas.blocksRaycasts = false;
                currentCanvas.interactable = false;
            }

            currentPageIndex = nextPageIndex;
            RefreshNavigationButtons();
        });

        pageTween = sequence;
    }

    private void RefreshNavigationButtons()
    {
        if (previousButton != null)
            previousButton.interactable = currentPageIndex > 0;
        if (nextButton != null)
            nextButton.interactable = currentPageIndex < pageObjects.Count - 1;
    }

    private void SetPopupVisible(bool visible, bool instant)
    {
        if (popupRoot == null)
            return;

        isVisible = visible;
        popupTween?.Kill();

        if (visible)
        {
            popupRoot.SetActive(true);
            popupCanvasGroup.alpha = instant ? 1f : 0f;
            popupCanvasGroup.blocksRaycasts = true;
            popupCanvasGroup.interactable = true;

            if (!instant)
                popupTween = popupCanvasGroup.DOFade(1f, fadeDuration).SetUpdate(true);
        }
        else
        {
            popupCanvasGroup.blocksRaycasts = false;
            popupCanvasGroup.interactable = false;

            if (instant)
            {
                popupCanvasGroup.alpha = 0f;
                popupRoot.SetActive(false);
            }
            else
            {
                popupTween = popupCanvasGroup.DOFade(0f, fadeDuration).SetUpdate(true).OnComplete(() =>
                {
                    if (popupRoot != null)
                        popupRoot.SetActive(false);
                });
            }
        }
    }
}
