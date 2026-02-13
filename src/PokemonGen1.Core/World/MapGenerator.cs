using PokemonGen1.Core.Data;

namespace PokemonGen1.Core.World;

public class MapGenerator
{
    private readonly Random _rng;

    public MapGenerator(Random? rng = null)
    {
        _rng = rng ?? new Random();
    }

    public MapData Generate(AreaData area, GameData gameData)
    {
        return area.Type switch
        {
            AreaType.Town or AreaType.City => GenerateTown(area, gameData),
            AreaType.Route => GenerateRoute(area, gameData),
            AreaType.Cave or AreaType.DungeonFloor => GenerateCave(area, gameData),
            AreaType.Building => area.Id.Contains("gym") ? GenerateGym(area, gameData) : GenerateBuilding(area, gameData),
            _ => GenerateGeneric(area, gameData)
        };
    }

    // ================================================================
    // TOWN / CITY
    // ================================================================
    private MapData GenerateTown(AreaData area, GameData gameData)
    {
        int w = 20, h = 15;
        var map = CreateMap(area.Id, w, h);

        // Fill with grass
        FillLayer(map.GroundLayer, w, 0, 0, w, h, TileType.Grass);

        // Tree border
        DrawBorder(map, TileType.Tree);

        // Main vertical path through center
        int pathX = w / 2;
        FillLayer(map.GroundLayer, w, pathX - 1, 1, 3, h - 2, TileType.Path);

        // Horizontal cross path
        int crossY = h / 2;
        FillLayer(map.GroundLayer, w, 1, crossY, w - 2, 1, TileType.Path);

        // Pokemon Center (left side)
        if (area.HasPokemonCenter)
        {
            PlaceBuilding(map, 3, 2, 5, 4, TileType.BuildingWall, TileType.Door);
            // Sign next to PC
            SetTile(map.ObjectLayer, w, 2, 6, TileType.Sign);
            map.CollisionLayer[6 * w + 2] = true;
            map.Events = map.Events.Append(new EventTrigger
            {
                X = 2, Y = 6, Type = "sign",
                Dialog = new[] { "POKEMON CENTER" }
            }).ToArray();
            // Nurse inside - warp leads to PC interior
            // For simplicity, heal happens at the door
            map.Events = map.Events.Append(new EventTrigger
            {
                X = 5, Y = 6, Type = "npc",
                Facing = Direction.Down,
                Dialog = new[] { "Welcome to the Pokemon Center!", "Your Pokemon will be fully healed." },
                SpriteColor = "pink"
            }).ToArray();
        }

        // Poke Mart (right side)
        if (area.HasPokeMart)
        {
            PlaceBuilding(map, 12, 2, 5, 4, TileType.BuildingWall, TileType.Door);
            SetTile(map.ObjectLayer, w, 17, 6, TileType.Sign);
            map.CollisionLayer[6 * w + 17] = true;
            map.Events = map.Events.Append(new EventTrigger
            {
                X = 17, Y = 6, Type = "sign",
                Dialog = new[] { "POKE MART" }
            }).ToArray();
            map.Events = map.Events.Append(new EventTrigger
            {
                X = 14, Y = 6, Type = "npc",
                Facing = Direction.Down,
                Dialog = new[] { "Welcome to the Poke Mart!" },
                SpriteColor = "green",
                ScriptId = "shop"
            }).ToArray();
        }

        // Place exits for connections
        PlaceEdgeExits(map, area);

        // Place trainers
        PlaceTrainers(map, area, gameData);

        // Place items
        PlaceItems(map, area);

        // Area name sign at entrance
        map.Events = map.Events.Append(new EventTrigger
        {
            X = pathX, Y = h - 2, Type = "sign",
            Dialog = new[] { area.Name }
        }).ToArray();

        return map;
    }

    // ================================================================
    // ROUTE
    // ================================================================
    private MapData GenerateRoute(AreaData area, GameData gameData)
    {
        bool isVertical = HasVerticalConnections(area);
        int w = isVertical ? 15 : 25;
        int h = isVertical ? 25 : 15;
        var map = CreateMap(area.Id, w, h);

        FillLayer(map.GroundLayer, w, 0, 0, w, h, TileType.Grass);
        DrawBorder(map, TileType.Tree);

        if (isVertical)
        {
            // Vertical path (3 tiles wide) through center
            int px = w / 2 - 1;
            FillLayer(map.GroundLayer, w, px, 0, 3, h, TileType.Path);

            // Tall grass patches on sides
            if (area.HasWildEncounters)
            {
                // Left patches
                PlaceTallGrass(map, 2, 3, 4, 5);
                PlaceTallGrass(map, 2, 14, 4, 5);
                // Right patches
                PlaceTallGrass(map, w - 6, 6, 4, 5);
                PlaceTallGrass(map, w - 6, 17, 4, 5);
            }
        }
        else
        {
            // Horizontal path
            int py = h / 2 - 1;
            FillLayer(map.GroundLayer, w, 0, py, w, 3, TileType.Path);

            if (area.HasWildEncounters)
            {
                PlaceTallGrass(map, 3, 2, 5, 4);
                PlaceTallGrass(map, 14, 2, 5, 4);
                PlaceTallGrass(map, 7, h - 6, 5, 4);
                PlaceTallGrass(map, 17, h - 6, 5, 4);
            }
        }

        PlaceEdgeExits(map, area);
        PlaceTrainers(map, area, gameData);
        PlaceItems(map, area);

        return map;
    }

    // ================================================================
    // CAVE / DUNGEON
    // ================================================================
    private MapData GenerateCave(AreaData area, GameData gameData)
    {
        int w = 18, h = 15;
        var map = CreateMap(area.Id, w, h);

        // Fill with void
        FillLayer(map.GroundLayer, w, 0, 0, w, h, TileType.Void);

        // Rock wall border
        DrawBorder(map, TileType.RockWall);

        // Carve out cave floor
        FillLayer(map.GroundLayer, w, 2, 2, w - 4, h - 4, TileType.Path);

        // Random rock pillars
        for (int i = 0; i < 6; i++)
        {
            int rx = _rng.Next(3, w - 3);
            int ry = _rng.Next(3, h - 3);
            SetTile(map.ObjectLayer, w, rx, ry, TileType.RockWall);
            map.CollisionLayer[ry * w + rx] = true;
        }

        // Entire walkable floor is encounter zone
        if (area.HasWildEncounters)
        {
            for (int y = 2; y < h - 2; y++)
                for (int x = 2; x < w - 2; x++)
                    if (!map.CollisionLayer[y * w + x])
                        map.EncounterLayer[y * w + x] = true;
        }

        PlaceEdgeExits(map, area);
        PlaceTrainers(map, area, gameData);
        PlaceItems(map, area);

        return map;
    }

    // ================================================================
    // GYM
    // ================================================================
    private MapData GenerateGym(AreaData area, GameData gameData)
    {
        int w = 10, h = 14;
        var map = CreateMap(area.Id, w, h);

        // Interior floor
        FillLayer(map.GroundLayer, w, 0, 0, w, h, TileType.Carpet);
        DrawBorder(map, TileType.BuildingWall);

        // Central path from door to leader
        FillLayer(map.GroundLayer, w, 4, 1, 2, h - 2, TileType.Path);

        // Door at bottom center
        SetTile(map.GroundLayer, w, 4, h - 1, TileType.Door);
        SetTile(map.GroundLayer, w, 5, h - 1, TileType.Door);
        map.CollisionLayer[(h - 1) * w + 4] = false;
        map.CollisionLayer[(h - 1) * w + 5] = false;

        // Place trainers linearly along the path
        var trainerIds = area.Trainers;
        int gymLeaderId = -1;
        var regularTrainers = new List<int>();

        foreach (var tid in trainerIds)
        {
            var trainer = gameData.GetTrainer(tid);
            if (trainer != null && trainer.IsGymLeader)
                gymLeaderId = tid;
            else
                regularTrainers.Add(tid);
        }

        // Gym leader at back
        if (gymLeaderId >= 0)
        {
            var leader = gameData.GetTrainer(gymLeaderId);
            if (leader != null)
            {
                map.Events = map.Events.Append(new EventTrigger
                {
                    X = 5, Y = 2, Type = "npc",
                    TrainerId = gymLeaderId,
                    Facing = Direction.Down,
                    Dialog = leader.BeforeBattleDialog,
                    SpriteColor = "red"
                }).ToArray();
                map.CollisionLayer[2 * w + 5] = true;
            }
        }

        // Regular trainers spaced along the path
        for (int i = 0; i < regularTrainers.Count; i++)
        {
            var trainer = gameData.GetTrainer(regularTrainers[i]);
            if (trainer == null) continue;

            int ty = 4 + i * 3;
            if (ty >= h - 2) ty = h - 3;
            int tx = (i % 2 == 0) ? 3 : 6; // Alternate sides

            map.Events = map.Events.Append(new EventTrigger
            {
                X = tx, Y = ty, Type = "npc",
                TrainerId = regularTrainers[i],
                Facing = Direction.Down,
                Dialog = trainer.BeforeBattleDialog,
                SpriteColor = "blue"
            }).ToArray();
            map.CollisionLayer[ty * w + tx] = true;
        }

        // Add exit connection (back to parent area)
        AddDoorExit(map, area);

        return map;
    }

    // ================================================================
    // BUILDING (PC, Mart, generic)
    // ================================================================
    private MapData GenerateBuilding(AreaData area, GameData gameData)
    {
        int w = 10, h = 10;
        var map = CreateMap(area.Id, w, h);

        FillLayer(map.GroundLayer, w, 0, 0, w, h, TileType.FloorTile);
        DrawBorder(map, TileType.BuildingWall);

        // Door at bottom
        SetTile(map.GroundLayer, w, w / 2, h - 1, TileType.Door);
        map.CollisionLayer[(h - 1) * w + w / 2] = false;

        PlaceTrainers(map, area, gameData);
        PlaceItems(map, area);
        AddDoorExit(map, area);

        return map;
    }

    // ================================================================
    // GENERIC (fallback for Special areas etc.)
    // ================================================================
    private MapData GenerateGeneric(AreaData area, GameData gameData)
    {
        int w = 15, h = 15;
        var map = CreateMap(area.Id, w, h);

        FillLayer(map.GroundLayer, w, 0, 0, w, h, TileType.Grass);
        DrawBorder(map, TileType.Tree);

        // Path cross
        FillLayer(map.GroundLayer, w, w / 2 - 1, 0, 3, h, TileType.Path);
        FillLayer(map.GroundLayer, w, 0, h / 2, w, 1, TileType.Path);

        if (area.HasWildEncounters)
        {
            PlaceTallGrass(map, 2, 2, 4, 4);
            PlaceTallGrass(map, w - 6, h - 6, 4, 4);
        }

        PlaceEdgeExits(map, area);
        PlaceTrainers(map, area, gameData);
        PlaceItems(map, area);

        return map;
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private MapData CreateMap(string id, int w, int h)
    {
        int size = w * h;
        return new MapData
        {
            Id = id,
            Width = w,
            Height = h,
            GroundLayer = new int[size],
            ObjectLayer = new int[size],
            OverheadLayer = new int[size],
            CollisionLayer = new bool[size],
            EncounterLayer = new bool[size],
            Warps = Array.Empty<WarpData>(),
            Connections = Array.Empty<MapConnection>(),
            Events = Array.Empty<EventTrigger>()
        };
    }

    private void FillLayer(int[] layer, int mapWidth, int x, int y, int w, int h, TileType tile)
    {
        int id = (int)tile;
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
            {
                int idx = (y + dy) * mapWidth + (x + dx);
                if (idx >= 0 && idx < layer.Length)
                    layer[idx] = id;
            }
    }

    private void SetTile(int[] layer, int mapWidth, int x, int y, TileType tile)
    {
        int idx = y * mapWidth + x;
        if (idx >= 0 && idx < layer.Length)
            layer[idx] = (int)tile;
    }

    private void DrawBorder(MapData map, TileType tile)
    {
        int w = map.Width, h = map.Height;
        for (int x = 0; x < w; x++)
        {
            SetTile(map.ObjectLayer, w, x, 0, tile);
            SetTile(map.ObjectLayer, w, x, h - 1, tile);
            map.CollisionLayer[x] = true;
            map.CollisionLayer[(h - 1) * w + x] = true;
        }
        for (int y = 0; y < h; y++)
        {
            SetTile(map.ObjectLayer, w, 0, y, tile);
            SetTile(map.ObjectLayer, w, w - 1, y, tile);
            map.CollisionLayer[y * w] = true;
            map.CollisionLayer[y * w + w - 1] = true;
        }
    }

    private void PlaceTallGrass(MapData map, int x, int y, int w, int h)
    {
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
            {
                int px = x + dx, py = y + dy;
                if (px <= 0 || px >= map.Width - 1 || py <= 0 || py >= map.Height - 1) continue;
                int idx = py * map.Width + px;
                if (map.CollisionLayer[idx]) continue;
                map.ObjectLayer[idx] = (int)TileType.TallGrass;
                map.EncounterLayer[idx] = true;
            }
    }

    private void PlaceBuilding(MapData map, int x, int y, int bw, int bh,
        TileType wallTile, TileType doorTile)
    {
        int w = map.Width;
        // Walls
        for (int dy = 0; dy < bh; dy++)
            for (int dx = 0; dx < bw; dx++)
            {
                int px = x + dx, py = y + dy;
                SetTile(map.ObjectLayer, w, px, py, wallTile);
                map.CollisionLayer[py * w + px] = true;
            }
        // Door at bottom center
        int doorX = x + bw / 2;
        int doorY = y + bh;
        SetTile(map.ObjectLayer, w, doorX, doorY, doorTile);
        map.CollisionLayer[doorY * w + doorX] = false;
    }

    private void PlaceEdgeExits(MapData map, AreaData area)
    {
        int w = map.Width, h = map.Height;
        var connections = new List<MapConnection>();

        foreach (var conn in area.Connections)
        {
            var dir = ParseDirection(conn.Direction);
            if (dir == null) continue;

            connections.Add(new MapConnection
            {
                Direction = dir.Value,
                TargetMapId = conn.AreaId,
                Offset = 0
            });

            // Clear border tiles for exit
            int mid;
            switch (dir.Value)
            {
                case Direction.Up:
                    mid = w / 2;
                    for (int i = -1; i <= 1; i++)
                    {
                        int x = mid + i;
                        if (x >= 0 && x < w)
                        {
                            map.ObjectLayer[x] = (int)TileType.Path;
                            map.CollisionLayer[x] = false;
                            map.GroundLayer[x] = (int)TileType.Path;
                        }
                    }
                    break;
                case Direction.Down:
                    mid = w / 2;
                    for (int i = -1; i <= 1; i++)
                    {
                        int x = mid + i;
                        if (x >= 0 && x < w)
                        {
                            map.ObjectLayer[(h - 1) * w + x] = (int)TileType.Path;
                            map.CollisionLayer[(h - 1) * w + x] = false;
                            map.GroundLayer[(h - 1) * w + x] = (int)TileType.Path;
                        }
                    }
                    break;
                case Direction.Left:
                    mid = h / 2;
                    for (int i = -1; i <= 1; i++)
                    {
                        int y = mid + i;
                        if (y >= 0 && y < h)
                        {
                            map.ObjectLayer[y * w] = (int)TileType.Path;
                            map.CollisionLayer[y * w] = false;
                            map.GroundLayer[y * w] = (int)TileType.Path;
                        }
                    }
                    break;
                case Direction.Right:
                    mid = h / 2;
                    for (int i = -1; i <= 1; i++)
                    {
                        int y = mid + i;
                        if (y >= 0 && y < h)
                        {
                            map.ObjectLayer[y * w + w - 1] = (int)TileType.Path;
                            map.CollisionLayer[y * w + w - 1] = false;
                            map.GroundLayer[y * w + w - 1] = (int)TileType.Path;
                        }
                    }
                    break;
            }
        }

        map.Connections = connections.ToArray();
    }

    private void AddDoorExit(MapData map, AreaData area)
    {
        // Find the first connection that looks like an exit
        var parentConn = area.Connections.FirstOrDefault();
        if (parentConn == null) return;

        int doorX = map.Width / 2;
        int doorY = map.Height - 1;

        map.Warps = map.Warps.Append(new WarpData
        {
            X = doorX,
            Y = doorY,
            TargetMapId = parentConn.AreaId,
            TargetX = -1, // -1 means "find a good spot"
            TargetY = -1
        }).ToArray();
    }

    private void PlaceTrainers(MapData map, AreaData area, GameData gameData)
    {
        int placed = 0;
        foreach (var trainerId in area.Trainers)
        {
            var trainer = gameData.GetTrainer(trainerId);
            if (trainer == null) continue;
            if (trainer.IsGymLeader) continue; // Gym leaders placed specially in GenerateGym

            var pos = FindOpenPosition(map, placed);
            map.Events = map.Events.Append(new EventTrigger
            {
                X = pos.x, Y = pos.y, Type = "npc",
                TrainerId = trainerId,
                Facing = Direction.Down,
                Dialog = trainer.BeforeBattleDialog,
                SpriteColor = trainer.IsGymLeader ? "red" : "blue"
            }).ToArray();
            map.CollisionLayer[pos.y * map.Width + pos.x] = true;
            placed++;
        }
    }

    private void PlaceItems(MapData map, AreaData area)
    {
        int placed = 0;
        foreach (var item in area.Items)
        {
            var pos = FindOpenPosition(map, placed + 100); // Different seed offset
            map.Events = map.Events.Append(new EventTrigger
            {
                X = pos.x, Y = pos.y, Type = "item",
                ItemId = item.ItemId
            }).ToArray();
            placed++;
        }
    }

    private (int x, int y) FindOpenPosition(MapData map, int seed)
    {
        int w = map.Width, h = map.Height;
        // Deterministic placement based on seed for consistency
        var localRng = new Random(map.Id.GetHashCode() + seed);

        for (int attempt = 0; attempt < 100; attempt++)
        {
            int x = localRng.Next(2, w - 2);
            int y = localRng.Next(2, h - 2);
            int idx = y * w + x;

            if (!map.CollisionLayer[idx] &&
                map.GroundLayer[idx] == (int)TileType.Path &&
                !map.Events.Any(e => e.X == x && e.Y == y))
            {
                return (x, y);
            }
        }
        // Fallback: try any non-blocked tile
        for (int attempt = 0; attempt < 100; attempt++)
        {
            int x = localRng.Next(2, w - 2);
            int y = localRng.Next(2, h - 2);
            if (!map.CollisionLayer[y * w + x] &&
                !map.Events.Any(e => e.X == x && e.Y == y))
                return (x, y);
        }
        return (w / 2, h / 2);
    }

    private bool HasVerticalConnections(AreaData area)
    {
        foreach (var conn in area.Connections)
        {
            var dir = conn.Direction.ToLowerInvariant();
            if (dir.Contains("north") || dir.Contains("south"))
                return true;
        }
        return false;
    }

    private Direction? ParseDirection(string dir)
    {
        var lower = dir.ToLowerInvariant();
        if (lower.Contains("north") || lower == "up") return Direction.Up;
        if (lower.Contains("south") || lower == "down") return Direction.Down;
        if (lower.Contains("west") || lower == "left") return Direction.Left;
        if (lower.Contains("east") || lower == "right") return Direction.Right;
        return null;
    }
}
