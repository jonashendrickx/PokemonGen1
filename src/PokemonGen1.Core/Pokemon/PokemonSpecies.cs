using PokemonGen1.Core.Types;

namespace PokemonGen1.Core.Pokemon;

public enum GrowthRate { Fast, MediumFast, MediumSlow, Slow }

public class PokemonSpecies
{
    public int DexNumber { get; set; }
    public string Name { get; set; } = "";
    public PokemonType Type1 { get; set; }
    public PokemonType? Type2 { get; set; }
    public int BaseHp { get; set; }
    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    public int BaseSpecial { get; set; }
    public int BaseSpeed { get; set; }
    public int CatchRate { get; set; }
    public int BaseExpYield { get; set; }
    public GrowthRate GrowthRate { get; set; }
    public string Category { get; set; } = "";
    public float HeightM { get; set; }
    public float WeightKg { get; set; }
}
