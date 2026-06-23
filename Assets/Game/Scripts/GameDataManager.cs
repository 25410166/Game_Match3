using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    [Serializable]
    public class PetStatSnapshot
    {
        public int petId;
        public int level;
        public string petName;
        public string element;
        public int baseHP;
        public int armor;
        public int baseMana;
        public int baseRage;
        public int baseAttack;
        public float critRate;
        public float critDamage;
        internal int skillId;
    }

    private static GameDataManager instance;
    public static GameDataManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<GameDataManager>();
            return instance;
        }
    }

    [Header("Core Data")]
    [SerializeField] private UnityEngine.Object petDatabase;
    [SerializeField] private MapDatabase mapDatabase;
    [SerializeField] private UnityEngine.Object cardDatabase;
    [SerializeField] private UnityEngine.Object gemCollection;
    [SerializeField] private UnityEngine.Object match3SpriteResource;
    [SerializeField] private UnityEngine.Object petResource;
    [SerializeField] private SkillDatabase skillDatabase;
    [SerializeField] private GuardianDatabase guardianDatabase;

    // Cache for ResolvePetFamilyId to avoid infinite recursion
    private Dictionary<int, int> petFamilyIdCache = new Dictionary<int, int>();

    public MapDatabase MapDatabase => mapDatabase;
    public UnityEngine.Object CardDatabaseObject => cardDatabase;
    public UnityEngine.Object GemCollectionObject => gemCollection;
    public UnityEngine.Object Match3SpriteResourceObject => match3SpriteResource;
    public SkillDatabase SkillDatabase => skillDatabase;
    public GuardianDatabase GuardianDatabase => guardianDatabase;

    public SkillData GetSkillData(int skillId)
    {
        if (skillId <= 0 || skillDatabase == null)
            return null;

        return skillDatabase.GetSkillById(skillId);
    }

    public MapDataAsset GetMapData(string mapId)
    {
        if (mapDatabase == null)
            return null;

        return mapDatabase.GetMapById(mapId);
    }

    /// <summary>
    /// Clear pet family ID cache (call after updating pet data)
    /// </summary>
    public void ClearPetFamilyIdCache()
    {
        if (petFamilyIdCache != null)
            petFamilyIdCache.Clear();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize cache
        if (petFamilyIdCache == null)
            petFamilyIdCache = new Dictionary<int, int>();
    }

    public GameObject GetPetPrefab(int petId, string fallbackName = "")
    {
        GameObject fromPetResourceId = CallPetResourceById(petId);
        if (fromPetResourceId != null)
            return fromPetResourceId;

        object petData = GetPetDataById(petId);
        if (petData != null)
        {
            GameObject directPrefab = GetFieldValue<GameObject>(petData, "prefab");
            if (directPrefab != null)
                return directPrefab;

            string prefabName = GetFieldValue<string>(petData, "prefabName");
            if (!string.IsNullOrWhiteSpace(prefabName))
            {
                GameObject byPrefabName = GetPetPrefabByName(prefabName.Trim());
                if (byPrefabName != null)
                    return byPrefabName;
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackName))
            return GetPetPrefabByName(fallbackName.Trim());

        return null;
    }

    public GameObject GetPetPrefabForLevel(int petId, int level, string fallbackName = "")
    {
        object petRow = GetPetDataByPetIdAndLevel(petId, level);
        if (petRow != null)
        {
            GameObject directPrefab = GetFieldValue<GameObject>(petRow, "prefab");
            if (directPrefab != null)
                return directPrefab;

            string prefabName = GetFieldValue<string>(petRow, "prefabName");
            if (!string.IsNullOrWhiteSpace(prefabName))
            {
                GameObject byPrefabName = GetPetPrefabByName(prefabName.Trim());
                if (byPrefabName != null)
                    return byPrefabName;
            }
        }

        return GetPetPrefab(petId, fallbackName);
    }

    public bool TryGetPetStatSnapshot(int petId, int level, out PetStatSnapshot snapshot)
    {
        snapshot = null;
        if (petId < 0)
            return false;

        object petRow = GetPetDataByPetIdAndLevel(petId, level);
        if (petRow == null)
            return false;

        snapshot = new PetStatSnapshot
        {
            petId = GetFieldValue<int>(petRow, "petId"),
            level = Mathf.Max(1, GetFieldValue<int>(petRow, "level")),
            petName = GetFieldValue<string>(petRow, "petName"),
            element = GetFieldValue<string>(petRow, "element"),
            baseHP = GetFieldValue<int>(petRow, "baseHP"),
            armor = GetFieldValue<int>(petRow, "armor"),
            baseMana = GetFieldValue<int>(petRow, "baseMana"),
            baseRage = GetFieldValue<int>(petRow, "baseRage"),
            baseAttack = GetFieldValue<int>(petRow, "baseAttack"),
            critRate = GetFieldValue<float>(petRow, "critRate"),
            critDamage = GetFieldValue<float>(petRow, "critDamage"),
            skillId = GetFieldValue<int>(petRow, "skillId")
        };

        if (snapshot.petId <= 0)
            snapshot.petId = petId;

        return true;
    }

    public int GetPetMaxLevel(int petId)
    {
        IList petList = GetPetDataList();
        if (petList == null)
            return 1;

        int resolvedFamilyPetId = ResolvePetFamilyId(petId);
        int maxLevel = 1;
        for (int i = 0; i < petList.Count; i++)
        {
            object pet = petList[i];
            if (pet == null)
                continue;

            int itemPetId = GetFieldValue<int>(pet, "petId");
            int itemInternalId = GetFieldValue<int>(pet, "id");
            if (itemPetId != petId && itemInternalId != petId && itemPetId != resolvedFamilyPetId)
                continue;

            int itemLevel = Mathf.Max(1, GetFieldValue<int>(pet, "level"));
            if (itemLevel > maxLevel)
                maxLevel = itemLevel;
        }

        return maxLevel;
    }

    public GameObject GetPetPrefabByName(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
            return null;

        string normalized = prefabName.Trim();

        GameObject fromPetResourceName = CallPetResourceByName(normalized);
        if (fromPetResourceName != null)
            return fromPetResourceName;

        IList petList = GetPetDataList();
        if (petList == null)
            return null;

        for (int i = 0; i < petList.Count; i++)
        {
            object pet = petList[i];
            if (pet == null)
                continue;

            GameObject prefab = GetFieldValue<GameObject>(pet, "prefab");
            if (prefab != null && string.Equals(prefab.name, normalized, StringComparison.OrdinalIgnoreCase))
                return prefab;
        }

        return null;
    }

    private object GetPetDataByPetIdAndLevel(int petId, int level)
    {
        IList petList = GetPetDataList();
        if (petList == null)
            return null;

        int targetLevel = Mathf.Max(1, level);
        int speciesId = ResolvePetFamilyId(petId);

        try
        {
            for (int i = 0; i < petList.Count; i++)
            {
                object pet = petList[i];
                if (pet == null) 
                    continue;

                int itemPetId = GetFieldValue<int>(pet, "petId");
                int itemInternalId = GetFieldValue<int>(pet, "id");
                int petLevel = GetFieldValue<int>(pet, "level");

                // Kiểm tra: Nếu khớp ID loài HOẶC khớp ID dòng, và phải khớp Level
                if ((itemPetId == speciesId || itemInternalId == petId) && petLevel == targetLevel)
                {
                    return pet;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameDataManager] Error in GetPetDataByPetIdAndLevel({petId}, {level}): {ex.Message}");
            return null;
        }

        return null;
    }

    private int ResolvePetFamilyId(int petId)
    {
        // Check cache first
        if (petFamilyIdCache != null && petFamilyIdCache.TryGetValue(petId, out int cachedResult))
            return cachedResult;

        IList petList = GetPetDataList();
        if (petList == null)
            return petId;

        int result = petId;

        try
        {
            for (int i = 0; i < petList.Count; i++)
            {
                object pet = petList[i];
                if (pet == null)
                    continue;

                int itemInternalId = GetFieldValue<int>(pet, "id");
                if (itemInternalId != petId)
                    continue;

                int itemPetId = GetFieldValue<int>(pet, "petId");
                if (itemPetId > 0)
                {
                    result = itemPetId;
                    break;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameDataManager] Error in ResolvePetFamilyId({petId}): {ex.Message}");
            result = petId;
        }

        // Cache the result
        if (petFamilyIdCache != null)
            petFamilyIdCache[petId] = result;

        return result;
    }

    private object GetPetDataById(int id)
    {
        IList petList = GetPetDataList();
        if (petList == null)
            return null;

        for (int i = 0; i < petList.Count; i++)
        {
            object pet = petList[i];
            if (pet == null)
                continue;

            int internalId = GetFieldValue<int>(pet, "id");
            int petId = GetFieldValue<int>(pet, "petId");
            if (internalId == id || petId == id)
                return pet;
        }

        return null;
    }

    private IList GetPetDataList()
    {
        if (petDatabase == null)
            return null;

        FieldInfo petsField = petDatabase.GetType().GetField("pets", BindingFlags.Public | BindingFlags.Instance);
        if (petsField == null)
            return null;

        return petsField.GetValue(petDatabase) as IList;
    }

    private GameObject CallPetResourceById(int petId)
    {
        if (petResource == null)
            return null;

        MethodInfo method = petResource.GetType().GetMethod("GetPrefabByPetId", BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
            return null;

        object result = method.Invoke(petResource, new object[] { petId });
        return result as GameObject;
    }

    private GameObject CallPetResourceByName(string prefabName)
    {
        if (petResource == null)
            return null;

        MethodInfo method = petResource.GetType().GetMethod("GetPrefabByName", BindingFlags.Public | BindingFlags.Instance);
        if (method == null)
            return null;

        object result = method.Invoke(petResource, new object[] { prefabName });
        return result as GameObject;
    }

    private static T GetFieldValue<T>(object target, string fieldName)
    {
        if (target == null)
            return default(T);

        if (string.IsNullOrWhiteSpace(fieldName))
            return default(T);

        try
        {
            Type targetType = target.GetType();
            if (targetType == null)
                return default(T);

            FieldInfo field = targetType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
                return default(T);

            object value = field.GetValue(target);
            if (value is T)
                return (T)value;

            return default(T);
        }
        catch (System.StackOverflowException ex)
        {
            Debug.LogError($"[GameDataManager] StackOverflow in GetFieldValue: target type={target?.GetType()?.Name}, field={fieldName}");
            throw ex;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameDataManager] Error getting field {fieldName}: {ex.GetType().Name} - {ex.Message}");
            return default(T);
        }
    }
}
