using DG.Tweening;
using TMPro;
using UnityEngine;

public class BattleTurnBannerUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform yourTurnText;
    [SerializeField] private RectTransform enemyTurnText;

    [Header("Animation")]
    [SerializeField] private float moveDuration = 0.35f;
    [SerializeField] private float stayDuration = 0.5f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private Vector2 centerPosition = Vector2.zero;

    private Tween yourTurnTween;
    private Tween enemyTurnTween;
    private Vector2 yourTurnHiddenPosition;
    private Vector2 enemyTurnHiddenPosition;

    private void Awake()
    {
        CachePositions();
        SetBannerVisible(yourTurnText, false);
        SetBannerVisible(enemyTurnText, false);
    }

    private void OnDestroy()
    {
        yourTurnTween?.Kill();
        enemyTurnTween?.Kill();
    }

    public void ShowPlayerTurn()
    {
        PlayBanner(yourTurnText, ref yourTurnTween, yourTurnHiddenPosition);
    }

    public void ShowEnemyTurn()
    {
        PlayBanner(enemyTurnText, ref enemyTurnTween, enemyTurnHiddenPosition);
    }

    public void ShowTurn(GameManager.Turn turn)
    {
        if (turn == GameManager.Turn.Player)
            ShowPlayerTurn();
        else
            ShowEnemyTurn();
    }

    private void CachePositions()
    {
        if (yourTurnText != null)
            yourTurnHiddenPosition = yourTurnText.anchoredPosition;

        if (enemyTurnText != null)
            enemyTurnHiddenPosition = enemyTurnText.anchoredPosition;
    }

    private void PlayBanner(RectTransform target, ref Tween tween, Vector2 hiddenPosition)
    {
        if (target == null)
            return;

        tween?.Kill();
        SetBannerVisible(target, true);

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();

        target.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Append(target.DOAnchorPos(centerPosition, moveDuration).SetEase(Ease.OutCubic));
        sequence.Join(canvasGroup.DOFade(1f, fadeDuration));
        sequence.AppendInterval(stayDuration);
        sequence.Append(canvasGroup.DOFade(0f, fadeDuration));
        sequence.Join(target.DOAnchorPos(hiddenPosition, moveDuration).SetEase(Ease.InCubic));
        sequence.OnComplete(() => SetBannerVisible(target, false));

        tween = sequence;
    }

    private void SetBannerVisible(RectTransform target, bool visible)
    {
        if (target == null)
            return;

        target.gameObject.SetActive(visible);

        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = target.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = visible ? 1f : 0f;
    }
}
