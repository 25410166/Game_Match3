using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PetResource", menuName = "Game/Resource/Pet Resource")]
public class PetResource : ScriptableObject
{
    [Serializable]
    public class PetPrefabEntry
    {
        public int petId;
        public string prefabName;
        public GameObject prefab;
    }

    public List<PetPrefabEntry> prefabs = new List<PetPrefabEntry>();

    public GameObject GetPrefabByPetId(int petId)
    {
        if (petId < 0 || prefabs == null)
            return null;

        for (int i = 0; i < prefabs.Count; i++)
        {
            PetPrefabEntry entry = prefabs[i];
            if (entry == null || entry.prefab == null)
                continue;

            if (entry.petId == petId)
                return entry.prefab;
        }

        return null;
    }

    public GameObject GetPrefabByName(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName) || prefabs == null)
            return null;

        string normalized = prefabName.Trim();
        for (int i = 0; i < prefabs.Count; i++)
        {
            PetPrefabEntry entry = prefabs[i];
            if (entry == null || entry.prefab == null)
                continue;

            if (string.Equals(entry.prefabName, normalized, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(entry.prefab.name, normalized, StringComparison.OrdinalIgnoreCase))
                return entry.prefab;
        }

        return null;
    }
}
