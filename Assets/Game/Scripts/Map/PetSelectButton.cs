using UnityEngine;

public class PetPopupManager : MonoBehaviour
{
    [Header("Popup Setup")]
    [SerializeField] private GameObject popupSetup;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private GameObject[] allPetPrefabs;

    [Header("UI Ẩn khi mở Popup")]
    [SerializeField] private GameObject uiToHide;

    private GameObject currentPet;

    public void ShowPetById(int petId)
    {
        if (allPetPrefabs == null || allPetPrefabs.Length == 0)
        {
            Debug.LogError("❌ Chưa gán danh sách allPetPrefabs!");
            return;
        }

        if (petId < 0 || petId >= allPetPrefabs.Length)
        {
            Debug.LogWarning($"⚠️ ID {petId} không hợp lệ!");
            return;
        }

        GameObject petPrefab = allPetPrefabs[petId];
        if (petPrefab == null)
        {
            Debug.LogWarning($"⚠️ Prefab của pet ID {petId} bị null!");
            return;
        }

        if (uiToHide != null) uiToHide.SetActive(false);
        popupSetup.SetActive(true);

        if (currentPet != null) Destroy(currentPet);

        Quaternion flippedRotation = Quaternion.Euler(
            spawnPoint.rotation.eulerAngles.x,
            spawnPoint.rotation.eulerAngles.y + 180f,
            spawnPoint.rotation.eulerAngles.z
        );

        currentPet = Instantiate(petPrefab, spawnPoint.position, flippedRotation);

        // 💾 Lưu pet đối thủ
        PlayerPrefs.SetString("EnemyPet", petPrefab.name);
        PlayerPrefs.Save();

        Debug.Log($"✅ Lưu pet đối thủ: {petPrefab.name}");
    }

    public void ClosePopup()
    {
        if (currentPet != null) Destroy(currentPet);
        popupSetup.SetActive(false);
        if (uiToHide != null) uiToHide.SetActive(true);
    }
}
