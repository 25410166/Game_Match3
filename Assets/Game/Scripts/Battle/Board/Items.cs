using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class Items : MonoBehaviour, IPointerClickHandler
{
    private Image imageComponent;
    [Header("Selection FX")]
    [SerializeField] private GameObject selectFxPrefab;
    [SerializeField] private Transform selectFxAnchor;
    [SerializeField] private bool keepFxVisibleWhileSelected = true;

    private GameObject selectFxInstance;
    private Coroutine selectFxHideCoroutine;

    public int itemId;
    public int gemValue = 1;
    public int column;
    public int row;

    [Header("Value UI")]
    [SerializeField] private TextMeshProUGUI txtGemValue;

    void Awake()
    {
        imageComponent = GetComponent<Image>();
        EnsureSelectFxInstance();
        HideSelectFx();
        if (imageComponent != null)
            imageComponent.color = Color.white;
    }

    private void OnEnable()
    {
        if (imageComponent == null)
            imageComponent = GetComponent<Image>();

        if (imageComponent != null)
            imageComponent.color = Color.white;

        HideSelectFx();
    }

    public void SetItem(int id, Sprite sprite, int value = 1)
    {
        itemId = id;
        gemValue = Mathf.Max(1, value);
        if (imageComponent == null) imageComponent = GetComponent<Image>();
        if (imageComponent != null) imageComponent.sprite = sprite;
        RefreshGemValueText();
    }

    public void SetGemValue(int value)
    {
        gemValue = Mathf.Max(1, value);
        RefreshGemValueText();
    }

    private void RefreshGemValueText()
    {
        if (txtGemValue == null)
            txtGemValue = GetComponentInChildren<TextMeshProUGUI>(true);

        if (txtGemValue == null)
            return;

        bool show = gemValue > 1;
        txtGemValue.gameObject.SetActive(show);
        if (show)
            txtGemValue.text = "x" + gemValue;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance == null || !GameManager.Instance.CanPlayerBoardInteract()) return;

        Board board = Board.Instance;
        if (board != null)
        {
            Items previousItem = board.GetSelectedItem();
            if (previousItem != null && previousItem != this)
            {
                GameManager.Instance.StartPlayerInteraction();
                board.SwapItems(this);
            }
            else
            {
                board.SetSelectedItem(this);
            }
        }
    }

    public void SetSelectedVisual(bool selected)
    {
        if (imageComponent == null)
            imageComponent = GetComponent<Image>();

        if (imageComponent != null)
            imageComponent.color = selected ? Color.yellow : Color.white;

        if (selected)
            PlaySelectFx();
        else
            HideSelectFx();
    }

    public void ShowTutorialHighlight()
    {
        PlaySelectFx();
    }

    public void HideTutorialHighlight()
    {
        HideSelectFx();
    }

    private void EnsureSelectFxInstance()
    {
        if (selectFxInstance != null)
            return;

        if (selectFxPrefab == null)
            return;

        if (selectFxPrefab.scene.IsValid())
        {
            selectFxInstance = selectFxPrefab;
            selectFxInstance.SetActive(false);
            return;
        }

        Transform parent = selectFxAnchor != null ? selectFxAnchor : transform;
        selectFxInstance = Instantiate(selectFxPrefab, parent);
        selectFxInstance.transform.localPosition = Vector3.zero;
        selectFxInstance.transform.localRotation = Quaternion.identity;
        selectFxInstance.transform.localScale = Vector3.one;
        selectFxInstance.SetActive(false);
    }

    private void PlaySelectFx()
    {
        EnsureSelectFxInstance();
        if (selectFxInstance == null)
            return;

        if (selectFxHideCoroutine != null)
        {
            StopCoroutine(selectFxHideCoroutine);
            selectFxHideCoroutine = null;
        }

        selectFxInstance.SetActive(true);

        ParticleSystem[] systems = selectFxInstance.GetComponentsInChildren<ParticleSystem>(true);
        float maxDuration = 0f;
        for (int i = 0; i < systems.Length; i++)
        {
            ParticleSystem ps = systems[i];
            if (ps == null)
                continue;

            ps.Clear(true);
            ps.Play(true);

            ParticleSystem.MainModule main = ps.main;
            float duration = main.duration + main.startLifetime.constantMax;
            maxDuration = Mathf.Max(maxDuration, duration);
        }

        if (!keepFxVisibleWhileSelected && maxDuration > 0f)
            selectFxHideCoroutine = StartCoroutine(HideFxAfter(maxDuration));
    }

    private IEnumerator HideFxAfter(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0.05f, delay));
        HideSelectFx();
    }

    private void HideSelectFx()
    {
        if (selectFxHideCoroutine != null)
        {
            StopCoroutine(selectFxHideCoroutine);
            selectFxHideCoroutine = null;
        }

        if (selectFxInstance != null)
            selectFxInstance.SetActive(false);
    }

    public RectTransform GetRectTransform()
    {
        return GetComponent<RectTransform>();
    }
}
