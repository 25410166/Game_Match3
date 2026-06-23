using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatusEffectIconItem : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI turnText;

    public void Setup(Sprite icon, int remainingTurns)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        SetTurns(remainingTurns);
    }

    public void SetTurns(int remainingTurns)
    {
        if (turnText == null)
            return;

        int safeTurns = Mathf.Max(0, remainingTurns);
        turnText.text = safeTurns.ToString();
        turnText.enabled = safeTurns > 0;
    }
}
