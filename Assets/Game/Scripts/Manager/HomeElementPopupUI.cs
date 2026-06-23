using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HomeElementPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private CanvasGroup popupCanvasGroup;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private float fadeDuration = 0.2f;

    private Tween popupTween;

    public Button OpenButton => openButton;
    public Button CloseButton => closeButton;
    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;
    public event Action OnOpened;
    public event Action OnClosed;

    private void Awake()
    {
        if (popupRoot == null)
            popupRoot = gameObject;

        if (popupCanvasGroup == null && popupRoot != null)
            popupCanvasGroup = popupRoot.GetComponent<CanvasGroup>();

        if (popupCanvasGroup == null && popupRoot != null)
            popupCanvasGroup = popupRoot.AddComponent<CanvasGroup>();

        BindButtons();
        SetVisible(false, true);
    }

    private void OnDestroy()
    {
        popupTween?.Kill();
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
    }

    public void OpenPopup()
    {
        SetVisible(true, false);
        OnOpened?.Invoke();
    }

    public void ClosePopup()
    {
        SetVisible(false, false);
        OnClosed?.Invoke();
    }

    private void SetVisible(bool visible, bool instant)
    {
        if (popupRoot == null)
            return;

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
