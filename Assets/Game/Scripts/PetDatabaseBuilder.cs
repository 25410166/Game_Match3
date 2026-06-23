#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;

public class PetDatabaseBuilder
{
    [MenuItem("Tools/Assign Prefabs To Pets (By Name_Prefab)")]
    public static void AssignPrefabsByName()
    {
        var db = Selection.activeObject as PetDatabase;
        if (db == null)
        {
            Debug.LogError("❌ Chọn PetDatabase.asset trong Project trước khi chạy!");
            return;
        }

        string prefabPath = "Assets/Fantazia Animated 2D Monsters/Prefabs";
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabPath });

        if (guids == null || guids.Length == 0)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy prefab nào trong {prefabPath}");
            return;
        }

        // Load tất cả prefab
        List<GameObject> prefabs = new List<GameObject>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
                prefabs.Add(prefab);
        }

        if (prefabs.Count == 0)
        {
            Debug.LogError("❌ Không có prefab hợp lệ nào.");
            return;
        }

        Dictionary<string, GameObject> prefabMap = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var prefab in prefabs)
        {
            if (prefab != null && !prefabMap.ContainsKey(prefab.name))
            {
                prefabMap[prefab.name] = prefab;
            }
        }

        int countAssigned = 0;
        int countMissing = 0;

        // Gán theo đúng tên prefab từ cột Name_Prefab
        for (int i = 0; i < db.pets.Count; i++)
        {
            var pet = db.pets[i];
            if (pet == null)
                continue;

            string prefabName = string.IsNullOrWhiteSpace(pet.prefabName) ? string.Empty : pet.prefabName.Trim();
            if (string.IsNullOrEmpty(prefabName))
            {
                countMissing++;
                Debug.LogWarning($"⚠️ Pet id={pet.id} ({pet.petName}) chưa có Name_Prefab.");
                continue;
            }

            if (prefabName.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                prefabName = prefabName.Substring(0, prefabName.Length - 7);
            }

            if (!prefabMap.TryGetValue(prefabName, out GameObject prefab))
            {
                string[] exactGuids = AssetDatabase.FindAssets($"{prefabName} t:Prefab", new[] { prefabPath });
                if (exactGuids != null && exactGuids.Length > 0)
                {
                    string foundPath = AssetDatabase.GUIDToAssetPath(exactGuids[0]);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(foundPath);
                    if (prefab != null && !prefabMap.ContainsKey(prefab.name))
                    {
                        prefabMap[prefab.name] = prefab;
                    }
                }
            }

            if (prefab == null)
            {
                countMissing++;
                Debug.LogWarning($"⚠️ Không tìm thấy prefab '{prefabName}' cho pet id={pet.id} ({pet.petName}).");
                continue;
            }

            pet.prefab = prefab;
            countAssigned++;
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Đã gán prefab theo Name_Prefab: {countAssigned} pets, thiếu {countMissing} pets.");
    }

    private static int ExtractNumber(string name)
    {
        // Tìm số đầu tiên trong tên prefab
        Match m = Regex.Match(name, @"\d+");
        if (m.Success)
            return int.Parse(m.Value);
        return int.MaxValue; // nếu không có số
    }
}
#endif
