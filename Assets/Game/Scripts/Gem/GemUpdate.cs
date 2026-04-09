using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GemUpdate : MonoBehaviour
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
    [SerializeField] private int currentPetLevel = 1; // Cấp hiện tại của pet (1-10)

    private const int MAX_LEVEL = 10; // Tối đa cấp 10
    private string currentPetId; // Lưu id (chỉ số) dưới dạng string
    private int[] selectedGemLevels = new int[3]; // 3 ô chứa cấp độ đá (1-5)

    private TextMeshProUGUI percentText; // Tham chiếu đến TextMeshProUGUI

    // Start is called before the first frame update
    void Start()
    {
        // Khởi tạo các ô đá
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            selectedGemLevels[i] = 0; // Mặc định 0, chưa chọn đá
        }

        // Lấy tham chiếu đến TextMeshProUGUI
        percentText = _txtPercent.GetComponent<TextMeshProUGUI>();
        if (percentText == null)
        {
            Debug.LogError("TextMeshProUGUI component missing on _txtPercent!");
        }

        // Gán sự kiện click cho các viên đá
        AddClickListeners();
        // Gán sự kiện click để xóa đá từ các ô cell
        AddCellClickListeners();
    }

    // Update is called once per frame
    void Update()
    {
        // Cập nhật hiển thị phần trăm
        UpdatePercentDisplay();
        // Cập nhật hiển thị các ô cell
        UpdateGemCells();
    }

    // Chọn pet với id (chỉ số)
    public void SelectPet(string id)
    {
        currentPetId = id;
        Debug.Log($"SelectPet called with id: {currentPetId}"); // Debug id nhận được
        UpdateGemDisplay();
    }

    // Cập nhật hiển thị các viên đá theo id
    private void UpdateGemDisplay()
    {
        if (gemCollection == null || string.IsNullOrEmpty(currentPetId))
        {
            Debug.LogWarning("gemCollection is null or currentPetId is empty!");
            return;
        }

        // Log nội dung mảng elements để debug
        if (gemCollection.elements != null)
        {
            Debug.Log("Available elements in GemCollection:");
            for (int i = 0; i < gemCollection.elements.Length; i++)
            {
                var elem = gemCollection.elements[i];
                Debug.Log($" - Index: {i}, Element: {elem.element}, GemLevels count: {(elem.gemLevels != null ? elem.gemLevels.Length : 0)}");
            }
        }
        else
        {
            Debug.LogError("gemCollection.elements is null!");
            return;
        }

        // Chuyển id từ string sang int để lấy chỉ số
        if (int.TryParse(currentPetId, out int elementIndex) && elementIndex >= 0 && elementIndex < gemCollection.elements.Length)
        {
            GemCollection.GemElementData elementData = gemCollection.elements[elementIndex];
            Debug.Log($"Found elementData for id {currentPetId}, gemLevels count: {(elementData.gemLevels != null ? elementData.gemLevels.Length : 0)}");
            _gemlvl1.GetComponent<Image>().sprite = elementData.gemLevels[0].sprite;
            _gemlvl2.GetComponent<Image>().sprite = elementData.gemLevels[1].sprite;
            _gemlvl3.GetComponent<Image>().sprite = elementData.gemLevels[2].sprite;
            _gemlvl4.GetComponent<Image>().sprite = elementData.gemLevels[3].sprite;
            _gemlvl5.GetComponent<Image>().sprite = elementData.gemLevels[4].sprite;
            Debug.Log($"Successfully loaded gems for id: {currentPetId}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Không tìm thấy id {currentPetId} trong GemCollection!");
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
        Button button = gemObject.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnGemClick(level));
        }
        else
        {
            // Nếu không có Button, thêm sự kiện click trực tiếp trên Image
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

    // Thêm sự kiện click để xóa đá từ các ô cell
    private void AddCellClickListeners()
    {
        AddCellClickListener(_gemCell1, 0); // Ô 1
        AddCellClickListener(_gemCell2, 1); // Ô 2
        AddCellClickListener(_gemCell3, 2); // Ô 3
    }

    private void AddCellClickListener(GameObject cellObject, int slotIndex)
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

    // Xử lý khi click vào viên đá
    private void OnGemClick(int gemLevel)
    {
        // Kiểm tra các ô cell để thay thế nếu có
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
            // Nếu có ô trống, gán gem mới
            selectedGemLevels[emptySlot] = gemLevel;
            Debug.Log($"Đặt đá cấp {gemLevel} vào ô trống {emptySlot + 1}");
        }
        else
        {
            // Nếu không có ô trống, thay thế ô đầu tiên (hoặc cho phép chọn ô để thay thế)
            selectedGemLevels[0] = gemLevel; // Thay thế ô đầu tiên làm ví dụ
            Debug.Log($"Thay thế đá cấp {gemLevel} vào ô 1 (trước đó có đá)");
        }
        UpdateGemCells(); // Cập nhật hình ảnh ngay sau khi chọn
    }

    // Xử lý khi click vào ô cell để xóa đá
    private void OnCellClick(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < selectedGemLevels.Length)
        {
            selectedGemLevels[slotIndex] = 0; // Đặt về 0 để xóa đá
            Debug.Log($"Xóa đá khỏi ô {slotIndex + 1}");
            UpdateGemCells(); // Cập nhật hình ảnh
            UpdatePercentDisplay(); // Cập nhật tỉ lệ
        }
        else
        {
            Debug.LogWarning($"Invalid slot index: {slotIndex}");
        }
    }

    // Cập nhật hiển thị các ô cell
    private void UpdateGemCells()
    {
        Image[] cellImages = { _gemCell1.GetComponent<Image>(), _gemCell2.GetComponent<Image>(), _gemCell3.GetComponent<Image>() };
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            if (cellImages[i] != null)
            {
                if (selectedGemLevels[i] > 0)
                {
                    // Tìm sprite từ gemCollection dựa trên id và level
                    if (int.TryParse(currentPetId, out int elementIndex) && elementIndex >= 0 && elementIndex < gemCollection.elements.Length)
                    {
                        GemCollection.GemElementData elementData = gemCollection.elements[elementIndex];
                        if (selectedGemLevels[i] - 1 < elementData.gemLevels.Length)
                        {
                            cellImages[i].sprite = elementData.gemLevels[selectedGemLevels[i] - 1].sprite;
                            cellImages[i].enabled = true;
                        }
                        else
                        {
                            Debug.LogWarning($"Cannot find sprite for level {selectedGemLevels[i]} in id {currentPetId}");
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
            // Kiểm tra 3 viên đá
            int gemCount = 0;
            for (int i = 0; i < selectedGemLevels.Length; i++)
            {
                if (selectedGemLevels[i] > 0 && selectedGemLevels[i] <= 5)
                    gemCount++;
            }
            if (gemCount == 3)
            {
                float averageGemLevel = CalculateAverageGemLevel();
                int nextPetLevel = currentPetLevel + 1;
                float successRate = CalculateSuccessRate(averageGemLevel, nextPetLevel);
                percentText.text = $"Tỉ lệ: {successRate:F0}%";
            }
            else
            {
                percentText.text = "Tỉ lệ: -";
            }
        }
    }

    // Hàm nâng cấp pet
    public bool UpgradePet()
    {
        if (currentPetLevel >= MAX_LEVEL)
        {
            Debug.Log("Pet đã đạt cấp tối đa (10)!");
            return false;
        }

        // Kiểm tra 3 viên đá
        int gemCount = 0;
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            if (selectedGemLevels[i] > 0 && selectedGemLevels[i] <= 5)
                gemCount++;
        }
        if (gemCount != 3)
        {
            Debug.LogWarning("Cần chọn đúng 3 viên đá!");
            return false;
        }

        // Tính cấp trung bình của 3 viên đá
        float averageGemLevel = CalculateAverageGemLevel();
        int nextPetLevel = currentPetLevel + 1;

        // Tính tỉ lệ thành công
        float successRate = CalculateSuccessRate(averageGemLevel, nextPetLevel);

        // Random roll
        float roll = Random.Range(0f, 100f);
        if (roll <= successRate)
        {
            currentPetLevel = nextPetLevel;
            Debug.Log($"Nâng cấp thành công! Cấp mới: {currentPetLevel}, Tỉ lệ: {successRate}%, Roll: {roll}%");
            return true;
        }
        else
        {
            Debug.Log($"Nâng cấp thất bại! Cấp hiện tại: {currentPetLevel}, Tỉ lệ: {successRate}%, Roll: {roll}%");
            return false;
        }
    }

    // Tính cấp trung bình của 3 viên đá
    private float CalculateAverageGemLevel()
    {
        float total = 0f;
        int count = 0;
        for (int i = 0; i < selectedGemLevels.Length; i++)
        {
            if (selectedGemLevels[i] > 0)
            {
                total += selectedGemLevels[i];
                count++;
            }
        }
        return count == 3 ? total / 3f : 0f;
    }

    // Tính tỉ lệ thành công
    private float CalculateSuccessRate(float averageGemLevel, int nextPetLevel)
    {
        float baseRate = 100f; // Tỉ lệ cơ bản 100% cho level 1-2

        // Điều chỉnh tỉ lệ dựa trên cấp pet
        if (nextPetLevel > 2)
        {
            // Tính khoảng cách cấp độ (cấp pet tiếp theo - cấp trung bình của đá)
            int levelDiff = nextPetLevel - Mathf.FloorToInt(averageGemLevel);
            baseRate = 100f - (levelDiff * 10f); // Giảm 10% cho mỗi cấp cách biệt

            // Giới hạn tối thiểu 5% khi nâng từ level 9 lên 10 với đá cấp 5
            if (nextPetLevel == 10 && averageGemLevel >= 5)
                baseRate = 5f;
            else
                baseRate = Mathf.Max(5f, baseRate); // Tối thiểu 5%
        }

        return Mathf.Clamp(baseRate, 5f, 100f);
    }

    // Đặt cấp độ cho ô đá (gọi từ UI hoặc script khác)
    public void SetGemLevel(int slotIndex, int level)
    {
        if (slotIndex >= 0 && slotIndex < selectedGemLevels.Length && level >= 0 && level <= 5)
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

    // Lấy cấp hiện tại của pet
    public int GetCurrentPetLevel()
    {
        return currentPetLevel;
    }
}