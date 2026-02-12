using PokemonGen1.Core.Battle;
using PokemonGen1.Core.Moves;

namespace PokemonGen1.Core.Pokemon;

public class PokemonInstance
{
    public int SpeciesId { get; set; }
    public string? Nickname { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }

    // Gen 1 DVs (Determinant Values), range 0-15
    public int AttackDV { get; set; }
    public int DefenseDV { get; set; }
    public int SpeedDV { get; set; }
    public int SpecialDV { get; set; }

    // HP DV is derived from other DVs
    public int HpDV => 8 * (AttackDV & 1) + 4 * (DefenseDV & 1)
                     + 2 * (SpeedDV & 1) + 1 * (SpecialDV & 1);

    // Gen 1 Stat Experience (0-65535)
    public int HpStatExp { get; set; }
    public int AttackStatExp { get; set; }
    public int DefenseStatExp { get; set; }
    public int SpeedStatExp { get; set; }
    public int SpecialStatExp { get; set; }

    public int CurrentHp { get; set; }
    public StatusCondition Status { get; set; } = StatusCondition.None;

    public MoveInstance[] Moves { get; set; } = Array.Empty<MoveInstance>();

    public string OtName { get; set; } = "";
    public int OtId { get; set; }

    public bool IsFainted => CurrentHp <= 0;

    public int MaxHp(PokemonSpecies species) =>
        StatCalculator.CalculateHp(species.BaseHp, HpDV, HpStatExp, Level);

    public int Attack(PokemonSpecies species) =>
        StatCalculator.CalculateStat(species.BaseAttack, AttackDV, AttackStatExp, Level);

    public int Defense(PokemonSpecies species) =>
        StatCalculator.CalculateStat(species.BaseDefense, DefenseDV, DefenseStatExp, Level);

    public int Special(PokemonSpecies species) =>
        StatCalculator.CalculateStat(species.BaseSpecial, SpecialDV, SpecialStatExp, Level);

    public int Speed(PokemonSpecies species) =>
        StatCalculator.CalculateStat(species.BaseSpeed, SpeedDV, SpeedStatExp, Level);

    /// <summary>
    /// Create a new Pokemon with random DVs at a given level with default moves.
    /// </summary>
    public static PokemonInstance Create(PokemonSpecies species, int level, Random? rng = null)
    {
        var r = rng ?? Random.Shared;
        var pokemon = new PokemonInstance
        {
            SpeciesId = species.DexNumber,
            Level = level,
            AttackDV = r.Next(16),
            DefenseDV = r.Next(16),
            SpeedDV = r.Next(16),
            SpecialDV = r.Next(16),
            OtName = "PLAYER",
            OtId = r.Next(65536)
        };
        pokemon.CurrentHp = pokemon.MaxHp(species);
        pokemon.Experience = StatCalculator.ExperienceForLevel(species.GrowthRate, level);
        return pokemon;
    }
}
