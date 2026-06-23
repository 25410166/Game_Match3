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
    private bool isAttackCard = false;

    public CardDatabase.Card CurrentCard => card;
    public int CurrentLevel => level;

    private void OnDestroy()
    {
        // Unsubscribe from player stat changes when destroyed
        if (GameManager.Instance?.player != null)
        {
            GameManager.Instance.player.OnStatChanged -= OnPlayerStatChanged;
        }
    }

    // Gán dữ liệu thẻ vào UI slot
    public void SetCard(CardDatabase.Card newCard, int lv, CardManager mgr)
    {
        // Unsubscribe from old card before setting new card
        if (GameManager.Instance?.player != null)
        {
            GameManager.Instance.player.OnStatChanged -= OnPlayerStatChanged;
        }

        card = newCard;
        level = lv;
        manager = mgr;

        if (card == null)
        {
            Debug.LogError("[CardSlotUI] Card is NULL in SetCard!");
            return;
        }

        var lvData = card.GetLevel(level);
        if (lvData == null)
        {
            Debug.LogError($"[CardSlotUI] Level data is NULL for level {level}");
            return;
        }

        if (icon != null)
            icon.sprite = lvData.sprite != null ? lvData.sprite : card.mainSprite;
        else
            Debug.LogWarning("[CardSlotUI] Icon Image component is NULL!");

        // Setup card name text based on card type
        if (cardNameText != null)
        {
            string cardDisplayName = $"{card.cardName} Lv{level}";

            // For Attack cards (Skill cards), add Mana and Rage cost
            if (card.type == CardDatabase.CardType.Attack)
            {
                SkillData cardSkill = GameDataManager.Instance?.GetSkillData(100);
                if (cardSkill != null)
                {
                    string manaText = LocalizationManager.Instance != null
                        ? LocalizationManager.Instance.GetText("mana", "Mana")
                        : "Mana";
                    string rageText = LocalizationManager.Instance != null
                        ? LocalizationManager.Instance.GetText("rage", "Rage")
                        : "Rage";
                    string hpText = LocalizationManager.Instance != null
                        ? LocalizationManager.Instance.GetText("hp", "HP")
                        : "HP";

                    string costDisplay = "";
                    if (cardSkill.manaCost > 0)
                        costDisplay += $"<color=blue>{manaText} {cardSkill.manaCost}</color>";
                    if (cardSkill.rageCost > 0)
                    {
                        if (!string.IsNullOrEmpty(costDisplay))
                            costDisplay += " ";
                        costDisplay += $"<color=red>{rageText} {cardSkill.rageCost}</color>";
                    }

                    float hpPercent = Mathf.Clamp(cardSkill.hpCostPercent, 0f, 100f);
                    if (hpPercent > 0f)
                    {
                        if (!string.IsNullOrEmpty(costDisplay))
                            costDisplay += " ";
                        string hpValue = Mathf.Abs(hpPercent - Mathf.Round(hpPercent)) < 0.001f
                            ? Mathf.RoundToInt(hpPercent).ToString()
                            : hpPercent.ToString("0.#");
                        costDisplay += $"<color=#6FFF00>{hpText} {hpValue}%</color>";
                    }

                    if (!string.IsNullOrEmpty(costDisplay))
                        cardDisplayName += $"\n{costDisplay}";
                }
            }

            cardNameText.text = cardDisplayName;
        }
        else
            Debug.LogWarning("[CardSlotUI] Card name text component is NULL!");

        // Đăng ký sự kiện click
        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(() => {
                if (manager != null)
                    manager.UseCard(card, level);
                else
                    Debug.LogError("[CardSlotUI] Manager is NULL!");
            });

            // Check if player has enough resources for Attack cards
            if (card.type == CardDatabase.CardType.Attack)
            {
                isAttackCard = true;
                // Subscribe to stat changes to update button state dynamically
                if (GameManager.Instance?.player != null)
                {
                    GameManager.Instance.player.OnStatChanged -= OnPlayerStatChanged;
                    GameManager.Instance.player.OnStatChanged += OnPlayerStatChanged;
                }
                
                RefreshButtonState();
            }
            else
            {
                isAttackCard = false;
                RefreshButtonState();
            }
        }
        else
        {
            Debug.LogError("[CardSlotUI] useButton is NULL! Cannot register click listener.");
        }
    }

    private bool CanUseAttackCard()
    {
        // Check if the player can act
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[CardSlotUI] CanUseAttackCard = false because GameManager.Instance is NULL");
            return false;
        }

        bool canPlayerMove = GameManager.Instance.CanPlayerMove();
        if (!canPlayerMove)
        {
            Debug.LogWarning($"[CardSlotUI] CanUseAttackCard = false because CanPlayerMove() is false | currentTurn={GameManager.Instance.currentTurn}");
            return false;
        }

        // Check if player has enough mana and rage
        if (GameManager.Instance.player == null)
        {
            Debug.LogWarning("[CardSlotUI] CanUseAttackCard = false because GameManager.Instance.player is NULL");
            return false;
        }

        SkillData cardSkill = GameDataManager.Instance?.GetSkillData(100);
        if (cardSkill == null)
        {
            Debug.LogError("[CardSlotUI] ⚠️ SKILL DATA NOT FOUND! ID=100 | CanUseAttackCard returning TRUE as fallback");
            return true; // If skill not found, allow anyway
        }

        // Get current player stats
        int currentMana = GameManager.Instance.player.Mana;
        int currentRage = GameManager.Instance.player.Rage;
        int requiredMana = cardSkill.manaCost;
        int requiredRage = cardSkill.rageCost;

        int requiredHp = 0;
        if (cardSkill.hpCostPercent > 0f)
        {
            int maxHp = GameManager.Instance.player.maxHP;
            requiredHp = Mathf.CeilToInt(maxHp * Mathf.Clamp(cardSkill.hpCostPercent, 0f, 100f) * 0.01f);
        }

        bool hasEnoughMana = currentMana >= requiredMana;
        bool hasEnoughRage = currentRage >= requiredRage;
        bool hasEnoughHp = requiredHp <= 0 || GameManager.Instance.player.HP > requiredHp;

        return hasEnoughMana && hasEnoughRage && hasEnoughHp;
    }

    private void UpdateAttackCardButtonState()
    {
        if (!isAttackCard || useButton == null)
            return;

        bool canUse = CanUseAttackCard();
        bool wasInteractable = useButton.interactable;
        useButton.interactable = canUse;
    }

    public void RefreshButtonState()
    {
        if (useButton == null)
            return;

        if (isAttackCard)
        {
            UpdateAttackCardButtonState();
            return;
        }

        bool canMove = GameManager.Instance != null && GameManager.Instance.CanPlayerMove();
        useButton.interactable = canMove;
    }

    private void OnPlayerStatChanged(string statType, int current, int max)
    {
        // Update button state when mana or rage changes
        if (isAttackCard && (statType.Equals("Mana", System.StringComparison.OrdinalIgnoreCase) ||
                              statType.Equals("Rage", System.StringComparison.OrdinalIgnoreCase) ||
                              statType.Equals("HP", System.StringComparison.OrdinalIgnoreCase)))
        {
            RefreshButtonState();
        }
    }
}
