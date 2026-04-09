using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PageBehavious : MonoBehaviour
{
    [SerializeField] private GameObject _bg;
    [SerializeField] private GameObject _page1;
    [SerializeField] private GameObject _page2;
    [SerializeField] private GameObject _page3;
    [SerializeField] private GameObject _page4;
    [SerializeField] private GameObject _changePet;
    [SerializeField] private GameObject _btnNext; // Sử dụng GameObject với Sprite
    [SerializeField] private GameObject _btnPre;  // Sử dụng GameObject với Sprite

    private int currentPage = 0; // Theo dõi trang hiện tại (0: page1, 1: page2, 2: page3, 3: page4)

    // Start is called before the first frame update
    void Start()
    {
        // Ẩn tất cả khi khởi động
        HideAllPages();

        // Đảm bảo các GameObject có Collider2D
        AddCollidersIfNeeded();
    }

    // Ẩn tất cả các trang và nút
    private void HideAllPages()
    {
        _bg.SetActive(false);
        _page1.SetActive(false);
        _page2.SetActive(false);
        _page3.SetActive(false);
        _page4.SetActive(false);
        _btnNext.SetActive(false);
        _btnPre.SetActive(false);
    }

    // Thêm Collider2D nếu chưa có
    private void AddCollidersIfNeeded()
    {
        AddColliderIfNeeded(_changePet);
        AddColliderIfNeeded(_btnNext);
        AddColliderIfNeeded(_btnPre);
    }

    private void AddColliderIfNeeded(GameObject obj)
    {
        if (obj != null && obj.GetComponent<Collider2D>() == null)
        {
            obj.AddComponent<BoxCollider2D>();
            Debug.Log("Added BoxCollider2D to " + obj.name);
        }
        else if (obj != null)
        {
            Collider2D collider = obj.GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.enabled = true;
                Debug.Log("Enabled Collider2D on " + obj.name);
            }
        }
    }

    // Cập nhật mỗi frame để kiểm tra click
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Phát hiện click chuột trái
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                GameObject clickedObject = hit.collider.gameObject;
                Debug.Log("Clicked on: " + clickedObject.name + " at currentPage: " + currentPage);

                if (clickedObject == _changePet)
                {
                    OnChangePetClick();
                }
                else if (clickedObject == _btnNext)
                {
                    OnNextClick();
                }
                else if (clickedObject == _btnPre)
                {
                    OnPreClick();
                }
            }
            else
            {
                Debug.Log("No collider hit at: " + Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }
        }
    }

    // Xử lý khi nhấn _changePet
    private void OnChangePetClick()
    {
        HideAllPages();
        _bg.SetActive(true);
        _page1.SetActive(true);
        currentPage = 0;
        _btnNext.SetActive(true);
        _btnPre.SetActive(false); // Ẩn nút Previous ở page 1
        Debug.Log("OnChangePetClick called, currentPage set to: " + currentPage);
    }

    // Xử lý khi nhấn _btnNext
    private void OnNextClick()
    {
        Debug.Log("OnNextClick called, currentPage before: " + currentPage);
        if (currentPage < 3) // Chỉ cho phép chuyển đến page4 (currentPage = 3)
        {
            // Ẩn trang hiện tại
            switch (currentPage)
            {
                case 0: _page1.SetActive(false); break;
                case 1: _page2.SetActive(false); break;
                case 2: _page3.SetActive(false); break;
                case 3: _page4.SetActive(false); break;
            }

            // Tăng currentPage
            currentPage++;
            Debug.Log("OnNextClick, currentPage after increment: " + currentPage);

            // Di chuyển và hiển thị trang tiếp theo
            switch (currentPage)
            {
                case 1:
                    _page2.transform.localPosition = Vector3.zero; // Đặt về vị trí (0, 0, 0)
                    _page2.SetActive(true);
                    _btnPre.SetActive(true); // Hiển thị nút Previous
                    break;
                case 2:
                    _page3.transform.localPosition = Vector3.zero; // Đặt về vị trí (0, 0, 0)
                    _page3.SetActive(true);
                    break;
                case 3:
                    _page4.transform.localPosition = Vector3.zero; // Đặt về vị trí (0, 0, 0)
                    _page4.SetActive(true);
                    _btnNext.SetActive(false); // Ẩn nút Next ở page 4
                    break;
            }
        }
        else
        {
            Debug.Log("OnNextClick skipped, currentPage >= 3");
        }
    }

    // Xử lý khi nhấn _btnPre
    private void OnPreClick()
    {
        Debug.Log("OnPreClick called, currentPage before: " + currentPage);
        if (currentPage > 0) // Chỉ cho phép quay lại page1
        {
            // Ẩn trang hiện tại
            switch (currentPage)
            {
                case 1: _page2.SetActive(false); break;
                case 2: _page3.SetActive(false); break;
                case 3: _page4.SetActive(false); break;
            }

            // Giảm currentPage
            currentPage--;
            Debug.Log("OnPreClick, currentPage after decrement: " + currentPage);

            // Di chuyển và hiển thị trang trước
            switch (currentPage)
            {
                case 0:
                    _page1.transform.localPosition = Vector3.zero; // Đặt về vị trí (0, 0, 0)
                    _page1.SetActive(true);
                    _btnPre.SetActive(false); // Ẩn nút Previous ở page 1
                    _btnNext.SetActive(true); // Hiển thị nút Next
                    break;
                case 1:
                    _page2.transform.localPosition = Vector3.zero; // Đặt về vị trí (0, 0, 0)
                    _page2.SetActive(true);
                    _btnNext.SetActive(true); // Hiển thị nút Next
                    break;
                case 2:
                    _page3.transform.localPosition = Vector3.zero; // Đặt về vị trí (0, 0, 0)
                    _page3.SetActive(true);
                    _btnNext.SetActive(true); // Hiển thị nút Next
                    break;
            }
        }
        else
        {
            Debug.Log("OnPreClick skipped, currentPage <= 0");
        }
    }
}