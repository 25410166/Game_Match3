using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPopupView : MonoBehaviour
{
    public enum LayoutMode
    {
        Above = 0,
        LeftArrowRightText = 1
    }

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image darkOverlay;
    [SerializeField] private RectTransform textPopupRoot;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button skipButton;
    [SerializeField] private RectTransform arrowRoot;

    [Header("Placement Above")]
    [SerializeField] private Vector2 textOffset = new Vector2(0f, 140f);
    [SerializeField] private Vector2 arrowOffset = new Vector2(0f, 70f);

    [Header("Placement Left/Right")]
    [SerializeField] private Vector2 sideArrowOffset = new Vector2(-140f, 0f);
    [SerializeField] private Vector2 sideTextOffset = new Vector2(110f, 0f);

    [Header("Arrow Animation")]
    [SerializeField] private float arrowMoveDistance = 18f;
    [SerializeField] private float arrowMoveDuration = 0.7f;

    private System.Action skipCallback;
    private Canvas rootCanvas;
    private Tween arrowTween;
    private Vector2 arrowBasePosition;
    private LocalizedText localizedMessageText;
    private LayoutMode currentLayoutMode = LayoutMode.Above;

    private void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        if (messageText != null)
            localizedMessageText = messageText.GetComponent<LocalizedText>();

        if (skipButton != null)
        {
            skipButton.onClick.RemoveListener(HandleSkipClicked);
            skipButton.onClick.AddListener(HandleSkipClicked);
        }

        Hide();
    }

    private void OnDestroy()
    {
        arrowTween?.Kill();

        if (skipButton != null)
            skipButton.onClick.RemoveListener(HandleSkipClicked);
    }

    public void Show(string messageKey, RectTransform target, System.Action onSkip, LayoutMode layoutMode = LayoutMode.Above)
    {
        skipCallback = onSkip;
        currentLayoutMode = layoutMode;
        ApplyMessage(messageKey);

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);

        RefreshPosition(target);
        PlayArrowAnimation();
    }

    public void RefreshPosition(RectTransform target)
    {
        if (target == null)
            return;

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        Camera uiCamera = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = rootCanvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, target.position);
        Vector2 nextTextOffset = currentLayoutMode == LayoutMode.LeftArrowRightText ? sideTextOffset : textOffset;
        Vector2 nextArrowOffset = currentLayoutMode == LayoutMode.LeftArrowRightText ? sideArrowOffset : arrowOffset;

        if (textPopupRoot != null)
            PositionRect(textPopupRoot, screenPoint + nextTextOffset, uiCamera);

        if (arrowRoot != null)
        {
            PositionRect(arrowRoot, screenPoint + nextArrowOffset, uiCamera);
            arrowRoot.localEulerAngles = currentLayoutMode == LayoutMode.LeftArrowRightText ? new Vector3(0f, 0f, -90f) : Vector3.zero;
            arrowBasePosition = arrowRoot.anchoredPosition;
            PlayArrowAnimation();
        }
    }

    public void Hide()
    {
        skipCallback = null;
        arrowTween?.Kill();

        if (arrowRoot != null)
        {
            arrowRoot.anchoredPosition = arrowBasePosition;
            arrowRoot.localEulerAngles = Vector3.zero;
        }

        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void HandleSkipClicked()
    {
        skipCallback?.Invoke();
    }

    private void PositionRect(RectTransform rect, Vector2 screenPoint, Camera uiCamera)
    {
        if (rect == null)
            return;

        RectTransform parentRect = rect.parent as RectTransform;
        if (parentRect == null)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, uiCamera, out Vector2 localPoint))
            rect.anchoredPosition = localPoint;
    }

    private void PlayArrowAnimation()
    {
        if (arrowRoot == null)
            return;

        arrowTween?.Kill();
        arrowRoot.anchoredPosition = arrowBasePosition;

        if (currentLayoutMode == LayoutMode.LeftArrowRightText)
        {
            arrowTween = arrowRoot.DOAnchorPosX(arrowBasePosition.x + arrowMoveDistance, Mathf.Max(0.05f, arrowMoveDuration))
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
            return;
        }

        arrowTween = arrowRoot.DOAnchorPosY(arrowBasePosition.y + arrowMoveDistance, Mathf.Max(0.05f, arrowMoveDuration))
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true);
    }

    private void ApplyMessage(string messageKey)
    {
        if (messageText == null)
            return;

        if (localizedMessageText == null)
            localizedMessageText = messageText.GetComponent<LocalizedText>();

        if (localizedMessageText != null)
        {
            localizedMessageText.SetTextId(messageKey);
        }
        else
        {
            string nextText = messageKey;
            if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
                nextText = LocalizationManager.Instance.GetText(messageKey, messageKey);

            messageText.text = nextText;
        }

        messageText.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(messageText.rectTransform);
        if (textPopupRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(textPopupRoot);
    }
}
