namespace PokemonGen1.Core.World;

public class WildEncounterTable
{
    public string AreaId { get; set; } = "";
    public int EncounterRate { get; set; }
    public EncounterSlot[] GrassEncounters { get; set; } = Array.Empty<EncounterSlot>();
    public EncounterSlot[]? SurfEncounters { get; set; }
    public EncounterSlot[]? FishingEncounters { get; set; }
}

public class EncounterSlot
{
    public int SpeciesId { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public int Weight { get; set; }
}
