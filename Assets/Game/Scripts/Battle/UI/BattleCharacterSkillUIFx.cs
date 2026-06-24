using DG.Tweening;
using UnityEngine;

public class BattleCharacterSkillUIFx : MonoBehaviour
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private float moveDistance = 180f;
    [SerializeField] private float moveDuration = 0.2f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private Ease moveEase = Ease.OutCubic;
    [SerializeField] private Ease returnEase = Ease.InCubic;

    private Vector2 initialAnchoredPosition;
    private Tween activeTween;
    private bool hasCachedInitialPosition;

    private void Awake()
    {
        if (targetRect == null)
            targetRect = transform as RectTransform;

        CacheInitialPosition();
    }

    private void OnEnable()
    {
        CacheInitialPosition();
    }

    private void OnDestroy()
    {
        activeTween?.Kill();
    }

    public void Play()
    {
        if (targetRect == null)
            return;

        CacheInitialPosition();
        activeTween?.Kill();
        targetRect.anchoredPosition = initialAnchoredPosition;

        Vector2 targetPosition = initialAnchoredPosition + Vector2.right * moveDistance;
        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Append(targetRect.DOAnchorPos(targetPosition, moveDuration).SetEase(moveEase));
        sequence.AppendInterval(holdDuration);
        sequence.Append(targetRect.DOAnchorPos(initialAnchoredPosition, moveDuration).SetEase(returnEase));
        activeTween = sequence;
    }

    private void CacheInitialPosition()
    {
        if (targetRect == null)
            return;

        if (!hasCachedInitialPosition)
        {
            initialAnchoredPosition = targetRect.anchoredPosition;
            hasCachedInitialPosition = true;
        }
    }
}