using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.World;

namespace PokemonGen1.Game.Rendering;

public class ProceduralSprites
{
    private const int Size = 16;
    private readonly GraphicsDevice _device;
    private readonly Dictionary<string, Texture2D> _cache = new();

    public ProceduralSprites(GraphicsDevice device)
    {
        _device = device;
    }

    public Texture2D GetPlayerSprite(Direction facing, int frame)
    {
        string key = $"player_{facing}_{frame}";
        if (_cache.TryGetValue(key, out var cached)) return cached;
        var tex = GenerateCharacter(facing, frame, new Color(220, 60, 60), new Color(40, 40, 180));
        _cache[key] = tex;
        return tex;
    }

    public Texture2D GetNPCSprite(string color, Direction facing)
    {
        string key = $"npc_{color}_{facing}";
        if (_cache.TryGetValue(key, out var cached)) return cached;

        Color shirt = color switch
        {
            "red" => new Color(200, 40, 40),
            "blue" => new Color(40, 80, 200),
            "green" => new Color(40, 160, 40),
            "pink" => new Color(240, 160, 180),
            "purple" => new Color(140, 40, 180),
            _ => new Color(120, 120, 140)
        };

        var tex = GenerateCharacter(facing, 0, shirt, new Color(60, 60, 60));
        _cache[key] = tex;
        return tex;
    }

    public Texture2D GetItemSprite()
    {
        if (_cache.TryGetValue("item", out var cached)) return cached;

        var tex = new Texture2D(_device, Size, Size);
        var px = new Color[Size * Size];

        // Pokeball shape
        Color red = new(220, 48, 48);
        Color white = new(240, 240, 240);
        Color black = new(24, 24, 24);

        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                float dist = dx * dx + dy * dy;
                if (dist > 49) // Outside circle
                    px[y * Size + x] = Color.Transparent;
                else if (y == 7 || y == 8) // Center line
                    px[y * Size + x] = black;
                else if (y < 7) // Top = red
                    px[y * Size + x] = red;
                else // Bottom = white
                    px[y * Size + x] = white;
            }

        // Center button
        for (int y = 6; y <= 9; y++)
            for (int x = 6; x <= 9; x++)
                px[y * Size + x] = black;
        px[7 * Size + 7] = white;
        px[7 * Size + 8] = white;
        px[8 * Size + 7] = white;
        px[8 * Size + 8] = white;

        tex.SetData(px);
        _cache["item"] = tex;
        return tex;
    }

    private Texture2D GenerateCharacter(Direction facing, int frame, Color shirtColor, Color pantsColor)
    {
        var tex = new Texture2D(_device, Size, Size);
        var px = new Color[Size * Size];

        Color skin = new(248, 216, 176);
        Color hair = new(48, 32, 16);
        Color outline = new(32, 32, 32);

        // Head (rows 1-6)
        // Hair top
        FillRect(px, 5, 1, 6, 2, hair);
        FillRect(px, 6, 0, 4, 3, hair);

        // Face
        FillRect(px, 6, 3, 4, 4, skin);

        // Eyes based on direction
        Color eyeColor = new(24, 24, 24);
        switch (facing)
        {
            case Direction.Down:
                px[4 * Size + 6] = eyeColor;
                px[4 * Size + 9] = eyeColor;
                break;
            case Direction.Up:
                px[3 * Size + 6] = eyeColor;
                px[3 * Size + 9] = eyeColor;
                break;
            case Direction.Left:
                px[4 * Size + 5] = eyeColor;
                px[4 * Size + 7] = eyeColor;
                break;
            case Direction.Right:
                px[4 * Size + 8] = eyeColor;
                px[4 * Size + 10] = eyeColor;
                break;
        }

        // Body (rows 7-11)
        FillRect(px, 5, 7, 6, 5, shirtColor);

        // Arms
        FillRect(px, 4, 8, 1, 3, shirtColor);
        FillRect(px, 11, 8, 1, 3, shirtColor);

        // Legs (rows 12-15)
        int legShift = frame == 1 ? 1 : 0;
        FillRect(px, 5 + legShift, 12, 2, 4, pantsColor);
        FillRect(px, 9 - legShift, 12, 2, 4, pantsColor);

        // Shoes
        FillRect(px, 5 + legShift, 15, 2, 1, outline);
        FillRect(px, 9 - legShift, 15, 2, 1, outline);

        tex.SetData(px);
        return tex;
    }

    private static void FillRect(Color[] pixels, int x, int y, int w, int h, Color color)
    {
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
            {
                int px = x + dx, py = y + dy;
                if (px >= 0 && px < Size && py >= 0 && py < Size)
                    pixels[py * Size + px] = color;
            }
    }
}
