using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrebattleGuardianItemButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI txtName;
    [SerializeField] private GameObject selectedTick;

    private int guardianId = -1;
    private Action<int> onSelect;

    public int GuardianId => guardianId;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (AudioManager.Instance != null)
            AudioManager.Instance.RegisterButtonClick(button);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    public void Setup(int inGuardianId, string displayName, Sprite avatarIcon, bool isSelected, Action<int> onSelected)
    {
        guardianId = inGuardianId;
        onSelect = onSelected;

        if (txtName != null)
            txtName.text = displayName ?? string.Empty;

        if (icon != null)
        {
            icon.sprite = avatarIcon;
            icon.enabled = avatarIcon != null;
        }

        SetSelected(isSelected);

        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
            if (AudioManager.Instance != null)
                AudioManager.Instance.RegisterButtonClick(button);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedTick != null)
            selectedTick.SetActive(isSelected);
    }

    private void OnClick()
    {
        if (guardianId >= 0 && onSelect != null)
            onSelect.Invoke(guardianId);
    }
}
