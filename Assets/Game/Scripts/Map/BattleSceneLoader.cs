using Spine.Unity;
using UnityEngine;

public class BattleSceneLoader : MonoBehaviour
{
    [Header("Vị trí spawn")]
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;

    [Header("Danh sách toàn bộ pet")]
    public GameObject[] allPetPrefabs;

    private void Start()
    {
        SpawnSelectedPets();
    }

    private void SpawnSelectedPets()
    {
        string playerPetName = PlayerPrefs.GetString("PlayerPet", "");
        string enemyPetName = PlayerPrefs.GetString("EnemyPet", "");

        if (string.IsNullOrEmpty(playerPetName) || string.IsNullOrEmpty(enemyPetName))
        {
            Debug.LogWarning("⚠️ Chưa có pet được lưu để spawn!");
            return;
        }

        // --- Spawn player pet ---
        GameObject playerPetPrefab = FindPetPrefab(playerPetName);
        GameObject playerPet = null;

        if (playerPetPrefab != null)
        {
            playerPet = Instantiate(playerPetPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
            Debug.Log($"✅ Spawn pet người chơi: {playerPetPrefab.name}");
        }
        else
        {
            Debug.LogWarning($"❌ Không tìm thấy pet người chơi: {playerPetName}");
        }

        // --- Spawn enemy pet ---
        GameObject enemyPetPrefab = FindPetPrefab(enemyPetName);
        GameObject enemyPet = null;

        if (enemyPetPrefab != null)
        {
            enemyPet = Instantiate(enemyPetPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);
            enemyPet.transform.localScale = new Vector3(-1, enemyPet.transform.localScale.y, enemyPet.transform.localScale.z);
            Debug.Log($"✅ Spawn pet đối thủ: {enemyPetPrefab.name}");
        }
        else
        {
            Debug.LogWarning($"❌ Không tìm thấy pet đối thủ: {enemyPetName}");
        }

        // --- Gán vào PlayerStats / AIStats ---
        AssignSpawnedPets(playerPet, enemyPet);
    }

    private GameObject FindPetPrefab(string name)
    {
        foreach (GameObject pet in allPetPrefabs)
        {
            if (pet != null && pet.name == name)
                return pet;
        }
        return null;
    }

    private void AssignSpawnedPets(GameObject playerPet, GameObject enemyPet)
    {
        PlayerStats playerStats = FindObjectOfType<PlayerStats>();
        AIStats aiStats = FindObjectOfType<AIStats>();

        // --- Gán cho player ---
        if (playerStats != null && playerPet != null)
        {
            playerStats.skeletonAnim = playerPet.GetComponentInChildren<SkeletonAnimation>();
            playerStats.player = playerPet; // chính con pet player
            Debug.Log("🔗 Gán pet cho PlayerStats thành công!");
        }
        else
        {
            Debug.LogWarning("⚠️ Không tìm thấy PlayerStats hoặc pet người chơi!");
        }

        // --- Gán cho AI ---
        if (aiStats != null && enemyPet != null)
        {
            aiStats.skeletonAnim = enemyPet.GetComponentInChildren<SkeletonAnimation>();
            aiStats.AI = playerPet; // con AI biết player target là ai
            Debug.Log("🔗 Gán pet cho AIStats thành công!");
        }
        else
        {
            Debug.LogWarning("⚠️ Không tìm thấy AIStats hoặc pet đối thủ!");
        }

        // --- Gán chéo lại cho player biết AI target ---
        if (playerStats != null && enemyPet != null)
        {
            // Nếu muốn PlayerStats biết target là AI thì thêm dòng này
            var aiComponent = enemyPet.GetComponentInChildren<AIStats>();
            if (aiComponent != null)
            {
                // Gán AI object trực tiếp
                GameManager.Instance.ai = aiComponent;
                Debug.Log("🔗 Gán AI vào GameManager cho Player sử dụng!");
            }
        }

        if (playerStats != null)
        {
            GameManager.Instance.player = playerStats;
            Debug.Log("🔗 Gán Player vào GameManager thành công!");
        }
    }

}
