using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Stat
{
    [SerializeField] private int baseValue;
    private List<int> modifiers = new List<int>();

    public Stat(int baseValue)
    {
        this.baseValue = baseValue;
    }

    public int Value
    {
        get
        {
            int finalValue = baseValue;
            foreach (int mod in modifiers) finalValue += mod;
            return Mathf.Max(0, finalValue);
        }
    }

    public void SetBase(int value) => baseValue = value;

    public void AddModifier(int value) => modifiers.Add(value);

    public void RemoveModifier(int value) => modifiers.Remove(value);

    public void ClearModifiers() => modifiers.Clear();
}
