using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Element weakness/advantage system.
/// 
/// Weakness Chart:
/// - Fire counters Metal
/// - Metal counters Wood
/// - Wood counters Earth
/// - Earth counters Water
/// - Water counters Fire
/// - Light counters Dark
/// - Dark counters Light
/// </summary>
public static class ElementWeaknessSystem
{
    private static readonly Dictionary<string, string> WeaknessMap = new Dictionary<string, string>()
    {
        { "Fire", "Metal" },
        { "Metal", "Wood" },
        { "Wood", "Earth" },
        { "Earth", "Water" },
        { "Water", "Fire" },
        { "Light", "Dark" },
        { "Dark", "Light" }
    };

    private const float WEAKNESS_MULTIPLIER = 1.2f; // +20% damage

    /// <summary>
    /// Check if attacker's element is effective against target's weakness.
    /// Returns the damage multiplier (1.0 if no weakness match, 1.2 if weakness matched).
    /// </summary>
    public static float GetWeaknessMultiplier(string attackerElement, string targetWeakness)
    {
        if (string.IsNullOrWhiteSpace(attackerElement) || string.IsNullOrWhiteSpace(targetWeakness))
            return 1.0f;

        string normalizedAttackerElement = attackerElement.Trim();
        string normalizedTargetWeakness = targetWeakness.Trim();

        // Check if attacker's element matches the target's weakness
        if (string.Equals(normalizedAttackerElement, normalizedTargetWeakness, System.StringComparison.OrdinalIgnoreCase))
            return WEAKNESS_MULTIPLIER;

        return 1.0f;
    }

    /// <summary>
    /// Get what element counters the given element.
    /// </summary>
    public static string GetCounterElement(string element)
    {
        if (string.IsNullOrWhiteSpace(element))
            return null;

        string normalized = element.Trim();
        if (WeaknessMap.TryGetValue(normalized, out string counterElement))
            return counterElement;

        return null;
    }

    /// <summary>
    /// Check if a given element is valid.
    /// </summary>
    public static bool IsValidElement(string element)
    {
        if (string.IsNullOrWhiteSpace(element))
            return false;

        return WeaknessMap.ContainsKey(element.Trim());
    }
}
