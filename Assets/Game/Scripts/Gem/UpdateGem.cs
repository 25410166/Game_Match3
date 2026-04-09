using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpdateGem : MonoBehaviour
{
    [SerializeField] private GameObject _gemlvl1;
    [SerializeField] private GameObject _gemlvl2;
    [SerializeField] private GameObject _gemlvl3;
    [SerializeField] private GameObject _gemlvl4;
    [SerializeField] private GameObject _gemlvl5;
    [SerializeField] private GameObject _gemCell1;
    [SerializeField] private GameObject _gemCell2;
    [SerializeField] private GameObject _gemCell3;
    [SerializeField] private GameObject _txtPercent;
    [SerializeField] private GemCollection gemCollection; // Tham chiếu đến GemCollection
    [SerializeField] private GameObject _popupGem; // Popup Gem
    [SerializeField] private GameObject _gemUpdate; // Hiển thị gem level mới
    [SerializeField] private Button _gemUpdateButton; // Button để gọi hàm nâng cấp

    private const int MAX_GEM_LEVEL = 5; // Tối đa cấp đá là 5
    private string currentElementId; // Lưu id (chỉ số) của hệ (0-6)
    private int[] selectedGemLevels = new int[3]; // 3 ô chứa cấp độ đá (1-5)

    private TextMeshProUGUI percentText; // Tham chiếu đến TextMeshProUGUI

    // Danh sách button cho các hệ
    [SerializeField] private Button[] elementButtons = new Button[7]; // 7 button cho 7 hệ

    // Start is called before the first frame update
    void Start()
    {
        _popupGem.SetActive(false);
        // Khởi tạo các ô đá
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            selectedGemLevels[i] = 0; // Mặc định 0, chưa chọn đá
        }

        // Lấy tham chiếu đến TextMeshProUGUI
        percentText = _txtPercent?.GetComponent<TextMeshProUGUI>();
        if (percentText == null && _txtPercent != null)
        {
            Debug.LogError("TextMeshProUGUI component missing on _txtPercent!");
        }
        else if (_txtPercent == null)
        {
            Debug.LogWarning("_txtPercent is not assigned!");
        }

        // Gán sự kiện click cho các viên đá
        AddClickListeners();
        // Gán sự kiện click để xóa đá từ các ô cell
        AddCellClickListeners();
        // Gán sự kiện click cho các button hệ
        AddElementButtonListeners();
        // Gán sự kiện click cho button nâng cấp
        if (_gemUpdateButton != null)
        {
            _gemUpdateButton.onClick.RemoveAllListeners();
            _gemUpdateButton.onClick.AddListener(() => { UpgradeGem(); });
            Debug.Log("Upgrade button event assigned.");
        }
        else
        {
            Debug.LogWarning("_gemUpdateButton is not assigned!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Cập nhật hiển thị phần trăm
        UpdatePercentDisplay();
        // Cập nhật hiển thị các ô cell
        UpdateGemCells();
    }

    // Chọn hệ với id (chỉ số 0-6)
    public void SelectElement(string id)
    {
        currentElementId = id;
        Debug.Log($"SelectElement called with id: {currentElementId}"); // Debug id nhận được
        if (gemCollection == null || string.IsNullOrEmpty(currentElementId) || !int.TryParse(currentElementId, out int elementIndex) || elementIndex < 0 || elementIndex >= (gemCollection?.elements?.Length ?? 0))
        {
            Debug.LogWarning($"Invalid element selection for id: {currentElementId}");
            if (_popupGem != null)
            {
                _popupGem.SetActive(false); // Tắt popup nếu không thành công
            }
            return;
        }
        UpdateGemDisplay();
    }

    // Cập nhật hiển thị các viên đá theo id hệ
    private void UpdateGemDisplay()
    {
        if (gemCollection == null || string.IsNullOrEmpty(currentElementId))
        {
            Debug.LogWarning("gemCollection is null or currentElementId is empty!");
            if (_popupGem != null)
            {
                _popupGem.SetActive(false); // Tắt popup nếu không thành công
            }
            return;
        }

        if (gemCollection.elements == null)
        {
            Debug.LogError("gemCollection.elements is null!");
            if (_popupGem != null)
            {
                _popupGem.SetActive(false); // Tắt popup nếu không thành công
            }
            return;
        }

        if (int.TryParse(currentElementId, out int elementIndex) && elementIndex >= 0 && elementIndex < gemCollection.elements.Length)
        {
            GemCollection.GemElementData elementData = gemCollection.elements[elementIndex];
            SetGemSprite(_gemlvl1, 0, elementData);
            SetGemSprite(_gemlvl2, 1, elementData);
            SetGemSprite(_gemlvl3, 2, elementData);
            SetGemSprite(_gemlvl4, 3, elementData);
            SetGemSprite(_gemlvl5, 4, elementData);
            if (_popupGem != null) _popupGem.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"⚠️ Không tìm thấy id {currentElementId} trong GemCollection!");
            if (_popupGem != null)
            {
                _popupGem.SetActive(false); // Tắt popup nếu không thành công
            }
        }
    }

    private void SetGemSprite(GameObject gemObject, int levelIndex, GemCollection.GemElementData elementData)
    {
        if (gemObject != null)
        {
            Image image = gemObject.GetComponent<Image>();
            if (image != null && elementData.gemLevels != null && levelIndex < elementData.gemLevels.Length)
            {
                image.sprite = elementData.gemLevels[levelIndex].sprite;
            }
            else
            {
                Debug.LogWarning($"Image or sprite missing for {gemObject.name} at level {levelIndex + 1}");
            }
        }
    }

    // Thêm sự kiện click cho các viên đá
    private void AddClickListeners()
    {
        AddClickListener(_gemlvl1, 1);
        AddClickListener(_gemlvl2, 2);
        AddClickListener(_gemlvl3, 3);
        AddClickListener(_gemlvl4, 4);
        AddClickListener(_gemlvl5, 5);
    }

    private void AddClickListener(GameObject gemObject, int level)
    {
        if (gemObject != null)
        {
            Button button = gemObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnGemClick(level));
            }
            else
            {
                Image image = gemObject.GetComponent<Image>();
                if (image != null)
                {
                    UnityEngine.EventSystems.EventTrigger trigger = gemObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                    if (trigger == null)
                    {
                        trigger = gemObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    }
                    UnityEngine.EventSystems.EventTrigger.Entry entry = new UnityEngine.EventSystems.EventTrigger.Entry();
                    entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
                    entry.callback.AddListener((data) => { OnGemClick(level); });
                    trigger.triggers.Add(entry);
                }
                else
                {
                    Debug.LogWarning($"Button or Image component missing on {gemObject.name}. Add an Image or Button component to enable clicking.");
                }
            }
        }
    }

    // Thêm sự kiện click để xóa đá từ các ô cell
    private void AddCellClickListeners()
    {
        AddCellClickListener(_gemCell1, 0); // Ô 1
        AddCellClickListener(_gemCell2, 1); // Ô 2
        AddCellClickListener(_gemCell3, 2); // Ô 3
    }

    private void AddCellClickListener(GameObject cellObject, int slotIndex)
    {
        if (cellObject != null)
        {
            Button button = cellObject.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnCellClick(slotIndex));
            }
            else
            {
                Image image = cellObject.GetComponent<Image>();
                if (image != null)
                {
                    UnityEngine.EventSystems.EventTrigger trigger = cellObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                    if (trigger == null)
                    {
                        trigger = cellObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                    }
                    UnityEngine.EventSystems.EventTrigger.Entry entry = new UnityEngine.EventSystems.EventTrigger.Entry();
                    entry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerClick;
                    entry.callback.AddListener((data) => { OnCellClick(slotIndex); });
                    trigger.triggers.Add(entry);
                }
                else
                {
                    Debug.LogWarning($"Button or Image component missing on {cellObject.name}. Add a Button or Image component to enable clicking.");
                }
            }
        }
    }

    // Thêm sự kiện click cho các button hệ
    private void AddElementButtonListeners()
    {
        string[] elements = { "Dark", "Earth", "Fire", "Light", "Metal", "Water", "Wood" };
        for (int i = 0; i < elementButtons.Length; i++)
        {
            if (elementButtons[i] != null)
            {
                int index = i; // Capture index for the lambda
                elementButtons[i].onClick.RemoveAllListeners();
                elementButtons[i].onClick.AddListener(() => SelectElement(index.ToString()));
                // Gán text cho button (nếu có Text component)
                TextMeshProUGUI buttonText = elementButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = elements[i];
                }
            }
            else
            {
                Debug.LogWarning($"Element button at index {i} is not assigned!");
            }
        }
    }

    // Xử lý khi click vào viên đá
    private void OnGemClick(int gemLevel)
    {
        int emptySlot = -1;
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            if (selectedGemLevels[i] == 0)
            {
                emptySlot = i;
                break;
            }
        }

        if (emptySlot >= 0)
        {
            selectedGemLevels[emptySlot] = gemLevel;
            Debug.Log($"Đặt đá cấp {gemLevel} vào ô trống {emptySlot + 1}");
        }
        else
        {
            selectedGemLevels[0] = gemLevel; // Thay thế ô đầu tiên
            Debug.Log($"Thay thế đá cấp {gemLevel} vào ô 1 (trước đó có đá)");
        }

        // Gán _gemUpdate về null khi chọn gem mới
        if (_gemUpdate != null)
        {
            Image updateImage = _gemUpdate.GetComponent<Image>();
            if (updateImage != null)
            {
                updateImage.sprite = null;
                _gemUpdate.SetActive(false);
            }
        }

        UpdateGemCells();
        UpdatePercentDisplay();
    }

    // Xử lý khi click vào ô cell để xóa đá
    private void OnCellClick(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < selectedGemLevels.Length)
        {
            selectedGemLevels[slotIndex] = 0; // Xóa đá
            Debug.Log($"Xóa đá khỏi ô {slotIndex + 1}");
            UpdateGemCells();
            UpdatePercentDisplay();
        }
        else
        {
            Debug.LogWarning($"Invalid slot index: {slotIndex}");
        }
    }

    // Cập nhật hiển thị các ô cell
    private void UpdateGemCells()
    {
        Image[] cellImages = { _gemCell1?.GetComponent<Image>(), _gemCell2?.GetComponent<Image>(), _gemCell3?.GetComponent<Image>() };
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            if (cellImages[i] != null)
            {
                if (selectedGemLevels[i] > 0)
                {
                    if (int.TryParse(currentElementId, out int elementIndex) && elementIndex >= 0 && elementIndex < (gemCollection?.elements?.Length ?? 0))
                    {
                        GemCollection.GemElementData elementData = gemCollection.elements[elementIndex];
                        if (selectedGemLevels[i] - 1 < (elementData.gemLevels?.Length ?? 0))
                        {
                            cellImages[i].sprite = elementData.gemLevels[selectedGemLevels[i] - 1].sprite;
                            cellImages[i].enabled = true;
                        }
                        else
                        {
                            Debug.LogWarning($"Cannot find sprite for level {selectedGemLevels[i]} in id {currentElementId}");
                        }
                    }
                }
                else
                {
                    cellImages[i].sprite = null;
                    cellImages[i].enabled = false;
                }
            }
        }
    }

    // Cập nhật hiển thị phần trăm
    private void UpdatePercentDisplay()
    {
        if (percentText != null)
        {
            int gemCount = 0;
            int sameLevel = -1;
            for (int i = 0; i < selectedGemLevels.Length; i++)
            {
                if (selectedGemLevels[i] > 0 && selectedGemLevels[i] <= MAX_GEM_LEVEL)
                {
                    gemCount++;
                    if (sameLevel == -1) sameLevel = selectedGemLevels[i];
                    else if (sameLevel != selectedGemLevels[i]) sameLevel = -1; // Không cùng cấp
                }
            }

            if (gemCount == 3 && sameLevel != -1)
            {
                float successRate = CalculateSuccessRate(sameLevel);
                percentText.text = $"Tỉ lệ: {successRate:F0}%";
            }
            else
            {
                percentText.text = "Tỉ lệ: -";
            }
        }
    }

    // Hàm nâng cấp đá
    public bool UpgradeGem()
    {
        int gemCount = 0;
        int sameLevel = -1;
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            if (selectedGemLevels[i] > 0 && selectedGemLevels[i] <= MAX_GEM_LEVEL)
            {
                gemCount++;
                if (sameLevel == -1)
                    sameLevel = selectedGemLevels[i];
                else if (sameLevel != selectedGemLevels[i])
                    return false; // Cần 3 viên cùng cấp
            }
        }

        if (gemCount != 3 || sameLevel == -1)
        {
            Debug.LogWarning("Cần chọn đúng 3 viên đá cùng cấp!");
            return false;
        }

        float successRate = CalculateSuccessRate(sameLevel);
        float roll = Random.Range(0f, 100f);
        if (roll <= successRate)
        {
            int newGemLevel = Mathf.Min(sameLevel + 1, MAX_GEM_LEVEL); // Tăng 1 cấp, tối đa 5
            // Xóa sprite và tắt hiển thị của các ô cell
            Image[] cellImages = { _gemCell1?.GetComponent<Image>(), _gemCell2?.GetComponent<Image>(), _gemCell3?.GetComponent<Image>() };
            for (int i = 0; i < cellImages.Length; i++)
            {
                if (cellImages[i] != null)
                {
                    cellImages[i].sprite = null;
                    cellImages[i].enabled = false;
                }
            }
            // Xóa 3 viên cũ trong mảng
            for (int i = 0; i < selectedGemLevels.Length; i++)
            {
                selectedGemLevels[i] = 0;
            }

            // Hiển thị ảnh gem level mới trên _gemUpdate
            if (_gemUpdate != null && int.TryParse(currentElementId, out int elementIndex) && elementIndex >= 0 && elementIndex < (gemCollection?.elements?.Length ?? 0))
            {
                GemCollection.GemElementData elementData = gemCollection.elements[elementIndex];
                if (elementData.gemLevels != null && newGemLevel - 1 < elementData.gemLevels.Length)
                {
                    Image updateImage = _gemUpdate.GetComponent<Image>();
                    if (updateImage != null)
                    {
                        updateImage.sprite = elementData.gemLevels[newGemLevel - 1].sprite;
                        _gemUpdate.SetActive(true); // Hiển thị _gemUpdate
                    }
                    else
                    {
                        Debug.LogWarning("_gemUpdate missing Image component!");
                    }
                }
                else
                {
                    Debug.LogWarning($"Cannot find sprite for new level {newGemLevel} in id {currentElementId}");
                }
            }

            // Cập nhật text thành "Thành công"
            if (percentText != null)
            {
                percentText.text = "Thành công";
            }

            Debug.Log($"Nâng cấp thành công! Đá cấp {sameLevel} → Đá cấp {newGemLevel}, Tỉ lệ: {successRate}%, Roll: {roll}%");
            return true;
        }
        else
        {
            Debug.Log($"Nâng cấp thất bại! Đá cấp {sameLevel}, Tỉ lệ: {successRate}%, Roll: {roll}%");
            // Cập nhật text thành "Thất bại"
            if (percentText != null)
            {
                percentText.text = "Thất bại";
            }
            return false;
        }
    }

    // Tính tỷ lệ thành công cho nâng cấp đá
    private float CalculateSuccessRate(int currentLevel)
    {
        if (currentLevel == 4) // 3 viên cấp 4 lên cấp 5
        {
            return 80f; // Tỷ lệ 80%
        }
        else if (currentLevel < 4) // 3 viên cánh 1, 2, 3 lên cấp tiếp theo
        {
            return 100f; // Tỷ lệ 100%
        }
        return 10f; // Mặc định tối thiểu 10% (dự phòng)
    }

    // Đặt cấp độ cho ô đá (gọi từ UI hoặc script khác)
    public void SetGemLevel(int slotIndex, int level)
    {
        if (slotIndex >= 0 && slotIndex < selectedGemLevels.Length && level >= 0 && level <= MAX_GEM_LEVEL)
        {
            selectedGemLevels[slotIndex] = level;
            Debug.Log($"Đặt đá cấp {level} vào ô {slotIndex + 1}");
            UpdateGemCells();
            UpdatePercentDisplay();
        }
        else
        {
            Debug.LogWarning("Ô hoặc cấp độ không hợp lệ!");
        }
    }
}