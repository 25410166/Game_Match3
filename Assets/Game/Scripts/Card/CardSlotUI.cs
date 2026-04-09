using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image icon;
    public TextMeshProUGUI cardNameText;
    public Button useButton;

    private CardDatabase.Card card;
    private int level;
    private CardManager manager;

    // Gán dữ liệu thẻ vào UI slot
    public void SetCard(CardDatabase.Card newCard, int lv, CardManager mgr)
    {
        card = newCard;
        level = lv;
        manager = mgr;

        var lvData = card.GetLevel(level);

        icon.sprite = lvData.sprite != null ? lvData.sprite : card.mainSprite;
        cardNameText.text = $"{card.cardName} Lv{level}";

        // Đăng ký sự kiện click
        useButton.onClick.RemoveAllListeners();
        useButton.onClick.AddListener(() => manager.UseCard(card, level));
    }
}
