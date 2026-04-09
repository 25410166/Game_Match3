using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class PetStatsHolder : MonoBehaviour
{
    [Header("Identification")]
    public int petId;
    public string petName;
    public GameObject prefabRef;

    [Header("Levels (1..10 expected)")]
    public List<PetLevelData> levels = new List<PetLevelData>();

    [Header("Inspector testing")]
    [Range(1, 10)]
    public int selectedLevel = 1;

    // tiện getter
    public PetLevelData GetLevelData(int level)
    {
        if (levels == null || levels.Count == 0) return null;
        // tìm đúng level nếu có
        foreach (var l in levels)
            if (l.level == level) return l;
        // fallback: trả item index (level-1) nếu tồn tại
        int idx = Mathf.Clamp(level - 1, 0, levels.Count - 1);
        return levels[idx];
    }

    private void OnValidate()
    {
        // keep selectedLevel valid range
        if (selectedLevel < 1) selectedLevel = 1;
        if (selectedLevel > 50 && levels.Count > 0) selectedLevel = Mathf.Clamp(selectedLevel, 1, levels.Count);
    }
}

[System.Serializable]
public class PetLevelData
{
    public int level;
    public int baseHP;
    public int armor;
    public int baseMana;
    public int baseRage;
    public int baseAttack;
    public float critRate;
    public float critDamage;
    public string weakness;
    public AttackType attackType;

}
