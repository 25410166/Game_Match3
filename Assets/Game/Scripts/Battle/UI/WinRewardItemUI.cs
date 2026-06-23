using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinRewardItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Image icon;

    public void Setup(string displayName, int amount, Sprite sprite = null)
    {
        Debug.Log($"[WinRewardItemUI] Setup called: displayName='{displayName}', amount={amount}, sprite={sprite?.name ?? "NULL"}");

        if (nameText != null)
        {
            nameText.text = string.IsNullOrWhiteSpace(displayName) ? "Gem" : displayName;
            Debug.Log($"[WinRewardItemUI] Set nameText: {nameText.text}");
        }
        else
            Debug.LogWarning("[WinRewardItemUI] nameText is NULL");

        if (amountText != null)
        {
            amountText.text = "x" + Mathf.Max(0, amount).ToString();
            Debug.Log($"[WinRewardItemUI] Set amountText: {amountText.text}");
        }
        else
            Debug.LogWarning("[WinRewardItemUI] amountText is NULL");

        if (icon != null)
        {
            Debug.Log($"[WinRewardItemUI] icon component found. Assigning sprite: {sprite?.name ?? "NULL"}");
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            Debug.Log($"[WinRewardItemUI] icon.sprite set to: {icon.sprite?.name ?? "NULL"}, icon.enabled: {icon.enabled}");
        }
        else
            Debug.LogError("[WinRewardItemUI] icon Image component is NULL!");
    }
}
