namespace PokemonGen1.Core.World;

public enum Direction { Up, Down, Left, Right }

public class MapData
{
    public string Id { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public int[] GroundLayer { get; set; } = Array.Empty<int>();
    public int[] ObjectLayer { get; set; } = Array.Empty<int>();
    public int[] OverheadLayer { get; set; } = Array.Empty<int>();
    public bool[] CollisionLayer { get; set; } = Array.Empty<bool>();
    public bool[] EncounterLayer { get; set; } = Array.Empty<bool>();
    public string TilesetId { get; set; } = "";
    public WarpData[] Warps { get; set; } = Array.Empty<WarpData>();
    public MapConnection[] Connections { get; set; } = Array.Empty<MapConnection>();
    public EventTrigger[] Events { get; set; } = Array.Empty<EventTrigger>();
    public string? WildEncounterTableId { get; set; }
    public string? MusicId { get; set; }
}

public class WarpData
{
    public int X { get; set; }
    public int Y { get; set; }
    public string TargetMapId { get; set; } = "";
    public int TargetX { get; set; }
    public int TargetY { get; set; }
}

public class MapConnection
{
    public Direction Direction { get; set; }
    public string TargetMapId { get; set; } = "";
    public int Offset { get; set; }
}

public class EventTrigger
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Type { get; set; } = ""; // "npc", "sign", "item", "script"
    public string? SpriteId { get; set; }
    public string[] Dialog { get; set; } = Array.Empty<string>();
    public Direction? Facing { get; set; }
    public string? ScriptId { get; set; }
    public int? ItemId { get; set; }
}
