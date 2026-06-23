// Placeholder file.
// Runtime implementation moved to:
// `Assets/Game/Scripts/User/ShopManager.cs`
using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI txtItemName;
    [SerializeField] private Image imgItemCard;
    [SerializeField] private Image imgItemGem;
    [SerializeField] private TextMeshProUGUI txtPrice;
    [SerializeField] private TMP_InputField inputQuantity;
    [SerializeField] private Button btnBuy;
    [SerializeField] private Button btnInfo;

    private int pricePerUnit;
    private Action<int> onBuy;
    private Action onInfo;
    private bool isCardItem;

    private void Awake()
    {
        if (btnBuy == null || btnInfo == null)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button b = buttons[i];
                if (b == null)
                    continue;

                if (btnBuy == null && string.Equals(b.name, "BtnBuy", StringComparison.OrdinalIgnoreCase))
                {
                    btnBuy = b;
                    continue;
                }

                if (btnInfo == null && string.Equals(b.name, "BtnInfo", StringComparison.OrdinalIgnoreCase))
                {
                    btnInfo = b;
                    continue;
                }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RegisterButtonClick(btnBuy);
            AudioManager.Instance.RegisterButtonClick(btnInfo);
        }
            }
        }
    }

    public void Setup(string itemName, Sprite sprite, int unitPrice, bool isCard, Action<int> buyCallback, Action infoCallback)
    {
        pricePerUnit = Mathf.Max(0, unitPrice);
        onBuy = buyCallback;
        onInfo = infoCallback;
        isCardItem = isCard;

        SetComponentText(txtItemName, LocalizeText(itemName));
        SetItemImage(sprite, isCard);
        SetImageGameObjectsActive(isCard);
        SetComponentText(txtPrice, pricePerUnit.ToString());

        SetupInputQuantity();

        if (btnBuy != null)
        {
            btnBuy.onClick.RemoveAllListeners();
            btnBuy.onClick.AddListener(OnBuyClicked);
            if (AudioManager.Instance != null)
                AudioManager.Instance.RegisterButtonClick(btnBuy);
        }

        if (btnInfo != null)
        {
            btnInfo.gameObject.SetActive(isCard);
            btnInfo.onClick.RemoveAllListeners();
            btnInfo.onClick.AddListener(OnInfoClicked);
            if (AudioManager.Instance != null)
                AudioManager.Instance.RegisterButtonClick(btnInfo);
        }
    }

    private void SetItemImage(Sprite sprite, bool isCard)
    {
        Image targetImage = isCard ? imgItemCard : imgItemGem;
        if (targetImage != null)
            targetImage.sprite = sprite;
    }

    private string LocalizeText(string textOrKey)
    {
        if (string.IsNullOrWhiteSpace(textOrKey))
            return string.Empty;

        LocalizationManager lm = LocalizationManager.Instance;
        if (lm != null && lm.IsLoaded)
            return lm.GetText(textOrKey, textOrKey);

        return textOrKey;
    }

    private void SetImageGameObjectsActive(bool isCard)
    {
        if (imgItemCard != null)
            imgItemCard.gameObject.SetActive(isCard);

        if (imgItemGem != null)
            imgItemGem.gameObject.SetActive(!isCard);
    }

    private void SetupInputQuantity()
    {
        if (inputQuantity == null) return;

        SetIntProperty(inputQuantity, "characterLimit", 2);
        SetEnumPropertyIfExists(inputQuantity, "contentType", "IntegerNumber");
        SetComponentText(inputQuantity, "1");
    }

    private int GetQuantity()
    {
        if (inputQuantity == null) return 1;

        string text = GetComponentText(inputQuantity);
        string digits = string.Empty;
        for (int i = 0; i < text.Length; i++)
        {
            if (char.IsDigit(text[i])) digits += text[i];
        }

        int qty;
        if (!int.TryParse(digits, out qty)) qty = 1;
        qty = Mathf.Clamp(qty, 1, 99);
        SetComponentText(inputQuantity, qty.ToString());
        return qty;
    }

    private void OnBuyClicked()
    {
        if (onBuy != null)
            onBuy.Invoke(GetQuantity());
    }

    private void OnInfoClicked()
    {
        if (onInfo != null)
            onInfo.Invoke();
    }

    private void SetIntProperty(Component c, string propName, int value)
    {
        PropertyInfo p = c.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
        if (p != null && p.PropertyType == typeof(int))
            p.SetValue(c, value, null);
    }

    private void SetEnumPropertyIfExists(Component c, string propName, string enumName)
    {
        PropertyInfo p = c.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
        if (p == null || !p.PropertyType.IsEnum) return;

        try
        {
            object parsed = Enum.Parse(p.PropertyType, enumName);
            p.SetValue(c, parsed, null);
        }
        catch
        {
        }
    }

    private void SetComponentText(Component target, string value)
    {
        if (target == null) return;

        PropertyInfo p = target.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        if (p != null && p.PropertyType == typeof(string))
        {
            p.SetValue(target, value, null);
            return;
        }

        FieldInfo f = target.GetType().GetField("text", BindingFlags.Public | BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(string))
            f.SetValue(target, value);
    }

    private string GetComponentText(Component target)
    {
        if (target == null) return string.Empty;

        PropertyInfo p = target.GetType().GetProperty("text", BindingFlags.Public | BindingFlags.Instance);
        if (p != null && p.PropertyType == typeof(string))
        {
            object value = p.GetValue(target, null);
            return value as string ?? string.Empty;
        }

        FieldInfo f = target.GetType().GetField("text", BindingFlags.Public | BindingFlags.Instance);
        if (f != null && f.FieldType == typeof(string))
        {
            object value = f.GetValue(target);
            return value as string ?? string.Empty;
        }

        return string.Empty;
    }
}
