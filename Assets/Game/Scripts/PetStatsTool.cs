#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// Tool editor:
/// - Chọn PetDatabase.asset trong Project window trước khi chạy.
/// - Tool sẽ load tất cả prefab trong thư mục prefabPath (thứ tự theo số trong tên).
/// - Mỗi prefab sẽ được gán 10 bản ghi liên tiếp từ PetDatabase.pets:
///     prefab index 0  <- db[0..9]
///     prefab index 1  <- db[10..19]
///     ...
/// - Nếu db không đủ entry cho 1 prefab thì sẽ gán những entry còn có.
/// - Tool dùng PrefabUtility.LoadPrefabContents/SaveAsPrefabAsset để sửa prefab asset trực tiếp.
/// </summary>
public class PetStatsTool_Sequential
{
    // sửa đường dẫn nếu bạn lưu prefab ở chỗ khác
    private const string prefabPath = "Assets/Fantazia Animated 2D Monsters/Prefabs";

    [MenuItem("Tools/Attach Stats To Prefabs (Sequential)")]
    public static void AttachStatsToPrefabsSequential()
    {
        // chọn database
        var db = Selection.activeObject as PetDatabase;
        if (db == null)
        {
            Debug.LogError("❌ Chọn PetDatabase.asset trước khi chạy!");
            return;
        }

        if (db.pets == null || db.pets.Count == 0)
        {
            Debug.LogError("❌ PetDatabase không có dữ liệu (db.pets rỗng).");
            return;
        }

        // load tất cả prefab trong thư mục
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabPath });
        List<GameObject> prefabs = new List<GameObject>();
        foreach (var g in guids)
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (pf != null) prefabs.Add(pf);
        }

        if (prefabs.Count == 0)
        {
            Debug.LogError($"❌ Không tìm thấy prefab trong: {prefabPath}");
            return;
        }

        // sort prefab theo số trong tên (ví dụ Monster_1, Monster_2, ...)
        prefabs.Sort((a, b) => ExtractNumber(a.name).CompareTo(ExtractNumber(b.name)));

        int totalDb = db.pets.Count;
        int totalPrefabs = prefabs.Count;
        Debug.Log($"Info: {totalDb} db entries, {totalPrefabs} prefabs found. Will assign up to {totalPrefabs * 10} entries.");

        int assignedPrefabs = 0;
        for (int i = 0; i < totalPrefabs; i++)
        {
            int startIndex = i * 10; // mỗi prefab nhận 10 entry liên tiếp
            if (startIndex >= totalDb) break; // không còn dữ liệu

            // load prefab asset contents để chỉnh
            string path = AssetDatabase.GetAssetPath(prefabs[i]);
            if (string.IsNullOrEmpty(path)) continue;

            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
            if (prefabRoot == null)
            {
                Debug.LogWarning($"⚠️ Không thể load prefab contents: {path}");
                continue;
            }

            // lấy hoặc thêm component PetStatsHolder (bạn đã có class này)
            PetStatsHolder holder = prefabRoot.GetComponent<PetStatsHolder>();
            if (holder == null) holder = prefabRoot.AddComponent<PetStatsHolder>();

            // reset và gán
            holder.levels = holder.levels ?? new List<PetLevelData>();
            holder.levels.Clear();

            // gán tối đa 10 levels (nếu db thiếu thì gán ít hơn)
            int added = 0;
            for (int j = 0; j < 10; j++)
            {
                int dbIndex = startIndex + j;
                if (dbIndex >= totalDb) break;

                var data = db.pets[dbIndex];
                if (data == null) continue;

                // Tạo PetLevelData và copy các trường tương ứng
                PetLevelData lvl = new PetLevelData()
                {
                    level = data.level,
                    baseHP = data.baseHP,
                    armor = data.armor,
                    baseMana = data.baseMana,
                    baseRage = data.baseRage,
                    baseAttack = data.baseAttack,
                    critRate = data.critRate,
                    critDamage = data.critDamage,
                    weakness = data.weakness,
                    attackType = data.attackType
                };

                holder.levels.Add(lvl);
                added++;
            }

            // set thông tin chung cho prefab holder
            holder.petId = i + 1; // tùy bạn muốn petId bắt đầu từ 1
            holder.petName = (startIndex < totalDb && db.pets[startIndex] != null) ? db.pets[startIndex].petName : prefabs[i].name;
            holder.prefabRef = prefabs[i];

            // Lưu prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            assignedPrefabs++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Gắn stats tuần tự hoàn tất: {assignedPrefabs} prefabs được gán (mỗi prefab nhận tới 10 level).");
    }

    private static int ExtractNumber(string name)
    {
        var m = Regex.Match(name, @"\d+");
        if (m.Success && int.TryParse(m.Value, out int v)) return v;
        return int.MaxValue;
    }
}
#endif
