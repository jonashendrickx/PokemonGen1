namespace PokemonGen1.Core.Pokemon;

public static class StatCalculator
{
    /// <summary>
    /// Gen 1 HP formula:
    /// floor(((Base + DV) * 2 + floor(ceil(sqrt(StatExp)) / 4)) * Level / 100) + Level + 10
    /// </summary>
    public static int CalculateHp(int baseStat, int dv, int statExp, int level)
    {
        int evBonus = (int)(Math.Ceiling(Math.Sqrt(statExp)) / 4.0);
        return ((baseStat + dv) * 2 + evBonus) * level / 100 + level + 10;
    }

    /// <summary>
    /// Gen 1 stat formula (Attack, Defense, Special, Speed):
    /// floor(((Base + DV) * 2 + floor(ceil(sqrt(StatExp)) / 4)) * Level / 100) + 5
    /// </summary>
    public static int CalculateStat(int baseStat, int dv, int statExp, int level)
    {
        int evBonus = (int)(Math.Ceiling(Math.Sqrt(statExp)) / 4.0);
        return ((baseStat + dv) * 2 + evBonus) * level / 100 + 5;
    }

    /// <summary>
    /// Experience required for a given level based on growth rate.
    /// </summary>
    public static int ExperienceForLevel(GrowthRate rate, int level)
    {
        int n = level;
        return rate switch
        {
            GrowthRate.Fast => 4 * n * n * n / 5,
            GrowthRate.MediumFast => n * n * n,
            GrowthRate.MediumSlow => 6 * n * n * n / 5 - 15 * n * n + 100 * n - 140,
            GrowthRate.Slow => 5 * n * n * n / 4,
            _ => n * n * n
        };
    }
}
