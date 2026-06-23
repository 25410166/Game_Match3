using Spine.Unity;
using UnityEngine;

public class BattleSceneLoader : MonoBehaviour
{
    [Header("Vị trí spawn")]
    public Transform playerSpawnPoint;
    public Transform enemySpawnPoint;

    [Header("Scale khi spawn")]
    [SerializeField] private float playerSpawnScale = 0.6f;
    [SerializeField] private float enemySpawnScale = 0.6f;

    private bool hasSpawned = false;

    private void Start()
    {
        // Only spawn once per scene load
        if (!hasSpawned)
        {
            hasSpawned = true;
            SpawnSelectedPets();
        }
    }

    private void SpawnSelectedPets()
    {
        PopulateBattleDataFromPrebattle();
        ApplySelectedAreaBackground();

        int selectedPlayerPetId = GameManager.Instance != null
            ? GameManager.Instance.battleData.playerPetId
            : PrebattleSelectionData.PlayerPetId;
        int selectedEnemyPetId = GameManager.Instance != null
            ? GameManager.Instance.battleData.enemyPetId
            : PrebattleSelectionData.EnemyPetId;

        int playerLevel = GameManager.Instance != null
            ? GameManager.Instance.battleData.playerLevel
            : PrebattleSelectionData.PlayerPetLevel;
        int enemyLevel = GameManager.Instance != null
            ? GameManager.Instance.battleData.enemyLevel
            : PrebattleSelectionData.EnemyPetLevel;

        if (selectedPlayerPetId < 0 || selectedEnemyPetId < 0)
        {
            Debug.LogWarning("⚠️ Pet ID từ PrebattleSelectionData không hợp lệ!");
            return;
        }

        // --- Spawn player pet ---
        GameObject playerPetPrefab = FindSelectedPlayerPetPrefab(selectedPlayerPetId, playerLevel);
        GameObject playerPet = null;

        if (playerPetPrefab != null)
        {
            // Use PetManager to spawn so PetBehaviour.Init is called and petData is set
            if (PetManager.Instance != null)
            {
                var pb = PetManager.Instance.SpawnPetForLevel(selectedPlayerPetId, playerLevel, playerSpawnPoint.position, null);
                if (pb != null)
                {
                    playerPet = pb.gameObject;
                    ApplyPetScale(playerPet, selectedPlayerPetId, playerLevel, isEnemy: false);

                    Debug.Log($"✅ Spawn pet người chơi (via PetManager) (ID: {selectedPlayerPetId}): {playerPet.name}");
                }
                else
                {
                    // fallback to direct instantiate
                    playerPet = Instantiate(playerPetPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
                    ApplyPetScale(playerPet, selectedPlayerPetId, playerLevel, isEnemy: false);
                    Debug.Log($"⚠️ Fallback spawn player pet via Instantiate: {playerPetPrefab.name}");
                }
            }
            else
            {
                playerPet = Instantiate(playerPetPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
                ApplyPetScale(playerPet, selectedPlayerPetId, playerLevel, isEnemy: false);
                Debug.Log($"✅ Spawn pet người chơi (fallback Instantiate) (ID: {selectedPlayerPetId}): {playerPetPrefab.name}");
            }
        }
        else
        {
            Debug.LogWarning($"❌ Không tìm thấy pet người chơi (ID: {selectedPlayerPetId})");
        }

        // --- Spawn enemy pet ---
        GameObject enemyPetPrefab = FindSelectedEnemyPetPrefab(selectedEnemyPetId, enemyLevel);
        GameObject enemyPet = null;

        if (enemyPetPrefab != null)
        {
            if (PetManager.Instance != null)
            {
                var pb = PetManager.Instance.SpawnPetForLevel(selectedEnemyPetId, enemyLevel, enemySpawnPoint.position, null);
                if (pb != null)
                {
                    enemyPet = pb.gameObject;
                    ApplyPetScale(enemyPet, selectedEnemyPetId, enemyLevel, isEnemy: true);
                    Debug.Log($"✅ Spawn pet đối thủ (via PetManager) (ID: {selectedEnemyPetId}): {enemyPet.name}");
                }
                else
                {
                    enemyPet = Instantiate(enemyPetPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);
                    ApplyPetScale(enemyPet, selectedEnemyPetId, enemyLevel, isEnemy: true);
                    Debug.Log($"⚠️ Fallback spawn enemy pet via Instantiate: {enemyPetPrefab.name}");
                }
            }
            else
            {
                enemyPet = Instantiate(enemyPetPrefab, enemySpawnPoint.position, enemySpawnPoint.rotation);
                ApplyPetScale(enemyPet, selectedEnemyPetId, enemyLevel, isEnemy: true);
                Debug.Log($"✅ Spawn pet đối thủ (fallback Instantiate) (ID: {selectedEnemyPetId}): {enemyPetPrefab.name}");
            }
        }
        else
        {
            Debug.LogWarning($"❌ Không tìm thấy pet đối thủ (ID: {selectedEnemyPetId})");
        }

        // --- Gán vào PlayerStats / AIStats ---
        AssignSpawnedPets(playerPet, enemyPet);
    }

    private void ApplySelectedAreaBackground()
    {
        int area = PrebattleSelectionData.MapArea;

        if (area < 0 && GameDataManager.Instance != null &&
            !string.IsNullOrWhiteSpace(PrebattleSelectionData.MapId))
        {
            MapDataAsset map = GameDataManager.Instance.GetMapData(PrebattleSelectionData.MapId);
            if (map != null)
                area = map.area;
        }

        if (area < 0)
            return;

        Debug.Log($"[BattleSceneLoader] Apply area background: {area}");
        AreaBackgroundManager.SetAreaBackground(area);
    }

    private GameObject FindSelectedPlayerPetPrefab(int petId, int level)
    {
        if (petId < 0)
        {
            Debug.LogWarning("[BattleSceneLoader] Player pet ID không hợp lệ!");
            return null;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("[BattleSceneLoader] GameDataManager không tồn tại!");
            return null;
        }

        int safeLevel = Mathf.Max(1, level);
        GameObject prefab = GameDataManager.Instance.GetPetPrefabForLevel(petId, safeLevel, string.Empty);
        if (prefab != null)
        {
            Debug.Log($"[BattleSceneLoader] Lấy pet player {petId} từ GameDataManager: {prefab.name}");
            return prefab;
        }

        Debug.LogWarning($"[BattleSceneLoader] Không tìm thấy pet {petId} trong GameDataManager!");
        return null;
    }

    private GameObject FindSelectedEnemyPetPrefab(int petId, int level)
    {
        if (petId < 0)
        {
            Debug.LogWarning("[BattleSceneLoader] Enemy pet ID không hợp lệ!");
            return null;
        }

        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("[BattleSceneLoader] GameDataManager không tồn tại!");
            return null;
        }

        int safeLevel = Mathf.Max(1, level);
        GameObject prefab = GameDataManager.Instance.GetPetPrefabForLevel(petId, safeLevel, string.Empty);
        if (prefab != null)
        {
            Debug.Log($"[BattleSceneLoader] Lấy pet enemy {petId} từ GameDataManager: {prefab.name}");
            return prefab;
        }

        Debug.LogWarning($"[BattleSceneLoader] Không tìm thấy pet {petId} trong GameDataManager!");
        return null;
    }

    private void ApplyPetScale(GameObject pet, int petId, int petLevel, bool isEnemy)
    {
        if (pet == null)
            return;

        float petScale = Mathf.Max(0.1f, isEnemy ? enemySpawnScale : playerSpawnScale);

        // Apply scale while preserving enemy flip
        Vector3 newScale = isEnemy
            ? new Vector3(-petScale, petScale, petScale)
            : new Vector3(petScale, petScale, petScale);

        pet.transform.localScale = newScale;
    }
    private void AssignSpawnedPets(GameObject playerPet, GameObject enemyPet)
{
    PlayerStats playerStats = FindObjectOfType<PlayerStats>();
    AIStats aiStats = FindObjectOfType<AIStats>();

    // 1. Gán tham chiếu cho Player và tính Scale
    if (playerStats != null && playerPet != null)
    {
        playerStats.skeletonAnim = playerPet.GetComponentInChildren<SkeletonAnimation>();
        playerStats.player = playerPet;
        GameManager.Instance.player = playerStats; // Đăng ký với Manager trước

        playerStats.baseScale = playerSpawnScale;
    }

    // 2. Gán tham chiếu cho AI và tính Scale
    if (aiStats != null && enemyPet != null)
    {
        aiStats.skeletonAnim = enemyPet.GetComponentInChildren<SkeletonAnimation>();
        aiStats.AI = enemyPet;
        GameManager.Instance.ai = aiStats; // Đăng ký với Manager trước

        aiStats.baseScale = enemySpawnScale;
    }

    // 3. Khởi tạo (Lúc này cả 2 đã ở trong GameManager nên Init() sẽ tìm thấy nhau để Flip)
    if (playerStats != null) {
        playerStats.ApplyBattleDataFromGameManager();
        playerStats.Init(); 
    }
    if (aiStats != null) {
        aiStats.ApplyBattleDataFromGameManager();
        aiStats.Init();
    }

    // 4. Cập nhật UI
    if (UIManager.Instance != null) UIManager.Instance.InitializeStatSliders();
}

    private void PopulateBattleDataFromPrebattle()
    {
        if (GameManager.Instance == null)
            return;

        int playerPetId = PrebattleSelectionData.PlayerPetId;
        int enemyPetId = PrebattleSelectionData.EnemyPetId;

        int playerLevel = Mathf.Max(1, PrebattleSelectionData.PlayerPetLevel);
        int enemyLevel = Mathf.Max(1, PrebattleSelectionData.EnemyPetLevel);

        if (playerLevel <= 1)
            playerLevel = ResolveOwnedPetLevel(playerPetId);

        if (enemyLevel <= 1 && GameDataManager.Instance != null)
        {
            MapDataAsset map = GameDataManager.Instance.GetMapData(PrebattleSelectionData.MapId);
            if (map != null)
                enemyLevel = Mathf.Max(1, map.petLevelSpawn);
        }

        int enemyGuardianId = PrebattleSelectionData.EnemyGuardianId;
        int enemyGuardianLevel = Mathf.Max(1, PrebattleSelectionData.EnemyGuardianLevel);

        if (GameDataManager.Instance != null)
        {
            MapDataAsset map = GameDataManager.Instance.GetMapData(PrebattleSelectionData.MapId);
            if (map != null)
            {
                enemyGuardianId = map.idGuadiant;
                enemyGuardianLevel = Mathf.Max(1, map.levelGuadiant);
            }
        }

        GameManager.Instance.SetBattleData(playerPetId, playerLevel, enemyPetId, enemyLevel, enemyGuardianId, enemyGuardianLevel);
    }

    private int ResolveOwnedPetLevel(int petId)
    {
        if (petId < 0)
            return 1;

        if (PlayerManager.Instance != null)
        {
            OwnedPetData owned = PlayerManager.Instance.GetOwnedPet(petId);
            if (owned != null)
                return Mathf.Max(1, owned.petLevel);
        }

        return 1;
    }

    private void LoadPlayerPetData(PlayerStats playerStats)
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("[BattleSceneLoader] GameDataManager không tồn tại!");
            return;
        }

        if (GameDataManager.Instance.TryGetPetStatSnapshot(playerStats.petId, playerStats.level, out var snapshot))
        {
            playerStats.maxHP = snapshot.baseHP;
            playerStats.maxMana = snapshot.baseMana;
            playerStats.maxRage = snapshot.baseRage;
            playerStats.armor = snapshot.armor;
            playerStats.baseAttack = snapshot.baseAttack;
            playerStats.critRate = snapshot.critRate;
            playerStats.critDamage = snapshot.critDamage;
            playerStats.playerName = snapshot.petName;

            Debug.Log($"✅ Load dữ liệu pet player (ID: {playerStats.petId}) - HP: {snapshot.baseHP}");
        }
        else
        {
            Debug.LogWarning($"❌ Không thể load dữ liệu pet player (ID: {playerStats.petId})");
        }
    }

    private void LoadAIPetData(AIStats aiStats)
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogWarning("[BattleSceneLoader] GameDataManager không tồn tại!");
            return;
        }

        if (GameDataManager.Instance.TryGetPetStatSnapshot(aiStats.petId, aiStats.level, out var snapshot))
        {
            aiStats.maxHealth = snapshot.baseHP;
            aiStats.maxMana = snapshot.baseMana;
            aiStats.maxRage = snapshot.baseRage;
            aiStats.armor = snapshot.armor;
            aiStats.baseAttack = snapshot.baseAttack;
            aiStats.critRate = snapshot.critRate;
            aiStats.critDamage = snapshot.critDamage;

            Debug.Log($"✅ Load dữ liệu pet AI (ID: {aiStats.petId}) - HP: {snapshot.baseHP}");
        }
        else
        {
            Debug.LogWarning($"❌ Không thể load dữ liệu pet AI (ID: {aiStats.petId})");
        }
    }

}
