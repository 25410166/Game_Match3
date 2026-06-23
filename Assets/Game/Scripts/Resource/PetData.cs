using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PetDatabase", menuName = "Pet/Pet Database")]
public class PetDatabase : ScriptableObject
{
    public List<PetDataAsset> pets;

    public PetDataAsset GetPetById(int id)
    {
        return pets.Find(p => p.id == id);
    }
}

[System.Serializable]
public class PetDataAsset
{
    [Header("Basic Info")]
    public int id;
    public string petName;
    public string element;
    public int level;
    public int petId; // Pet_ID trong sheet

    [Header("Prefab")]
    public string prefabName;
    public GameObject prefab;

    [Header("Stats")]
    public int baseHP;
    public int armor;
    public int baseMana;
    public int baseRage;
    public int baseAttack;

    [Header("Crit Stats")]
    public float critRate;
    public float critDamage;

    [Header("Other")]
    public string weakness;
    public AttackType attackType; // Melee hoặc Ranged

    [Header("Animations")]
    public string idleAnim = "Idle";
    public string attackMeleeAnim = "Attack";
    public string attackRangedAnim = "Attack";
    public string deadAnim = "Dead";
    [Header("Skill")]
public int skillId;
}

public enum AttackType
{
    Melee,
    Range
}
