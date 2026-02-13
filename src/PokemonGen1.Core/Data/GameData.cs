using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonGen1.Core.Evolution;
using PokemonGen1.Core.Items;
using PokemonGen1.Core.Moves;
using PokemonGen1.Core.Pokemon;
using PokemonGen1.Core.Trainers;
using PokemonGen1.Core.Types;
using PokemonGen1.Core.World;

namespace PokemonGen1.Core.Data;

public class GameData
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public Dictionary<int, PokemonSpecies> Species { get; private set; } = new();
    public Dictionary<int, MoveData> Moves { get; private set; } = new();
    public TypeChart TypeChart { get; private set; } = null!;
    public List<EvolutionEntry> Evolutions { get; private set; } = new();
    public Dictionary<int, ItemData> Items { get; private set; } = new();
    public Dictionary<int, List<LearnsetEntry>> Learnsets { get; private set; } = new();
    public Dictionary<string, AreaData> Areas { get; private set; } = new();
    public Dictionary<string, WildEncounterTable> Encounters { get; private set; } = new();
    public Dictionary<int, TrainerData> Trainers { get; private set; } = new();
    public Dictionary<string, ShopData> Shops { get; private set; } = new();

    public PokemonSpecies GetSpecies(int dexNumber) => Species[dexNumber];
    public MoveData GetMove(int moveId) => Moves[moveId];
    public ItemData GetItem(int itemId) => Items[itemId];
    public AreaData? GetArea(string areaId) => Areas.TryGetValue(areaId, out var area) ? area : null;
    public TrainerData? GetTrainer(int trainerId) => Trainers.TryGetValue(trainerId, out var trainer) ? trainer : null;
    public WildEncounterTable? GetEncounterTable(string areaId) => Encounters.TryGetValue(areaId, out var table) ? table : null;
    public ShopData? GetShop(string shopId) => Shops.TryGetValue(shopId, out var shop) ? shop : null;

    public List<EvolutionEntry> GetEvolutions(int speciesId) =>
        Evolutions.Where(e => e.FromSpeciesId == speciesId).ToList();

    public List<LearnsetEntry> GetLearnset(int speciesId) =>
        Learnsets.TryGetValue(speciesId, out var list) ? list : new List<LearnsetEntry>();

    /// <summary>
    /// Get the moves a Pokemon would know at a given level (last 4 learned).
    /// </summary>
    public MoveInstance[] GetDefaultMoves(int speciesId, int level)
    {
        var learnset = GetLearnset(speciesId);
        var learned = learnset
            .Where(e => e.Level <= level)
            .OrderByDescending(e => e.Level)
            .Take(4)
            .Select(e => new MoveInstance
            {
                MoveId = e.MoveId,
                CurrentPP = Moves[e.MoveId].MaxPP,
                MaxPP = Moves[e.MoveId].MaxPP
            })
            .ToArray();
        return learned;
    }

    public static GameData LoadFromDirectory(string dataDir)
    {
        var data = new GameData();

        // Load species
        var speciesJson = File.ReadAllText(Path.Combine(dataDir, "pokemon", "species.json"));
        var speciesList = JsonSerializer.Deserialize<List<PokemonSpecies>>(speciesJson, Options)!;
        data.Species = speciesList.ToDictionary(s => s.DexNumber);

        // Load moves
        var movesJson = File.ReadAllText(Path.Combine(dataDir, "moves", "moves.json"));
        var movesList = JsonSerializer.Deserialize<List<MoveData>>(movesJson, Options)!;
        data.Moves = movesList.ToDictionary(m => m.Id);

        // Load type chart
        var typeChartJson = File.ReadAllText(Path.Combine(dataDir, "types", "type_chart.json"));
        data.TypeChart = TypeChart.LoadFromJson(typeChartJson);

        // Load evolutions
        var evoJson = File.ReadAllText(Path.Combine(dataDir, "pokemon", "evolution.json"));
        data.Evolutions = JsonSerializer.Deserialize<List<EvolutionEntry>>(evoJson, Options)!;

        // Load items
        var itemsPath = Path.Combine(dataDir, "items", "items.json");
        if (File.Exists(itemsPath))
        {
            var itemsJson = File.ReadAllText(itemsPath);
            var itemsList = JsonSerializer.Deserialize<List<ItemData>>(itemsJson, Options)!;
            data.Items = itemsList.ToDictionary(i => i.Id);
        }

        // Load learnsets
        var learnsetsPath = Path.Combine(dataDir, "pokemon", "learnsets.json");
        if (File.Exists(learnsetsPath))
        {
            var learnsetsJson = File.ReadAllText(learnsetsPath);
            data.Learnsets = JsonSerializer.Deserialize<Dictionary<int, List<LearnsetEntry>>>(learnsetsJson, Options)!;
        }

        // Load world data
        var worldDir = Path.Combine(dataDir, "world");
        if (Directory.Exists(worldDir))
        {
            var areasPath = Path.Combine(worldDir, "areas.json");
            if (File.Exists(areasPath))
            {
                var areasJson = File.ReadAllText(areasPath);
                var areasList = JsonSerializer.Deserialize<List<AreaData>>(areasJson, Options)!;
                data.Areas = areasList.ToDictionary(a => a.Id);
            }

            var encountersPath = Path.Combine(worldDir, "encounters.json");
            if (File.Exists(encountersPath))
            {
                var encountersJson = File.ReadAllText(encountersPath);
                var encountersList = JsonSerializer.Deserialize<List<WildEncounterTable>>(encountersJson, Options)!;
                data.Encounters = encountersList.ToDictionary(e => e.AreaId);
            }

            var trainersPath = Path.Combine(worldDir, "trainers.json");
            if (File.Exists(trainersPath))
            {
                var trainersJson = File.ReadAllText(trainersPath);
                var trainersList = JsonSerializer.Deserialize<List<TrainerData>>(trainersJson, Options)!;
                data.Trainers = trainersList.ToDictionary(t => t.Id);
            }

            var shopsPath = Path.Combine(worldDir, "shops.json");
            if (File.Exists(shopsPath))
            {
                var shopsJson = File.ReadAllText(shopsPath);
                var shopsList = JsonSerializer.Deserialize<List<ShopData>>(shopsJson, Options)!;
                data.Shops = shopsList.ToDictionary(s => s.Id);
            }
        }

        return data;
    }
}

public class LearnsetEntry
{
    public int Level { get; set; }
    public int MoveId { get; set; }
}
