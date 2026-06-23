using UnityEngine;
using UnityEngine.UI;

public static class BoardPresentationExtensions
{
    public static void SetPresentationVisible(this Board board, bool visible)
    {
        if (board == null)
            return;

        CanvasGroup canvasGroup = board.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = board.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.blocksRaycasts = visible;
        canvasGroup.interactable = visible;
    }
}
