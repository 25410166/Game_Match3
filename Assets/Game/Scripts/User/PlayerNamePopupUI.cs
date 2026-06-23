using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNamePopupUI : MonoBehaviour
{
    [Header("Popup References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI errorText;

    [Header("Name Rules")]
    [SerializeField] private int maxCharacters = 24;

    [Header("Optional References")]
    [SerializeField] private StarterPetPopupUI starterPetPopupUI;

    private void Start()
    {
        if (nameInput != null)
        {
            nameInput.characterLimit = maxCharacters;
            nameInput.onValueChanged.AddListener(OnNameChanged);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        ShowPopupIfRequired();
    }

    private void OnDestroy()
    {
        if (nameInput != null)
            nameInput.onValueChanged.RemoveListener(OnNameChanged);
    }

    private void ShowPopupIfRequired()
    {
        if (PlayerManager.Instance == null)
        {
            SetPopupVisible(true);
            ShowError(GetLocalizedText("player_name_popup_player_manager_not_ready", "PlayerManager chưa sẵn sàng."));
            return;
        }

        bool needInput = !PlayerManager.Instance.HasConfirmedPlayerName;
        SetPopupVisible(needInput);

        if (!needInput) return;

        if (nameInput != null)
            nameInput.text = string.Empty;

        ShowError(string.Empty);
    }

    private void OnNameChanged(string value)
    {
        if (nameInput == null) return;

        string sanitized = SanitizeName(value, maxCharacters);
        if (sanitized == value) return;

        int caret = nameInput.caretPosition;
        nameInput.text = sanitized;
        nameInput.caretPosition = Mathf.Clamp(caret - 1, 0, sanitized.Length);
    }

    private void OnContinueClicked()
    {
        if (PlayerManager.Instance == null)
        {
            ShowError(GetLocalizedText("player_name_popup_player_manager_not_found", "Không tìm thấy PlayerManager."));
            return;
        }

        string raw = nameInput != null ? nameInput.text : string.Empty;
        string sanitized = SanitizeName(raw, maxCharacters).Trim();

        if (!PlayerManager.IsValidPlayerName(sanitized, maxCharacters))
        {
            ShowError(GetLocalizedText("player_name_popup_invalid_name_format", "Tên tối đa 24 ký tự, chỉ gồm chữ/số và khoảng trắng."));
            return;
        }

        bool ok = PlayerManager.Instance.TryConfirmPlayerName(sanitized);
        if (!ok)
        {
            ShowError(GetLocalizedText("player_name_popup_invalid_name", "Tên không hợp lệ."));
            return;
        }

        PlayerManager.Instance.SaveData();
        ShowError(string.Empty);
        SetPopupVisible(false);
    }

    private string SanitizeName(string value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        System.Text.StringBuilder sb = new System.Text.StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsLetterOrDigit(c) || c == ' ')
                sb.Append(c);
        }

        string result = sb.ToString();
        if (result.Length > maxLen)
            result = result.Substring(0, maxLen);

        return result;
    }

    private void SetPopupVisible(bool visible)
    {
        if (popupRoot != null) popupRoot.SetActive(visible);
        else gameObject.SetActive(visible);
    }

    private void ShowError(string message)
    {
        if (errorText == null) return;
        errorText.text = message;
        errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private string GetLocalizedText(string key, string fallback)
    {
        if (LocalizationManager.Instance == null || !LocalizationManager.Instance.IsLoaded)
            return fallback;
        return LocalizationManager.Instance.GetText(key, fallback);
    }
}


