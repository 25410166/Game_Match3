using UnityEngine;

[CreateAssetMenu(fileName = "New Gem Collection", menuName = "Gem/GemCollection")]
public class GemCollection : ScriptableObject
{
    [System.Serializable]
    public class GemLevelData
    {
        [Range(1, 5)] public int level;          // Cấp độ (1-5)
        public Sprite sprite;                    // Hình ảnh của đá
        [Range(0, 100)] public float upgradeBonus; // Tăng tỉ lệ nâng cấp pet (%)
    }

    [System.Serializable]
    public class GemElementData
    {
        public string element;                   // Tên hệ (Fire, Water, v.v.)
        public GemLevelData[] gemLevels = new GemLevelData[5]; // 5 cấp độ của hệ này
    }

    public GemElementData[] elements = new GemElementData[7]; // 7 hệ, mỗi hệ 5 viên
}
