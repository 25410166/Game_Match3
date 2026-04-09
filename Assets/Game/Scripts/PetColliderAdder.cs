#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PetColliderAdder
{
    [MenuItem("Tools/Add Collider To All Pet Prefabs")]
    public static void AddColliders()
    {
        string prefabPath = "Assets/Fantazia Animated 2D Monsters/Prefabs";
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null && prefab.GetComponent<Collider2D>() == null)
            {
                var collider = prefab.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                EditorUtility.SetDirty(prefab);
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✅ Đã thêm BoxCollider2D cho tất cả pet prefab (nếu chưa có).");
    }
}
#endif
