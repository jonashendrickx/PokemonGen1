using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.World;

namespace PokemonGen1.Game.Rendering;

public class ProceduralTileset
{
    public const int TileSize = 16;
    private const int TilesPerRow = 16;

    public Texture2D Texture { get; }

    public ProceduralTileset(GraphicsDevice device)
    {
        int texSize = TilesPerRow * TileSize;
        Texture = new Texture2D(device, texSize, texSize);
        var pixels = new Color[texSize * texSize];

        GenerateSolidTile(pixels, texSize, TileType.Void, new Color(16, 16, 24));
        GenerateGrassTile(pixels, texSize);
        GenerateTallGrassTile(pixels, texSize);
        GeneratePathTile(pixels, texSize);
        GenerateTreeTile(pixels, texSize);
        GenerateWaterTile(pixels, texSize);
        GenerateRockWallTile(pixels, texSize);
        GenerateSolidTile(pixels, texSize, TileType.BuildingWall, new Color(180, 150, 120));
        GenerateDoorTile(pixels, texSize);
        GenerateSolidTile(pixels, texSize, TileType.PCFloor, new Color(220, 200, 200));
        GenerateSolidTile(pixels, texSize, TileType.MartFloor, new Color(200, 220, 210));
        GenerateSignTile(pixels, texSize);
        GenerateSolidTile(pixels, texSize, TileType.Sand, new Color(220, 200, 150));
        GenerateFlowersTile(pixels, texSize);
        GenerateCheckerTile(pixels, texSize, TileType.FloorTile, new Color(200, 200, 200), new Color(190, 190, 190));
        GenerateCheckerTile(pixels, texSize, TileType.Carpet, new Color(160, 50, 50), new Color(140, 40, 40));
        GenerateSolidTile(pixels, texSize, TileType.Counter, new Color(140, 100, 60));

        Texture.SetData(pixels);
    }

    public Rectangle GetSourceRect(TileType type)
    {
        int id = (int)type;
        return new Rectangle(
            (id % TilesPerRow) * TileSize,
            (id / TilesPerRow) * TileSize,
            TileSize, TileSize);
    }

    public Rectangle GetSourceRect(int tileId)
    {
        return GetSourceRect((TileType)tileId);
    }

    private (int tx, int ty) TileOrigin(TileType type)
    {
        int id = (int)type;
        return ((id % TilesPerRow) * TileSize, (id / TilesPerRow) * TileSize);
    }

    private void SetPixel(Color[] pixels, int texSize, int x, int y, Color color)
    {
        if (x >= 0 && x < texSize && y >= 0 && y < texSize)
            pixels[y * texSize + x] = color;
    }

    private void FillRect(Color[] pixels, int texSize, int x, int y, int w, int h, Color color)
    {
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
                SetPixel(pixels, texSize, x + dx, y + dy, color);
    }

    private void GenerateSolidTile(Color[] pixels, int texSize, TileType type, Color baseColor)
    {
        var (tx, ty) = TileOrigin(type);
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                int noise = ((x + y) % 3 - 1) * 4;
                var col = new Color(
                    Math.Clamp(baseColor.R + noise, 0, 255),
                    Math.Clamp(baseColor.G + noise, 0, 255),
                    Math.Clamp(baseColor.B + noise, 0, 255));
                SetPixel(pixels, texSize, tx + x, ty + y, col);
            }
    }

    private void GenerateGrassTile(Color[] pixels, int texSize)
    {
        var (tx, ty) = TileOrigin(TileType.Grass);
        Color c1 = new(88, 168, 88);
        Color c2 = new(96, 176, 96);
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
                SetPixel(pixels, texSize, tx + x, ty + y, (x + y) % 2 == 0 ? c1 : c2);
    }

    private void GenerateTallGrassTile(Color[] pixels, int texSize)
    {
        var (tx, ty) = TileOrigin(TileType.TallGrass);
        Color bgColor = new(72, 136, 72);
        Color bladeColor = new(56, 176, 56);

        // Background
        FillRect(pixels, texSize, tx, ty, TileSize, TileSize, bgColor);

        // Grass blades in a pattern
        for (int i = 0; i < 5; i++)
        {
            int bx = tx + 1 + i * 3;
            for (int by = ty + 4; by < ty + 14; by++)
                SetPixel(pixels, texSize, bx, by, bladeColor);
            // Blade tip
            SetPixel(pixels, texSize, bx - 1, ty + 4, bladeColor);
            SetPixel(pixels, texSize, bx + 1, ty + 3 + (i % 2), bladeColor);
        }
    }

    private void GeneratePathTile(Color[] pixels, int texSize)
    {
        var (tx, ty) = TileOrigin(TileType.Path);
        Color c1 = new(192, 168, 128);
        Color c2 = new(184, 160, 120);
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
                SetPixel(pixels, texSize, tx + x, ty + y, (x + y) % 3 == 0 ? c2 : c1);
    }

    private void GenerateTreeTile(Color[] pixels, int texSize)
    {
        var (tx, ty) = TileOrigin(TileType.Tree);
        Color leaves = new(32, 96, 32);
        Color leavesLight = new(48, 120, 48);
        Color trunk = new(96, 64, 32);

        // Leaves (top ~10 rows)
        for (int y = 0; y < 10; y++)
            for (int x = 0; x < TileSize; x++)
                SetPixel(pixels, texSize, tx + x, ty + y,
                    (x + y) % 3 == 0 ? leavesLight : leaves);

        // Dark background below leaves
        for (int y = 10; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
                SetPixel(pixels, texSize, tx + x, ty + y, leaves);

        // Trunk
        FillRect(pixels, texSize, tx + 6, ty + 10, 4, 6, trunk);
    }

    private void GenerateWaterTile(Color[] pixels, int texSize)
    {
        var (tx, ty) = TileOrigin(TileType.Water);
        Color c1 = new(56, 112, 200);
        Color c2 = new(72, 128, 216);
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
                SetPixel(pixels, texSize, tx + x, ty + y,
                    ((x + y * 2) % 4 < 2) ? c1 : c2);
    }

    private void GenerateRockWallTile(Color[] pixels, int texSize)
    {
        var (tx, ty) = TileOrigin(TileType.RockWall);
        Color c1 = new(100, 100, 108);
        Color c2 = new(80, 80, 88);
        Color c3 = new(112, 112, 120);
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
            {
                int v = (x * 3 + y * 7) % 5;
                SetPixel(pixels, texSize, tx + x, ty + y, v < 2 ? c1 : v < 4 ? c2 : c3);
            }
    }

    private void GenerateDoorTile(Color[] pixels, int texSize)
    {
        var (tx, ty) = TileOrigin(TileType.Door);
        Color frame = new(100, 70, 40);
        Color door = new(140, 90, 50);

        FillRect(pixels, texSize, tx, ty, TileSize, TileSize, frame);
        FillRect(pixels, texSize, tx + 3, ty + 2, 10, 12, door);
        // Doorknob
        SetPixel(pixels, texSize, tx + 11, ty + 8, new Color(200, 180, 80));
    }

    private void GenerateSignTile(Color[] pixels, int texSize)
    {
        var (tx, ty) = TileOrigin(TileType.Sign);
        Color grass = new(88, 168, 88);
        Color board = new(180, 140, 80);
        Color pole = new(80, 56, 32);

        FillRect(pixels, texSize, tx, ty, TileSize, TileSize, grass);
        FillRect(pixels, texSize, tx + 3, ty + 2, 10, 6, board);
        FillRect(pixels, texSize, tx + 7, ty + 8, 2, 6, pole);
    }

    private void GenerateFlowersTile(Color[] pixels, int texSize)
    {
        var (tx, ty) = TileOrigin(TileType.Flowers);
        Color grass = new(88, 168, 88);
        Color[] flowerColors = { new(220, 80, 80), new(240, 200, 40), new(200, 80, 200) };

        FillRect(pixels, texSize, tx, ty, TileSize, TileSize, grass);
        for (int i = 0; i < 6; i++)
        {
            int fx = tx + 2 + (i % 3) * 4 + (i / 3) * 2;
            int fy = ty + 3 + (i / 3) * 6;
            SetPixel(pixels, texSize, fx, fy, flowerColors[i % 3]);
            SetPixel(pixels, texSize, fx + 1, fy, flowerColors[i % 3]);
            SetPixel(pixels, texSize, fx, fy + 1, flowerColors[i % 3]);
        }
    }

    private void GenerateCheckerTile(Color[] pixels, int texSize, TileType type, Color c1, Color c2)
    {
        var (tx, ty) = TileOrigin(type);
        for (int y = 0; y < TileSize; y++)
            for (int x = 0; x < TileSize; x++)
                SetPixel(pixels, texSize, tx + x, ty + y,
                    ((x / 4) + (y / 4)) % 2 == 0 ? c1 : c2);
    }
}
