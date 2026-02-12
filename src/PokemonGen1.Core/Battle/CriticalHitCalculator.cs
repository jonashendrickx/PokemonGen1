namespace PokemonGen1.Core.Battle;

public static class CriticalHitCalculator
{
    /// <summary>
    /// Gen 1 critical hit rate: baseSpeed / 512.
    /// High crit moves (Slash, Razor Leaf, Crabhammer, Karate Chop): baseSpeed * 8 / 512 (capped at 255/256).
    /// Focus Energy bug in Gen 1: it divides crit rate by 4 instead of multiplying.
    /// </summary>
    public static bool RollCritical(int baseSpeed, bool highCritRate, bool hasFocusEnergy, Random rng)
    {
        int threshold;
        if (highCritRate)
        {
            threshold = Math.Min(baseSpeed * 8, 255);
        }
        else
        {
            threshold = baseSpeed / 2;
        }

        // Gen 1 Focus Energy bug: divides by 4
        if (hasFocusEnergy)
        {
            threshold /= 4;
        }

        threshold = Math.Clamp(threshold, 0, 255);
        return rng.Next(256) < threshold;
    }
}
