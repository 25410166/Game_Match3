using System;
using System.Collections.Generic;

public static class ElementResourceGainConfig
{
    public const int DefaultManaPerGem = 25;
    public const int DefaultRagePerGem = 25;

    private readonly struct ResourceGain
    {
        public readonly int ManaPerGem;
        public readonly int RagePerGem;

        public ResourceGain(int manaPerGem, int ragePerGem)
        {
            ManaPerGem = manaPerGem;
            RagePerGem = ragePerGem;
        }
    }

    private static readonly Dictionary<string, ResourceGain> GainByElement =
        new Dictionary<string, ResourceGain>(StringComparer.OrdinalIgnoreCase)
        {
            { "Fire", new ResourceGain(15, 40) },
            { "Metal", new ResourceGain(20, 15) },
            { "Wood", new ResourceGain(25, 10) },
            { "Earth", new ResourceGain(15, 20) },
            { "Water", new ResourceGain(40, 10) },
            { "Light", new ResourceGain(20, 15) },
            { "Dark", new ResourceGain(15, 30) },
            { "Leaf", new ResourceGain(25, 10) }
        };

    public static int GetManaPerGem(string element)
    {
        return TryGetGain(element, out ResourceGain gain) ? gain.ManaPerGem : DefaultManaPerGem;
    }

    public static int GetRagePerGem(string element)
    {
        return TryGetGain(element, out ResourceGain gain) ? gain.RagePerGem : DefaultRagePerGem;
    }

    public static void GetPerGemGains(string element, out int manaPerGem, out int ragePerGem)
    {
        if (TryGetGain(element, out ResourceGain gain))
        {
            manaPerGem = gain.ManaPerGem;
            ragePerGem = gain.RagePerGem;
            return;
        }

        manaPerGem = DefaultManaPerGem;
        ragePerGem = DefaultRagePerGem;
    }

    private static bool TryGetGain(string element, out ResourceGain gain)
    {
        if (string.IsNullOrWhiteSpace(element))
        {
            gain = default;
            return false;
        }

        return GainByElement.TryGetValue(element.Trim(), out gain);
    }
}
