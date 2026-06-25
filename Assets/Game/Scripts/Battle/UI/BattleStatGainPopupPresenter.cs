using DG.Tweening;
using TMPro;
using UnityEngine;

public class BattleStatGainPopupPresenter : MonoBehaviour
{
    [Header("Popup Prefabs")]
    [SerializeField] private GameObject hpGainPopupPrefab;
    [SerializeField] private GameObject manaGainPopupPrefab;
    [SerializeField] private GameObject rageGainPopupPrefab;
    [SerializeField] private float textScaleMultiplier = 0.5f;

    public void Show(PlayerStats player, string statType, int amount)
    {
        if (player == null || amount <= 0)
            return;

        ShowPopup(ResolveAnchor(player.player, player.transform), ResolvePrefab(statType), amount);
    }

    public void Show(AIStats ai, string statType, int amount)
    {
        if (ai == null || amount <= 0)
            return;

        ShowPopup(ResolveAnchor(ai.AI, ai.transform), ResolvePrefab(statType), amount);
    }

    private void ShowPopup(Transform anchor, GameObject prefab, int amount)
    {
        if (anchor == null || prefab == null)
            return;

        GameObject popup = Instantiate(prefab, anchor.position + Vector3.up * 0.6f, Quaternion.identity);
        TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = $"+{amount}";
            text.transform.localScale = text.transform.localScale * textScaleMultiplier;
        }

        Transform popupTransform = popup.transform;
        popupTransform.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.Join(popupTransform.DOMoveY(popupTransform.position.y + 1.2f, 0.7f).SetEase(Ease.OutQuad));
        seq.Join(popupTransform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.28f, 8, 1f));

        if (text != null)
            seq.Join(text.DOFade(0f, 0.7f));

        seq.OnComplete(() =>
        {
            if (popup != null)
                Destroy(popup);
        });
    }

    private GameObject ResolvePrefab(string statType)
    {
        if (string.Equals(statType, "HP", System.StringComparison.OrdinalIgnoreCase))
            return hpGainPopupPrefab;

        if (string.Equals(statType, "Mana", System.StringComparison.OrdinalIgnoreCase))
            return manaGainPopupPrefab;

        if (string.Equals(statType, "Rage", System.StringComparison.OrdinalIgnoreCase))
            return rageGainPopupPrefab;

        return null;
    }

    private static Transform ResolveAnchor(GameObject actorRoot, Transform fallback)
    {
        if (actorRoot != null)
            return actorRoot.transform;

        return fallback;
    }
}
