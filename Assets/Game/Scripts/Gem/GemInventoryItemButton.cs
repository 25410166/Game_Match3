using System;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GemInventoryItemButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image gemIcon;
    [SerializeField] private TextMeshProUGUI txtLevel;
    [SerializeField] private TextMeshProUGUI txtQuantity;

    private int gemLevel;
    private int gemQuantity;
    private Action<int> onClickGem;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (AudioManager.Instance != null)
            AudioManager.Instance.RegisterButtonClick(button);
    }

    public void Setup(int level, int quantity, Sprite icon, Action<int> onClick)
    {
        gemLevel = Mathf.Max(1, level);
        gemQuantity = Mathf.Max(0, quantity);
        onClickGem = onClick;

        if (gemIcon != null)
        {
            gemIcon.sprite = icon;
            gemIcon.enabled = icon != null;
        }

        if (txtLevel != null)
            txtLevel.text = "Lv. " + gemLevel;

        if (txtQuantity != null)
            txtQuantity.text = "x" + gemQuantity;

        if (button != null)
        {
            button.onClick.RemoveListener(OnClickButton);
            button.onClick.AddListener(OnClickButton);
            if (AudioManager.Instance != null)
                AudioManager.Instance.RegisterButtonClick(button);
        }

    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClickButton);
    }

    private void OnClickButton()
    {
        if (onClickGem != null)
            onClickGem.Invoke(gemLevel);
    }
}
