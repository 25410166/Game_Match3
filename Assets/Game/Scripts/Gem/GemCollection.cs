using UnityEngine;

[CreateAssetMenu(fileName = "New Gem Collection", menuName = "Gem/GemCollection")]
public class GemCollection : ScriptableObject
{
    [System.Serializable]
    public class GemLevelData
    {
        [Range(1, 5)] public int level;          // Cấp độ (1-5)
        public Sprite sprite;                    // Hình ảnh của đá
        public int priceGold;                    // giá vàng trong shop
    }

    [System.Serializable]
    public class GemElementData
    {
        public string element;                   // Tên hệ (Fire, Water, v.v.)
        public GemLevelData[] gemLevels = new GemLevelData[5]; // 5 cấp độ của hệ này
    }

    public GemElementData[] elements = new GemElementData[7]; // 7 hệ, mỗi hệ 5 viên

    private void OnValidate()
    {
        if (elements == null) return;

        for (int i = 0; i < elements.Length; i++)
        {
            GemElementData element = elements[i];
            if (element == null || element.gemLevels == null) continue;

            for (int j = 0; j < element.gemLevels.Length; j++)
            {
                GemLevelData levelData = element.gemLevels[j];
                if (levelData == null) continue;

                levelData.level = j + 1;
            }
        }
    }
}
