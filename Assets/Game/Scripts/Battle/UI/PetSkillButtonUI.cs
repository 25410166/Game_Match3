using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PetSkillButtonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button skillButton;
    [SerializeField] private TextMeshProUGUI skillButtonText;
    [SerializeField] private Image skillButtonImage;
    [SerializeField] private ParticleSystem canClickFx;

    private SkillData skillData;
    private PlayerStats player;

    private void OnEnable()
    {
        RegisterLocalizationEvents();
        UpdateSkillButtonVisuals();
        UpdateClickFx(false);
    }

    private void OnDisable()
    {
        UnregisterLocalizationEvents();
        UpdateClickFx(false);
    }

    public void Initialize(PlayerStats playerStats)
    {
        player = playerStats;
        int skillId = playerStats != null ? playerStats.skillId : -1;
        SetupSkillButton(skillId);
        RefreshState();
    }

    public void RefreshState()
    {
        CheckSkillRequirements();
    }

    private void SetupSkillButton(int skillId)
    {
        if (skillButton == null)
            return;

        if (skillId <= 0 || GameDataManager.Instance == null)
        {
            skillData = null;
            skillButton.gameObject.SetActive(false);
            UpdateClickFx(false);
            return;
        }

        skillData = GameDataManager.Instance.GetSkillData(skillId);
        if (skillData == null)
        {
            skillButton.gameObject.SetActive(false);
            UpdateClickFx(false);
            return;
        }

        skillButton.gameObject.SetActive(true);
        skillButton.interactable = false;

        UpdateSkillButtonVisuals();

        skillButton.onClick.RemoveAllListeners();
        skillButton.onClick.AddListener(() =>
        {
            ExecutePlayerSkillAsync().Forget();
        });
    }

    private void UpdateSkillButtonVisuals()
    {
        UpdateSkillButtonText();
        UpdateSkillButtonIcon();
    }

    private void UpdateSkillButtonText()
    {
        if (skillData == null)
            return;

        TextMeshProUGUI textTarget = skillButtonText != null
            ? skillButtonText
            : (skillButton != null ? skillButton.GetComponentInChildren<TextMeshProUGUI>(true) : null);
        if (textTarget == null)
            return;

        string skillName = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText(skillData.skillName, skillData.skillName ?? string.Empty)
            : skillData.skillName;
        string manaText = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText("mana", "Mana")
            : "Mana";
        string rageText = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText("rage", "Rage")
            : "Rage";

        string costDisplay = string.Empty;
        if (skillData.manaCost > 0)
            costDisplay += $"<color=blue>{manaText} {skillData.manaCost}</color>";
        if (skillData.rageCost > 0)
        {
            if (!string.IsNullOrEmpty(costDisplay))
                costDisplay += " ";
            costDisplay += $"<color=red>{rageText} {skillData.rageCost}</color>";
        }

        textTarget.text = string.IsNullOrEmpty(costDisplay)
            ? skillName
            : $"{skillName}\n{costDisplay}";
    }

    private void UpdateSkillButtonIcon()
    {
        if (skillButtonImage == null)
            return;

        if (skillData == null || skillData.skillSprite == null)
        {
            skillButtonImage.sprite = null;
            skillButtonImage.enabled = false;
            return;
        }

        skillButtonImage.sprite = skillData.skillSprite;
        skillButtonImage.enabled = true;
    }

    private async UniTaskVoid ExecutePlayerSkillAsync()
    {
        if (GameManager.Instance == null || GameManager.Instance.player == null)
            return;

        if (!GameManager.Instance.CanPlayerUseSkill())
            return;

        if (GameManager.Instance.player.isAttacking)
            return;

        AudioManager.Instance?.PlayBattleCharacterSkillSound();
        UIManager.Instance?.PlayPlayerSkillUIFx(GetLocalizedSkillName());
        if (skillButton != null)
            skillButton.interactable = false;

        GameManager.Instance.OnCardActionStart();

        try
        {
            GameManager.Instance.player.Attack();
            await UniTask.WaitUntil(() => GameManager.Instance == null || GameManager.Instance.player == null || !GameManager.Instance.player.isAttacking);
        }
        finally
        {
            if (GameManager.Instance != null && GameManager.Instance.currentTurn == GameManager.Turn.Player)
                GameManager.Instance.EndTurn(null);
        }
    }

    private string GetLocalizedSkillName()
    {
        if (skillData == null)
            return string.Empty;

        return LocalizationManager.Instance != null
            ? LocalizationManager.Instance.GetText(skillData.skillName, skillData.skillName ?? string.Empty)
            : skillData.skillName ?? string.Empty;
    }

    private void CheckSkillRequirements()
    {
        if (skillButton == null || !skillButton.gameObject.activeSelf || skillData == null)
        {
            UpdateClickFx(false);
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.player == null)
        {
            UpdateClickFx(false);
            return;
        }

        var p = GameManager.Instance.player;

        int hpCost = Mathf.CeilToInt(p.maxHP * Mathf.Clamp(skillData.hpCostPercent, 0f, 100f) * 0.01f);
        bool hasResources = p.Mana >= skillData.manaCost &&
                            p.Rage >= skillData.rageCost &&
                            p.HP > hpCost;

        bool isPlayerTurn = (GameManager.Instance.currentTurn == GameManager.Turn.Player);
        bool canAction = !p.isAttacking;
        bool isSilenced = p.HasStatusEffect(StatusEffectType.Silence);
        bool canUseSkillNow = GameManager.Instance.CanPlayerUseSkill();

        bool canClick = hasResources && isPlayerTurn && canAction && !isSilenced && canUseSkillNow;
        skillButton.interactable = canClick;
        UpdateClickFx(canClick);
    }

    private void UpdateClickFx(bool canClick)
    {
        if (canClickFx == null)
            return;

        if (canClick)
        {
            if (!canClickFx.gameObject.activeSelf)
                canClickFx.gameObject.SetActive(true);
            if (!canClickFx.isPlaying)
                canClickFx.Play();
            return;
        }

        if (canClickFx.isPlaying)
            canClickFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (canClickFx.gameObject.activeSelf)
            canClickFx.gameObject.SetActive(false);
    }

    private void RegisterLocalizationEvents()
    {
        if (LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged -= UpdateSkillButtonText;
        LocalizationManager.Instance.OnLocalizationLoaded -= UpdateSkillButtonText;
        LocalizationManager.Instance.OnLanguageChanged += UpdateSkillButtonText;
        LocalizationManager.Instance.OnLocalizationLoaded += UpdateSkillButtonText;
    }

    private void UnregisterLocalizationEvents()
    {
        if (LocalizationManager.Instance == null)
            return;

        LocalizationManager.Instance.OnLanguageChanged -= UpdateSkillButtonText;
        LocalizationManager.Instance.OnLocalizationLoaded -= UpdateSkillButtonText;
    }
}






