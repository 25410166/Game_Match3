using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [SerializeField] private GameObject[] _element;        // Các nút trên map
    [SerializeField] private GameObject[] _popupContent;   // Các popup tương ứng

    private Vector3 _defaultScale = Vector3.one;           // Scale mặc định
    private Vector3 _selectedScale = new Vector3(1.5f, 1.5f, 1.5f); // Scale khi được chọn

    void Start()
    {
        // Gán sự kiện bấm cho từng element
        for (int i = 0; i < _element.Length; i++)
        {
            int index = i; // cần biến tạm để tránh lỗi delegate closure
            Button btn = _element[i].GetComponent<Button>();

            if (btn != null)
            {
                btn.onClick.AddListener(() => OnElementClicked(index));
            }
            else
            {
                Debug.LogWarning($"{_element[i].name} chưa có Button component!");
            }
        }

        // Thiết lập trạng thái ban đầu
        InitializeDefaultSelection();
    }

    private void InitializeDefaultSelection()
    {
        // Bật popup số 0 và scale to element 0
        for (int i = 0; i < _popupContent.Length; i++)
        {
            _popupContent[i].SetActive(i == 0);
        }

        for (int i = 0; i < _element.Length; i++)
        {
            _element[i].transform.localScale = (i == 0) ? _selectedScale : _defaultScale;
        }
    }

    private void OnElementClicked(int index)
    {
        // Ẩn tất cả popup
        for (int i = 0; i < _popupContent.Length; i++)
        {
            _popupContent[i].SetActive(i == index); // chỉ bật popup trùng index
        }

        // Đưa tất cả về scale bình thường
        for (int i = 0; i < _element.Length; i++)
        {
            _element[i].transform.localScale = _defaultScale;
        }

        // Scale to lên cho element đang chọn
        _element[index].transform.localScale = _selectedScale;
    }
}
