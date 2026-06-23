using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Image image;
    public Sprite normalSprite;
    public Sprite critSprite;

    public void Setup(int damage, bool isCrit = false)
    {
        if (text != null)
        {
            text.text = damage.ToString();
            text.color = isCrit ? Color.yellow : Color.black;
        }

        if (image != null)
        {
            Sprite selectedSprite = isCrit && critSprite != null ? critSprite : normalSprite;
            if (selectedSprite != null)
                image.sprite = selectedSprite;
            image.enabled = selectedSprite != null;
        }

        // Reduce motion/scale intensity by ~50%
        transform.DOMoveY(transform.position.y + 1f, 1f);
        transform.DOScale(0.7f, 0.3f).SetLoops(2, LoopType.Yoyo);

        Destroy(gameObject, 1f);
    }
}