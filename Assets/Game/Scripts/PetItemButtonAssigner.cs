#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PetItemButtonAssigner
{
    [MenuItem("Tools/Assign PetItemButton To All Pet Prefabs")]
    public static void AssignPetItemButton()
    {
        // --- B1: Tìm ChoosePet trong scene ---
        ChoosePet choosePetUI = Object.FindObjectOfType<ChoosePet>();
        if (choosePetUI == null)
        {
            Debug.LogError("❌ Không tìm thấy ChoosePet trong Scene. Hãy mở Scene có UI chọn pet trước!");
            return;
        }

        // --- B2: Tìm tất cả prefab trong folder pet ---
        string prefabPath = "Assets/Fantazia Animated 2D Monsters/Prefabs";
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabPath });
        if (guids.Length == 0)
        {
            Debug.LogWarning($"⚠️ Không tìm thấy prefab nào trong thư mục: {prefabPath}");
            return;
        }

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            // --- B3: Thêm component PetItemButton nếu chưa có ---
            PetItemButton button = prefab.GetComponent<PetItemButton>();
            if (button == null)
                button = prefab.AddComponent<PetItemButton>();

            // Giữ lại component để cấu hình trong prefab UI item.

            // Đánh dấu thay đổi
            EditorUtility.SetDirty(prefab);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ Đã gắn PetItemButton cho {count} prefabs trong thư mục {prefabPath}!");
    }
}
#endif
