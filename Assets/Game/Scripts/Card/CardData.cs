// CardDatabase.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Card System/Card Database")]
public class CardDatabase : ScriptableObject
{
    [Header("List<Card>")]
    public List<Card> cards = new List<Card>();

    // Lấy theo index (0-based)
    public Card GetCard(int index)
    {
        if (index < 0 || index >= cards.Count) return null;
        return cards[index];
    }

    // Lấy theo id (unique)
    public Card GetCardById(int id)
    {
        return cards.Find(c => c.id == id);
    }

    // Lấy theo tên
    public Card GetCardByName(string name)
    {
        return cards.Find(c => c.cardName == name);
    }

    // Thêm (runtime) — đảm bảo id không trùng (tự tăng)
    public Card AddNewCard()
    {
        int newId = 0;
        if (cards.Count > 0) newId = Mathf.Max(cards.ConvertAll(c => c.id).ToArray()) + 1;
        var newCard = new Card { id = newId, cardName = "New Card", type = CardType.Attack, subType = AttackSubType.None };
        // khởi tạo 3 level mặc định
        newCard.EnsureThreeLevels();
        cards.Add(newCard);
        return newCard;
    }

    // Xoá (runtime)
    public void RemoveCard(int id)
    {
        var c = GetCardById(id);
        if (c != null) cards.Remove(c);
    }

    [System.Serializable]
    public class Card
    {
        [Tooltip("Unique ID cho thẻ (tự đặt hoặc để auto)")]
        public int id = -1;

        public string cardName;
        [TextArea] public string description;
        public Sprite mainSprite;

        public CardType type;
        public AttackSubType subType;

        // 3 cấp độ (luôn 3)
        public CardLevel[] levels = new CardLevel[3];

        // đảm bảo có 3 phần tử và id cấp đúng
        public void EnsureThreeLevels()
        {
            if (levels == null || levels.Length != 3)
            {
                levels = new CardLevel[3];
            }
            for (int i = 0; i < 3; i++)
            {
                if (levels[i] == null) levels[i] = new CardLevel();
                levels[i].level = i + 1;
            }
        }

        // lấy level
        public CardLevel GetLevel(int lv)
        {
            int i = Mathf.Clamp(lv - 1, 0, levels.Length - 1);
            return levels[i];
        }
    }

    [System.Serializable]
    public class CardLevel
    {
        [Range(1, 3)] public int level = 1;      // 1..3
        public int value;                        // sát thương / hồi máu / mana / rage
        public Sprite sprite;                    // sprite riêng cho cấp này
        public string effectDesc;                // mô tả ngắn
    }

    public enum CardType
    {
        Attack,
        Heal,
        Rage,
        Mana
    }

    public enum AttackSubType
    {
        None,
        Attack1,
        Attack2
    }
}
