using UnityEngine;
using UnityEngine.UI;

public class AreaBackgroundManager : MonoBehaviour
{
    [SerializeField] private GameObject[] areaBackgrounds = new GameObject[7]; // 7 areas (0-6)
    [SerializeField] private Image backgroundImage; // Background RectTransform
    [SerializeField] private Sprite[] areaBackgroundSprites = new Sprite[7]; // 7 background sprites
    
    private static AreaBackgroundManager instance;

    private void Awake()
    {
        instance = this;
    }

    public static void SetAreaBackground(int area)
    {
        if (instance == null)
            return;

        int index = Mathf.Clamp(area - 1, 0, 6);

        // Set environment gameobjects
        for (int i = 0; i < instance.areaBackgrounds.Length; i++)
        {
            if (instance.areaBackgrounds[i] != null)
            instance.areaBackgrounds[i].SetActive(i == index);
        }

        // Set background image
        if (instance.backgroundImage != null && index < instance.areaBackgroundSprites.Length && instance.areaBackgroundSprites[index] != null)
            instance.backgroundImage.sprite = instance.areaBackgroundSprites[index];
    }
}
