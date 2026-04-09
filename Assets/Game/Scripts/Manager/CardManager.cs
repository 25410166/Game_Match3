using UnityEngine;
using UnityEngine.UI;
using TMPro; // nếu bạn dùng TextMeshPro

public class CardManager : MonoBehaviour
{
    [Header("Database & Target")]
    public CardDatabase database;         // Gán CardDatabase vào đây
    public PlayerStats playerStats;       // Gán PlayerStats hoặc PlayerController
    public AIStats aiStats;               // Gán AI target (nếu có)

    [Header("UI Buttons (4 thẻ trên tay)")]
    public CardSlotUI[] cardSlots = new CardSlotUI[4]; // 4 ô thẻ

    private void Start()
    {
        LoadCardsToSlots();
    }

    // --- NẠP THẺ VÀO 4 Ô ---
    public void LoadCardsToSlots()
    {
        for (int i = 0; i < cardSlots.Length; i++)
        {
            var card = database.cards[i]; // hoặc Random.Range(0, database.cards.Count)
            int randomLevel = Random.Range(1, 4); // ngẫu nhiên cấp 1-3

            cardSlots[i].SetCard(card, randomLevel, this);
        }
    }

    // --- THỰC THI HIỆU ỨNG THẺ ---
    public void UseCard(CardDatabase.Card card, int level)
    {
        var lvData = card.GetLevel(level);

        Debug.Log($"Dùng thẻ {card.cardName} (Lv {level})");

        switch (card.type)
        {
            case CardDatabase.CardType.Attack:
                playerStats.Attack(); // hoạt ảnh / animation
                aiStats.TakeDamage(lvData.value);
                break;

            case CardDatabase.CardType.Heal:
                playerStats.Heal(lvData.value);
                break;

            case CardDatabase.CardType.Rage:
                playerStats.GainRage(lvData.value);
                break;

            case CardDatabase.CardType.Mana:
                playerStats.GainMana(lvData.value);
                break;
        }
    }
}
