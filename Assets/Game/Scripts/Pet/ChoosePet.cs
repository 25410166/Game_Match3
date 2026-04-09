using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoosePet : MonoBehaviour
{
    [Header("UI - Cấp hiện tại")]
    [SerializeField] private TextMeshProUGUI _txtLevel;
    [SerializeField] private TextMeshProUGUI _txtHP;
    [SerializeField] private TextMeshProUGUI _txtAtk;
    [SerializeField] private TextMeshProUGUI _txtMana;
    [SerializeField] private TextMeshProUGUI _txtCrit;

    [Header("UI - Cấp kế tiếp")]
    [SerializeField] private TextMeshProUGUI _txtLevelNext;
    [SerializeField] private TextMeshProUGUI _txtHPNext;
    [SerializeField] private TextMeshProUGUI _txtAtkNext;
    [SerializeField] private TextMeshProUGUI _txtManaNext;
    [SerializeField] private TextMeshProUGUI _txtCritNext;

    [Header("UI - Thông tin Pet")]
    [SerializeField] private TextMeshProUGUI _txtPetName;
    [SerializeField] private Image _elementIcon;
    [SerializeField] private Sprite[] _elementSprite; // Mảng sprite cho các element (7 hệ)

    [Header("Vị trí spawn Pet")]
    [SerializeField] private Transform _petSpawnPoint;
    [SerializeField] private GameObject _pagePet;

    [Header("Nút Nâng Cấp")]
    [SerializeField] private Button _upgradeButton; // Thêm Button cho nâng cấp

    [Header("Tham chiếu đến GemUpdate")]
    [SerializeField] private GemUpdate gemUpdate; // Tham chiếu đến GemUpdate để gọi SelectPet

    private GameObject currentPetInstance;
    private PetStatsHolder currentStats;
    private int currentLevel = 1;

    // Start is called before the first frame update
    void Start()
    {
        // Gán sự kiện cho nút nâng cấp
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.AddListener(OnUpgradeButtonClick);
            _upgradeButton.gameObject.SetActive(false); // Ẩn nút lúc đầu
        }
        else
        {
            Debug.LogError("❌ _upgradeButton không được gán trong Inspector!");
        }
    }

    // --- Gọi khi click vào 1 pet ---
    public void ShowPet(GameObject petPrefab)
    {
        _pagePet.SetActive(false);
        if (petPrefab == null)
        {
            Debug.LogError("❌ PetPrefab bị null!");
            return;
        }

        // Xóa pet cũ nếu có
        if (currentPetInstance != null)
            Destroy(currentPetInstance);

        // Spawn pet mới
        currentPetInstance = Instantiate(petPrefab, _petSpawnPoint.position, Quaternion.identity, _petSpawnPoint);
        currentPetInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0); // Đặt scale về (0.5, 0.5, 0)
        currentStats = currentPetInstance.GetComponent<PetStatsHolder>();

        if (currentStats == null)
        {
            Debug.LogError($"❌ Prefab {petPrefab.name} không có PetStatsHolder!");
            return;
        }

        currentLevel = 1;
        UpdatePetUI(currentStats.petName); // Sử dụng petName từ PetStatsHolder

        // Gọi hàm SelectPet trong GemUpdate với id (chỉ số từ _elementSprite)
        if (gemUpdate != null && currentStats != null)
        {
            int id = GetElementIndexFromPetName(currentStats.petName); // Lấy id từ tên
            gemUpdate.SelectPet(id.ToString()); // Gửi id dưới dạng string
        }
        else
        {
            Debug.LogError("❌ GemUpdate hoặc currentStats không được gán!");
        }

        if (_upgradeButton != null)
            _upgradeButton.gameObject.SetActive(true); // Hiển thị nút nâng cấp khi có pet
    }

    private void UpdatePetUI(string petNameFromStats)
    {
        if (currentStats == null || currentStats.levels.Count == 0 || _elementSprite == null || _elementSprite.Length == 0)
        {
            Debug.LogWarning("⚠️ Không có dữ liệu PetStatsHolder hoặc _elementSprite chưa được gán!");
            return;
        }

        int index = Mathf.Clamp(currentLevel - 1, 0, currentStats.levels.Count - 1);
        PetLevelData data = currentStats.levels[index];

        // --- Lấy Pet Name trực tiếp từ PetStatsHolder ---
        _txtPetName.text = petNameFromStats ?? "Unknown"; // Sử dụng petName từ ScriptableObject
        Debug.Log("Pet Name from Stats: " + petNameFromStats); // Debug để kiểm tra

        // --- Lấy element từ petName ---
        string rawElement = petNameFromStats;
        Debug.Log("Raw element before processing: " + rawElement); // Debug tên gốc
        int firstNumberIndex = -1;
        for (int i = 0; i < rawElement.Length; i++)
        {
            if (char.IsDigit(rawElement[i]))
            {
                firstNumberIndex = i;
                break;
            }
        }
        if (firstNumberIndex >= 0)
            rawElement = rawElement.Substring(0, firstNumberIndex);
        Debug.Log("Processed element: " + rawElement); // Debug element sau xử lý

        // So sánh với mảng elements để lấy chỉ số
        int elementIndex = GetElementIndexFromPetName(petNameFromStats);
        if (elementIndex >= 0 && elementIndex < _elementSprite.Length)
            _elementIcon.sprite = _elementSprite[elementIndex];
        else
            Debug.LogWarning($"⚠️ Không tìm thấy element phù hợp: {rawElement}");

        // --- Cấp hiện tại ---
        _txtLevel.text = $"Lv. {data.level}";
        _txtHP.text = data.baseHP.ToString();
        _txtAtk.text = data.baseRage.ToString();
        _txtMana.text = data.baseMana.ToString();
        _txtCrit.text = $"{data.critRate}%";

        // --- Cấp kế tiếp ---
        if (index + 1 < currentStats.levels.Count)
        {
            PetLevelData next = currentStats.levels[index + 1];
            _txtLevelNext.text = $"Lv. {next.level}";
            _txtHPNext.text = next.baseHP.ToString();
            _txtAtkNext.text = next.baseRage.ToString();
            _txtManaNext.text = next.baseMana.ToString();
            _txtCritNext.text = $"{next.critRate}%";
        }
        else
        {
            _txtLevelNext.text = "Max";
            _txtHPNext.text = "-";
            _txtAtkNext.text = "-";
            _txtManaNext.text = "-";
            _txtCritNext.text = "-";
            if (_upgradeButton != null)
                _upgradeButton.gameObject.SetActive(false); // Ẩn nút khi đạt max level
        }

        Debug.Log($"✅ Hiển thị pet: {petNameFromStats} | Hệ: {rawElement} | Cấp {data.level}");
    }

    // Xử lý khi nhấn nút nâng cấp
    private void OnUpgradeButtonClick()
    {
        ChangeLevel(1); // Tăng cấp lên 1
    }

    public void ChangeLevel(int delta)
    {
        if (currentStats == null) return;
        currentLevel = Mathf.Clamp(currentLevel + delta, 1, currentStats.levels.Count);
        UpdatePetUI(currentStats.petName); // Cập nhật với petName từ PetStatsHolder
    }

    // Hàm lấy chỉ số element từ petName
    private int GetElementIndexFromPetName(string petName)
    {
        if (string.IsNullOrEmpty(petName)) return -1;

        string rawElement = petName;
        int firstNumberIndex = -1;
        for (int i = 0; i < rawElement.Length; i++)
        {
            if (char.IsDigit(rawElement[i]))
            {
                firstNumberIndex = i;
                break;
            }
        }
        if (firstNumberIndex >= 0)
            rawElement = rawElement.Substring(0, firstNumberIndex);

        string[] elements = { "Dark", "Earth", "Fire", "Light", "Metal", "Water", "Wood" };
        for (int i = 0; i < elements.Length; i++)
        {
            if (rawElement == elements[i])
            {
                return i; // Trả về chỉ số (0-6) làm id
            }
        }
        return -1; // Không tìm thấy
    }

}