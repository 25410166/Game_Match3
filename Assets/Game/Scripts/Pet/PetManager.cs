using UnityEngine;
using System.Collections.Generic;

public class PetManager : MonoBehaviour
{
    public static PetManager Instance;

    [Header("Pet Prefabs")]
    public List<GameObject> petPrefabs;

    [Header("Pet Data Assets")]
    public List<PetDataAsset> petDatabase; // lưu list các asset PetData

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public PetDataAsset GetPetById(int id)
    {
        return petDatabase.Find(p => p.id == id);
    }

    public PetBehaviour SpawnPet(int id, Vector3 pos, Transform parent = null)
    {
        PetDataAsset data = GetPetById(id);
        if (data == null)
        {
            Debug.LogError($"Không tìm thấy pet id={id}");
            return null;
        }

        GameObject prefab = petPrefabs.Find(p => p.name.Contains(data.petName));
        if (prefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab cho pet {data.petName}");
            return null;
        }

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, parent);
        PetBehaviour pet = obj.GetComponent<PetBehaviour>();
        pet.Init(data);

        return pet;
    }

    public PetBehaviour SpawnPetForLevel(int id, int level, Vector3 pos, Transform parent = null)
    {
        PetDataAsset data = GetPetById(id);
        if (data == null)
        {
            Debug.LogError($"Không tìm thấy pet id={id}");
            return null;
        }

        GameObject prefab = null;
        if (GameDataManager.Instance != null)
            prefab = GameDataManager.Instance.GetPetPrefabForLevel(id, Mathf.Max(1, level), data.petName);

        if (prefab == null)
            prefab = petPrefabs.Find(p => p.name.Contains(data.petName));

        if (prefab == null)
        {
            Debug.LogError($"Không tìm thấy prefab cho pet {data.petName}");
            return null;
        }

        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, parent);
        PetBehaviour pet = obj.GetComponent<PetBehaviour>();
        if (pet != null)
            pet.Init(data);

        return pet;
    }
}
