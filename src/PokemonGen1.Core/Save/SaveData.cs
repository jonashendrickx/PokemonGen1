using PokemonGen1.Core.Items;
using PokemonGen1.Core.Pokemon;
using PokemonGen1.Core.World;

namespace PokemonGen1.Core.Save;

public class SaveData
{
    public string PlayerName { get; set; } = "RED";
    public string RivalName { get; set; } = "BLUE";
    public PokemonInstance[] Party { get; set; } = Array.Empty<PokemonInstance>();
    public List<PokemonInstance> PcBoxes { get; set; } = new();
    public Inventory Inventory { get; set; } = new();
    public string CurrentMapId { get; set; } = "pallet_town";
    public int PlayerX { get; set; }
    public int PlayerY { get; set; }
    public Direction PlayerFacing { get; set; }
    public int BadgeCount { get; set; }
    public bool[] BadgesObtained { get; set; } = new bool[8];
    public HashSet<int> DefeatedTrainerIds { get; set; } = new();
    public HashSet<int> CollectedItemIds { get; set; } = new();
    public HashSet<int> PokedexSeen { get; set; } = new();
    public HashSet<int> PokedexCaught { get; set; } = new();
    public int Money { get; set; } = 3000;
    public TimeSpan PlayTime { get; set; }
    public HashSet<string> StoryFlags { get; set; } = new();
}
