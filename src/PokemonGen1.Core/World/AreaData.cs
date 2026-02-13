namespace PokemonGen1.Core.World;

public enum AreaType { Town, City, Route, Cave, Building, DungeonFloor, Special }

public class AreaData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public AreaType Type { get; set; }
    public string Description { get; set; } = "";
    public AreaConnection[] Connections { get; set; } = Array.Empty<AreaConnection>();
    public bool HasWildEncounters { get; set; }
    public bool HasPokemonCenter { get; set; }
    public bool HasPokeMart { get; set; }
    public string? ShopId { get; set; }
    public int[] Trainers { get; set; } = Array.Empty<int>();
    public AreaItem[] Items { get; set; } = Array.Empty<AreaItem>();
}

public class AreaConnection
{
    public string AreaId { get; set; } = "";
    public string Direction { get; set; } = "";
    public string? RequiredFlag { get; set; }
}

public class AreaItem
{
    public int ItemId { get; set; }
    public int Quantity { get; set; } = 1;
    public bool Hidden { get; set; }
    public string? RequiredFlag { get; set; }
}
