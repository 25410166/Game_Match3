using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TutorialBlocker : MonoBehaviour
{
    [SerializeField] private Image overlayImage;

    private void Awake()
    {
        if (overlayImage == null)
            overlayImage = GetComponent<Image>();

        if (overlayImage != null)
            overlayImage.raycastTarget = true;
    }
}
