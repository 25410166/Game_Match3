using UnityEngine;
using UnityEngine.UI;

public class RhythmArrowItemUI : MonoBehaviour
{
    [SerializeField] private Image arrowImage;

    public void Setup(Sprite sprite, Color color)
    {
        if (arrowImage == null)
            arrowImage = GetComponent<Image>();

        if (arrowImage == null)
            return;

        arrowImage.sprite = sprite;
        arrowImage.color = color;
        arrowImage.enabled = sprite != null;
    }
}
