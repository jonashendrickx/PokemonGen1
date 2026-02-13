using PokemonGen1.Core.World;

namespace PokemonGen1.Game.Entities;

public class PlayerEntity
{
    private const float MoveSpeed = 6.67f; // tiles per second (~0.15s per tile)

    public int TileX { get; set; }
    public int TileY { get; set; }
    public float VisualX { get; private set; }
    public float VisualY { get; private set; }
    public Direction Facing { get; set; } = Direction.Down;
    public int AnimFrame { get; private set; }
    public bool IsMoving { get; private set; }

    private int _targetX, _targetY;
    private float _moveProgress;

    public PlayerEntity(int tileX, int tileY, Direction facing = Direction.Down)
    {
        TileX = tileX;
        TileY = tileY;
        VisualX = tileX;
        VisualY = tileY;
        Facing = facing;
    }

    public bool TryMove(Direction direction, MapData map)
    {
        if (IsMoving) return false;

        Facing = direction;

        int nx = TileX, ny = TileY;
        switch (direction)
        {
            case Direction.Up: ny--; break;
            case Direction.Down: ny++; break;
            case Direction.Left: nx--; break;
            case Direction.Right: nx++; break;
        }

        // Bounds check
        if (nx < 0 || nx >= map.Width || ny < 0 || ny >= map.Height)
            return false;

        // Collision check
        if (map.CollisionLayer[ny * map.Width + nx])
            return false;

        // NPC blocking check
        foreach (var evt in map.Events)
        {
            if (evt.X == nx && evt.Y == ny && evt.Type == "npc")
                return false;
        }

        _targetX = nx;
        _targetY = ny;
        _moveProgress = 0;
        IsMoving = true;
        return true;
    }

    public void Update(float dt)
    {
        if (!IsMoving) return;

        _moveProgress += dt * MoveSpeed;

        // Toggle walk frame at midpoint
        if (_moveProgress >= 0.5f)
            AnimFrame = 1;

        if (_moveProgress >= 1f)
        {
            // Snap to target
            TileX = _targetX;
            TileY = _targetY;
            VisualX = TileX;
            VisualY = TileY;
            IsMoving = false;
            AnimFrame = 0;
        }
        else
        {
            // Lerp
            VisualX = TileX + (_targetX - TileX) * _moveProgress;
            VisualY = TileY + (_targetY - TileY) * _moveProgress;
        }
    }

    public (int x, int y) GetFacingTile()
    {
        return Facing switch
        {
            Direction.Up => (TileX, TileY - 1),
            Direction.Down => (TileX, TileY + 1),
            Direction.Left => (TileX - 1, TileY),
            Direction.Right => (TileX + 1, TileY),
            _ => (TileX, TileY)
        };
    }

    public void SnapToPosition(int x, int y)
    {
        TileX = x;
        TileY = y;
        VisualX = x;
        VisualY = y;
        IsMoving = false;
        _moveProgress = 0;
        AnimFrame = 0;
    }
}
