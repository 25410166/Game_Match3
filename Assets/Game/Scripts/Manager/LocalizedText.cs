using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string textId;
    [SerializeField] private string fallbackText;
    [SerializeField] private bool debugMode = false;

    private TMP_Text tmpText;

    private void Reset()
    {
        TryResolveTarget();
    }

    private void Awake()
    {
        TryResolveTarget();
    }

    private void Start()
    {
        Refresh();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLocalizationLoaded += Refresh;
            LocalizationManager.Instance.OnLanguageChanged += Refresh;

            if (debugMode)
                Debug.Log($"[LocalizedText] Subscribed to LocalizationManager events for textId: {textId}");
        }
        else
        {
            if (debugMode)
                Debug.LogWarning($"[LocalizedText] LocalizationManager.Instance is null!");
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLocalizationLoaded -= Refresh;
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;
        }
    }

    public void SetTextId(string id)
    {
        textId = id;
        Refresh();
    }

    public void Refresh()
    {
        if (tmpText == null)
            TryResolveTarget();

        string nextText = fallbackText;

        if (LocalizationManager.Instance != null && LocalizationManager.Instance.IsLoaded)
        {
            nextText = LocalizationManager.Instance.GetText(textId, fallbackText);
            if (debugMode)
                Debug.Log($"[LocalizedText] Refreshing textId: {textId}, result: {nextText}");
        }
        else
        {
            if (debugMode)
                Debug.LogWarning($"[LocalizedText] Localization not ready. textId: {textId}");
        }

        ApplyText(nextText);
    }

    private void TryResolveTarget()
    {
        tmpText = GetComponent<TMP_Text>();

        if (tmpText == null && debugMode)
        {
            Debug.LogError($"[LocalizedText] TMP_Text component not found on this GameObject! textId: {textId}");
        }

        if (debugMode && tmpText != null)
        {
            Debug.Log($"[LocalizedText] TryResolveTarget - TMP_Text found");
        }
    }

    private void ApplyText(string value)
    {
        if (string.IsNullOrEmpty(value))
            value = fallbackText;

        if (tmpText != null)
        {
            tmpText.text = value;
            if (debugMode)
                Debug.Log($"[LocalizedText] Applied to TMP_Text: {value}");
            return;
        }

        if (debugMode)
            Debug.LogError($"[LocalizedText] TMP_Text component not assigned or not found!");
    }
}
