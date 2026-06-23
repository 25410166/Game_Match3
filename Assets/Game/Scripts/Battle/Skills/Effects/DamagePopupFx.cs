using DG.Tweening;
using TMPro;
using UnityEngine;

public static class DamagePopupFx
{
    public static void ShowDamage(GameObject popupPrefab, Transform target, int damage, bool isCrit = false)
    {
        if (popupPrefab == null || target == null)
            return;

        GameObject popup = Object.Instantiate(popupPrefab, target.position + Vector3.up * 0.6f, Quaternion.identity);

        DamagePopup popupComponent = popup.GetComponent<DamagePopup>();
        if (popupComponent != null)
        {
            popupComponent.Setup(damage, isCrit);
            return;
        }

        TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = damage.ToString();
            text.color = Color.black;
        }

        Transform t = popup.transform;
        t.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.Join(t.DOMoveY(t.position.y + 1.2f, 0.7f).SetEase(Ease.OutQuad));
        seq.Join(t.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.28f, 8, 1f));

        if (text != null)
            seq.Join(text.DOFade(0f, 0.7f));

        seq.OnComplete(() =>
        {
            if (popup != null)
                Object.Destroy(popup);
        });
    }

    public static void ShowText(GameObject popupPrefab, Transform target, string message)
    {
        if (popupPrefab == null || target == null)
            return;

        GameObject popup = Object.Instantiate(popupPrefab, target.position + Vector3.up * 0.6f, Quaternion.identity);

        TextMeshProUGUI text = popup.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            text.text = message ?? string.Empty;
            text.color = Color.black;
        }

        Transform t = popup.transform;
        t.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.Join(t.DOMoveY(t.position.y + 1.2f, 0.7f).SetEase(Ease.OutQuad));
        seq.Join(t.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.28f, 8, 1f));

        if (text != null)
            seq.Join(text.DOFade(0f, 0.7f));

        seq.OnComplete(() =>
        {
            if (popup != null)
                Object.Destroy(popup);
        });
    }
}
