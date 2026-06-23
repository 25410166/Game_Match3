using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHome : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI txtPlayerName;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtExp;
    [SerializeField] private TextMeshProUGUI txtGold;
    [SerializeField] private TextMeshProUGUI txtDiamond;
    [SerializeField] private Slider expSlider;

    [Header("Guideline")]
    [SerializeField] private Button guidelineButton;
    [SerializeField] private HomeGuidelinePopupUI guidelinePopup;

    [Header("Level Reward")]
    [SerializeField] private LevelRewardPopupUI levelRewardPopup;

    private void Awake()
    {
        if (guidelineButton != null)
        {
            guidelineButton.onClick.RemoveListener(OpenGuidelinePopup);
            guidelineButton.onClick.AddListener(OpenGuidelinePopup);
        }

    }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged += RefreshUI;

        RefreshUI();

        if (levelRewardPopup == null)
            levelRewardPopup = FindObjectOfType<LevelRewardPopupUI>(true);

        if (levelRewardPopup != null)
            levelRewardPopup.TryShowPendingReward();
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataChanged -= RefreshUI;
    }

    private void OpenGuidelinePopup()
    {
        if (guidelinePopup == null)
            guidelinePopup = FindObjectOfType<HomeGuidelinePopupUI>(true);

        if (guidelinePopup != null)
            guidelinePopup.OpenPopup();
    }

    public void RefreshUI()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.Data == null)
            return;

        var data = PlayerManager.Instance.Data;

        int currentLevel = Mathf.Max(1, data.level);
        int expRequire = PlayerManager.Instance.GetExpRequireForLevel(currentLevel);

        if (txtPlayerName != null)
            txtPlayerName.text = string.IsNullOrWhiteSpace(data.playerName) ? "-" : data.playerName;

        if (txtLevel != null)
            txtLevel.text = "Lv. " + currentLevel;

        if (txtExp != null)
            txtExp.text = data.currentExp + " / " + expRequire;

        if (txtGold != null)
            txtGold.text = data.gold.ToString();

        if (txtDiamond != null)
            txtDiamond.text = data.diamond.ToString();

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = Mathf.Max(1, expRequire);
            expSlider.value = Mathf.Clamp(data.currentExp, 0, expRequire);
        }
    }
}
