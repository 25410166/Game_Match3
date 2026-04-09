using UnityEngine;
using UnityEngine.SceneManagement;

public class PetItemButton : MonoBehaviour
{
    [Header("Pet Settings")]
    public GameObject petPrefab; // Prefab thật của pet
    public ChoosePet choosePetUI; // UI chọn pet (nếu có)
    [Header("Spawn Settings (SceneMap)")]
    public Transform spawnPoint; // Vị trí spawn
    [Header("UI Settings")]
    public GameObject petPage; // Tham chiếu đến GameObject của petPage (UI)

    private GameObject spawnedPet;
    private bool isMapScene;

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        isMapScene = currentScene.Contains("SceneMap");
    }

    private void OnMouseDown()
    {
        if (isMapScene)
        {
            SpawnPetInMap();
            return;
        }
        if (choosePetUI != null)
        {
            choosePetUI.ShowPet(petPrefab);
        }
    }

    private void SpawnPetInMap()
    {
        if (petPrefab == null)
        {
            Debug.LogWarning("⚠️ PetPrefab chưa gán!");
            return;
        }
        if (spawnedPet != null)
            Destroy(spawnedPet);
        Transform point = spawnPoint != null ? spawnPoint : transform;
        spawnedPet = Instantiate(petPrefab, point.position, point.rotation);
        // 💾 Lưu pet người chơi
        PlayerPrefs.SetString("PlayerPet", petPrefab.name);
        PlayerPrefs.Save();
        Debug.Log($"✅ Lưu pet người chơi: {petPrefab.name}");

        // Tắt petPage sau khi spawn
        if (petPage != null)
        {
            petPage.SetActive(false);
            Debug.Log($"✅ Tắt petPage thành công!");
        }
        else
        {
            Debug.LogWarning("⚠️ petPage chưa được gán trong Inspector!");
        }
    }
}