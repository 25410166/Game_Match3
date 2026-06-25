using UnityEngine;
using UnityEngine.UI;
using TMPro; // nÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â¿u bÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â¡n dÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¹ng TextMeshPro
using Cysharp.Threading.Tasks;

public class CardManager : MonoBehaviour
{
    [Header("Database & Target")]
    public PlayerStats playerStats;       // GÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡n PlayerStats hoÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â·c PlayerController
    public AIStats aiStats;               // GÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡n AI target (nÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â¿u cÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â³)

    private CardDatabase database;
    private bool isCardAttackInProgress = false;

    [Header("UI Buttons (4 thÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â» trÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Âªn tay)")]
    public CardSlotUI[] cardSlots = new CardSlotUI[4]; // 4 ÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â´ thÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â»

    public void RefreshCardInteractables()
    {
        if (cardSlots == null)
            return;

        for (int i = 0; i < cardSlots.Length; i++)
        {
            CardSlotUI slot = cardSlots[i];
            if (slot != null)
                slot.RefreshButtonState();
        }
    }

    private void Start()
    {
        // TÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚Â»Ãƒâ€šÃ‚Â± ÃƒÆ’Ã¢â‚¬Å¾ÃƒÂ¢Ã¢â€šÂ¬Ã‹Å“ÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚Â»ÃƒÂ¢Ã¢â‚¬Å¾Ã‚Â¢ng lÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â¥y CardDatabase tÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚Â»Ãƒâ€šÃ‚Â« GameDataManager nÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â¿u chÃƒÆ’Ã¢â‚¬Â Ãƒâ€šÃ‚Â°a gÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â¡n
        if (database == null && GameDataManager.Instance != null)
        {
            database = GameDataManager.Instance.CardDatabaseObject as CardDatabase;
        }

        // Auto-find PlayerStats and AIStats if not assigned
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
            if (playerStats == null)
                Debug.LogError("[CardManager] PlayerStats NOT found! Assign it in Inspector.");
        }

        if (aiStats == null)
        {
            aiStats = FindObjectOfType<AIStats>();
            if (aiStats == null)
                Debug.LogError("[CardManager] AIStats NOT found! Assign it in Inspector.");
        }

        if (cardSlots == null || cardSlots.Length == 0)
            Debug.LogWarning("[CardManager] cardSlots array is empty! Assign UI slots in Inspector.");

        ValidateSetup();

        LoadCardsToSlots();
    }

    // Validation method to debug setup issues
    private void ValidateSetup()
    {
    }

    // Public method to check setup from console or elsewhere
    [ContextMenu("Validate Setup")]
    public void ValidateSetupContext()
    {
        ValidateSetup();
    }

    // --- NÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â P THÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Âº VÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â‚¬Å¡Ã‚Â¬O 4 ÃƒÆ’Ã†â€™ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â ---
    public void LoadCardsToSlots()
    {
        if (database == null || cardSlots == null)
            return;

        bool hasPrebattleContext = !string.IsNullOrWhiteSpace(PrebattleSelectionData.MapId) ||
                                   PrebattleSelectionData.PlayerPetId >= 0 ||
                                   PrebattleSelectionData.EnemyPetId >= 0;

        if (hasPrebattleContext)
        {
            LoadPrebattleSelectedCards();
            return;
        }

        for (int i = 0; i < cardSlots.Length; i++)
        {
            var card = database.cards[i]; // hoÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â·c Random.Range(0, database.cards.Count)
            int randomLevel = Random.Range(1, 4); // ngÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â«u nhiÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Âªn cÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Â¥p 1-3

            cardSlots[i].SetCard(card, randomLevel, this);
        }
    }

    private void LoadPrebattleSelectedCards()
    {
        int selectedCount = PrebattleSelectionData.SelectedCards != null ? PrebattleSelectionData.SelectedCards.Count : 0;

        for (int i = 0; i < cardSlots.Length; i++)
        {
            CardSlotUI slot = cardSlots[i];
            if (slot == null)
                continue;

            if (i >= selectedCount)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            PrebattleCardData selected = PrebattleSelectionData.SelectedCards[i];
            if (selected == null)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            CardDatabase.Card card = database.GetCardById(selected.cardId);
            if (card == null)
            {
                slot.gameObject.SetActive(false);
                continue;
            }

            slot.gameObject.SetActive(true);
            slot.SetCard(card, Mathf.Clamp(selected.cardLevel, 1, 3), this);
        }
    }

    // --- THÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚Â»Ãƒâ€šÃ‚Â°C THI HIÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚Â»ÃƒÂ¢Ã¢â€šÂ¬Ã‚Â U ÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚Â»Ãƒâ€šÃ‚Â¨NG THÃƒÆ’Ã‚Â¡Ãƒâ€šÃ‚ÂºÃƒâ€šÃ‚Âº ---
    public void UseCard(CardDatabase.Card card, int level)
    {
        // Prevent using cards when the player cannot act
        if (GameManager.Instance == null || !GameManager.Instance.CanPlayerMove())
        {
            Debug.LogWarning("[CardManager] Cannot use card: player cannot act now.");
            return;
        }

        if (card == null)
        {
            Debug.LogError("[CardManager] Card is NULL!");
            return;
        }

        if (playerStats == null)
        {
            Debug.LogError("[CardManager] PlayerStats is NULL! Cannot use card.");
            return;
        }

        if (card.type == CardDatabase.CardType.Attack)
        {
            AudioManager.Instance?.PlayBattleCharacterSkillSound();
            string localizedSkillName = card != null
                ? (LocalizationManager.Instance != null
                    ? LocalizationManager.Instance.GetText(card.cardName, card.cardName ?? string.Empty)
                    : card.cardName ?? string.Empty)
                : string.Empty;
            UIManager.Instance?.PlayPlayerSkillUIFx(localizedSkillName);
        }
        var lvData = card.GetLevel(level);
        if (lvData == null)
        {
            Debug.LogError($"[CardManager] Card level data is NULL for level {level}");
            return;
        }

        switch (card.type)
        {
            case CardDatabase.CardType.Attack:
                // Execute attack card asynchronously (pass level)
                // Attack cards are NOT consumed - can be used multiple times
                ExecuteCardAttackAsync(card, lvData.value, level).Forget();
                break;

            case CardDatabase.CardType.Heal:
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.IncreasePlayerHP(lvData.value);
                }
                else
                {
                    var targetPlayerForHeal = GameManager.Instance != null && GameManager.Instance.player != null ? GameManager.Instance.player : playerStats;
                    if (targetPlayerForHeal != null)
                        targetPlayerForHeal.Heal(lvData.value);
                }
                ConsumeNonAttackCard(card, level);
                break;

            case CardDatabase.CardType.Rage:
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.IncreasePlayerRage(lvData.value);
                }
                else
                {
                    var targetPlayerForRage = GameManager.Instance != null && GameManager.Instance.player != null ? GameManager.Instance.player : playerStats;
                    if (targetPlayerForRage != null)
                        targetPlayerForRage.GainRage(lvData.value);
                }
                ConsumeNonAttackCard(card, level);
                break;

            case CardDatabase.CardType.Mana:
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.IncreasePlayerMana(lvData.value);
                }
                else
                {
                    var targetPlayerForMana = GameManager.Instance != null && GameManager.Instance.player != null ? GameManager.Instance.player : playerStats;
                    if (targetPlayerForMana != null)
                        targetPlayerForMana.GainMana(lvData.value);
                }
                ConsumeNonAttackCard(card, level);
                break;

            case CardDatabase.CardType.Immortal:
                {
                    var targetPlayerForImmortal = GameManager.Instance != null && GameManager.Instance.player != null ? GameManager.Instance.player : playerStats;
                    if (targetPlayerForImmortal != null)
                        targetPlayerForImmortal.ScheduleImmortal(Mathf.Max(1, lvData.value));

                    UIManager.Instance?.PreviewImmortalEffectOnPlayer();
                    ConsumeNonAttackCard(card, level);
                    break;
                }
        }
    }

    // Check if a card attack is currently in progress
    public bool IsCardAttackInProgress => isCardAttackInProgress;

    /// <summary>
    /// Execute a card attack using skill system (skill id 100 for card attacks)
    /// </summary>
    private async UniTaskVoid ExecuteCardAttackAsync(CardDatabase.Card card, int baseValue, int level)
    {
        if (playerStats == null)
        {
            Debug.LogError("[CardManager] ExecuteCardAttackAsync: PlayerStats is NULL!");
            return;
        }

        if (aiStats == null)
        {
            Debug.LogError("[CardManager] ExecuteCardAttackAsync: AIStats is NULL!");
            return;
        }

        isCardAttackInProgress = true;
        
        // Signal GameManager that player is taking action
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCardActionStart();
        }
        else
        {
            Debug.LogWarning("[CardManager] GameManager.Instance is NULL");
        }

        try
        {
            int baseDamage = Mathf.Max(1, baseValue);
            
            // Get skill id 100 for card attacks
            SkillData skillData = GameDataManager.Instance?.GetSkillData(100);
            if (skillData == null)
            {
                Debug.LogWarning("[CardManager] Skill id 100 not found, falling back to direct damage");
                await FallbackDirectDamageAsync(baseDamage);
                return;
            }

            // Create a copy of the skill with card-specific damage
            SkillData cardSkill = ScriptableObject.CreateInstance<SkillData>();
            cardSkill.skillId = skillData.skillId;
            cardSkill.skillName = $"{card.cardName} Attack";
            cardSkill.attackType = skillData.attackType;
            cardSkill.rangeType = SkillRangeType.DirectImpact;  // Card attacks use DirectImpact - charged attack effect
            cardSkill.hitCount = skillData.hitCount;
            cardSkill.hitDelay = skillData.hitDelay;
            cardSkill.damageMultiplier = skillData.damageMultiplier;
            cardSkill.animationPlayCount = skillData.animationPlayCount;
            cardSkill.fxPlayCount = skillData.fxPlayCount;
            cardSkill.animationDuration = skillData.animationDuration;
            cardSkill.manaCost = skillData.manaCost;
            cardSkill.rageCost = skillData.rageCost;
            cardSkill.hpCostPercent = skillData.hpCostPercent;
            cardSkill.fxPrefab = skillData.fxPrefab;
            cardSkill.projectilePrefab = skillData.projectilePrefab;
            cardSkill.gemTypesAffected = skillData.gemTypesAffected;
            cardSkill.boardEffectType = skillData.boardEffectType;
            
            // Queue the skill in PlayerStats (similar to TriggerGemAttack)
            // This will be executed when Attack() is called
            playerStats.SetQueuedCardSkill(cardSkill, baseDamage);
            // Wait for attack to complete
            playerStats.Attack();
            await UniTask.WaitUntil(() => !playerStats.isAttacking);
            // Attack cards are NOT consumed - they can be used multiple times
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CardManager] Exception in ExecuteCardAttackAsync: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            isCardAttackInProgress = false;
            
            // End the turn after card attack completes
            if (GameManager.Instance != null && GameManager.Instance.currentTurn == GameManager.Turn.Player)
            {
                GameManager.Instance.EndTurn(null);
            }
            else if (GameManager.Instance == null)
            {
                Debug.LogWarning("[CardManager] GameManager.Instance is NULL in finally block");
            }
            else
            {
                Debug.LogWarning($"[CardManager] Not ending turn - currentTurn is {GameManager.Instance.currentTurn}");
            }
        }
    }

    private void ConsumeNonAttackCard(CardDatabase.Card card, int level)
    {
        if (card == null) return;

        // Consume one copy from player's owned cards
        if (PlayerManager.Instance != null)
        {
            bool consumed = PlayerManager.Instance.ConsumeOwnedCard(card.id, level, 1);
            if (consumed)
            {
                // Hide the used card slot in UI
                for (int s = 0; s < cardSlots.Length; s++)
                {
                    var slot = cardSlots[s];
                    if (slot != null && slot.CurrentCard == card && slot.CurrentLevel == level)
                    {
                        slot.gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }
        // Do NOT end the turn for non-attack cards; player can continue
    }

    /// <summary>
    /// Fallback to direct damage if skill 100 is not found
    /// </summary>
    private async UniTask FallbackDirectDamageAsync(int baseDamage)
    {
        if (aiStats == null)
        {
            Debug.LogError("[CardManager] FallbackDirectDamageAsync: AIStats is NULL!");
            return;
        }

        if (playerStats == null)
        {
            Debug.LogError("[CardManager] FallbackDirectDamageAsync: PlayerStats is NULL!");
            return;
        }

        // Apply damage directly
        aiStats.TakeDamage(baseDamage);
        
        // Wait a bit for damage to settle
        await UniTask.Delay(500);
    }

    private int ApplyCardDamageToAI(int amount)
    {
        if (aiStats == null)
            return 0;

        int before = aiStats.Health;
        aiStats.TakeDamage(amount);
        return Mathf.Max(0, before - aiStats.Health);
    }

    private SkillData ResolveCardSkill(CardDatabase.Card card)
    {
        if (card == null || GameDataManager.Instance == null)
            return null;

        // For Attack cards, always use skill id 100
        if (card.type == CardDatabase.CardType.Attack)
        {
            return GameDataManager.Instance.GetSkillData(100);
        }

        // For other card types, map by card id if needed
        return GameDataManager.Instance.GetSkillData(card.id);
    }
}

