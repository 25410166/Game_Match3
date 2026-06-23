using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationTableData", menuName = "Game/Localization/Localization Table Data")]
public class LocalizationTableData : ScriptableObject
{
    public List<LocalizationEntry> entries = new List<LocalizationEntry>();

    public void SetEntries(List<LocalizationEntry> source)
    {
        entries = new List<LocalizationEntry>();
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            LocalizationEntry entry = source[i];
            if (entry == null)
                continue;

            entries.Add(entry.Clone());
        }
    }
}
