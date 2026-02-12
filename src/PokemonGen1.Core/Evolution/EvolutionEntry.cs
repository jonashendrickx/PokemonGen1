namespace PokemonGen1.Core.Evolution;

public enum EvolutionMethod { LevelUp, Stone, Trade }

public class EvolutionEntry
{
    public int FromSpeciesId { get; set; }
    public int ToSpeciesId { get; set; }
    public EvolutionMethod Method { get; set; }
    public int? Level { get; set; }
    public string? ItemId { get; set; }
}
