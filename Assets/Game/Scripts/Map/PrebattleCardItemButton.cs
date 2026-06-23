using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrebattleCardItemButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Button infoButton;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI txtQuantity;

    private int cardId;
    private int cardLevel;
    private Action<int, int> onSelect;
    private Action<int, int> onShowInfo;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (AudioManager.Instance != null)
            AudioManager.Instance.RegisterButtonClick(button);
    }

    public void Setup(int inCardId, int inCardLevel, string inName, int inQuantity, Sprite inIcon, Action<int, int> callback, Action<int, int> callbackInfo = null)
    {
        cardId = inCardId;
        cardLevel = inCardLevel;
        onSelect = callback;
        onShowInfo = callbackInfo;

       

        if (txtQuantity != null)
            txtQuantity.text = "x" + Mathf.Max(0, inQuantity);

        if (icon != null)
        {
            icon.sprite = inIcon;
            icon.enabled = inIcon != null;
        }

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
            if (AudioManager.Instance != null)
                AudioManager.Instance.RegisterButtonClick(button);
        }

        if (infoButton != null)
        {
            infoButton.onClick.RemoveListener(OnClickInfo);
            infoButton.onClick.AddListener(OnClickInfo);
            if (AudioManager.Instance != null)
                AudioManager.Instance.RegisterButtonClick(infoButton);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);

        if (infoButton != null)
            infoButton.onClick.RemoveListener(OnClickInfo);
    }

    private void OnClick()
    {
        if (onSelect != null)
            onSelect.Invoke(cardId, cardLevel);
    }

    private void OnClickInfo()
    {
        if (onShowInfo != null)
            onShowInfo.Invoke(cardId, cardLevel);
    }
}
