#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class PetDatabaseBuilder
{
    [MenuItem("Tools/Assign Prefabs To Pets (Sequential)")]
    public static void AssignPrefabsSequential()
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

        // 👉 Sort theo số trong tên (Monster_1, Monster_2, ..., Monster_10, ...)
        prefabs.Sort((a, b) =>
        {
            int numA = ExtractNumber(a.name);
            int numB = ExtractNumber(b.name);
            return numA.CompareTo(numB);
        });

        if (prefabs.Count == 0)
        {
            Debug.LogError("❌ Không có prefab hợp lệ nào.");
            return;
        }

        int countAssigned = 0;

        // Gán tuần tự: 10 pet trong db xài chung 1 prefab
        for (int i = 0; i < db.pets.Count; i++)
        {
            int prefabIndex = (i / 10) % prefabs.Count;
            db.pets[i].prefab = prefabs[prefabIndex];
            countAssigned++;
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Đã gán prefab cho {countAssigned} pets (theo thứ tự số Monster_1 -> Monster_70).");
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
