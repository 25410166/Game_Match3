using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopBehavious : MonoBehaviour
{
    [SerializeField] private Button _btnTabCard;
    [SerializeField] private Button _btnTabGem;
    [SerializeField] private Button _btnTabBag;

    [SerializeField] private GameObject _imgTabCard;
    [SerializeField] private GameObject _imgTabGem;
    [SerializeField] private GameObject _imgTabBag;

    [SerializeField] private GameObject _gem;
    [SerializeField] private GameObject _card;
    [SerializeField] private GameObject _scrollView;

    private ScrollRect _scrollRect;

    void Start()
    {
        _scrollRect = _scrollView.GetComponent<ScrollRect>();

        // Gán sự kiện bấm nút
        _btnTabCard.onClick.AddListener(() => OnTabSelected(_card, _imgTabCard));
        _btnTabGem.onClick.AddListener(() => OnTabSelected(_gem, _imgTabGem));
        _btnTabBag.onClick.AddListener(() => OnTabSelected(null, _imgTabBag));

        // Mặc định chọn tab Card khi mới vào
        OnTabSelected(_card, _imgTabCard);
    }

    private void OnTabSelected(GameObject newContent, GameObject selectedTabImage)
    {
        // Reset toàn bộ tab về false
        _imgTabCard.SetActive(false);
        _imgTabGem.SetActive(false);
        _imgTabBag.SetActive(false);

        // Bật tab đang chọn
        selectedTabImage.SetActive(true);

        // Đổi content trong ScrollRect
        SetScrollContent(newContent);
    }

    private void SetScrollContent(GameObject newContent)
    {
        if (_scrollRect == null)
        {
            Debug.LogWarning("ScrollRect chưa được gán!");
            return;
        }

        // Ẩn content cũ (nếu có)
        if (_scrollRect.content != null)
        {
            _scrollRect.content.gameObject.SetActive(false);
        }

        if (newContent != null)
        {
            RectTransform newRect = newContent.GetComponent<RectTransform>();
            _scrollRect.content = newRect;
            newContent.SetActive(true);
        }
    }
}
