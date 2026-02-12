using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PokemonGen1.Game.Rendering;

/// <summary>
/// Loads and caches Pokemon sprites from the data/sprites directory.
/// Sprites are loaded at runtime using Texture2D.FromStream (not the content pipeline).
/// </summary>
public class SpriteManager
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly string _spritesDir;
    private readonly Dictionary<string, Texture2D> _cache = new();

    public SpriteManager(GraphicsDevice graphicsDevice, string spritesDir)
    {
        _graphicsDevice = graphicsDevice;
        _spritesDir = spritesDir;
    }

    /// <summary>
    /// Get the front sprite for a Pokemon by dex number.
    /// </summary>
    public Texture2D? GetFrontSprite(int dexNumber)
    {
        return GetSprite($"front/{dexNumber}.png");
    }

    /// <summary>
    /// Get the back sprite for a Pokemon by dex number.
    /// </summary>
    public Texture2D? GetBackSprite(int dexNumber)
    {
        return GetSprite($"back/{dexNumber}.png");
    }

    private Texture2D? GetSprite(string relativePath)
    {
        if (_cache.TryGetValue(relativePath, out var cached))
            return cached;

        var fullPath = Path.Combine(_spritesDir, relativePath);
        if (!File.Exists(fullPath))
            return null;

        try
        {
            using var stream = File.OpenRead(fullPath);
            var texture = Texture2D.FromStream(_graphicsDevice, stream);

            // Gen 1 sprites have white backgrounds - make white pixels transparent
            MakeWhiteTransparent(texture);

            _cache[relativePath] = texture;
            return texture;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Replace white/near-white pixels with transparent.
    /// Gen 1 Red/Blue sprites have white backgrounds that need to be removed.
    /// </summary>
    private static void MakeWhiteTransparent(Texture2D texture)
    {
        var data = new Color[texture.Width * texture.Height];
        texture.GetData(data);

        for (int i = 0; i < data.Length; i++)
        {
            // If pixel is white or near-white, make it transparent
            if (data[i].R > 240 && data[i].G > 240 && data[i].B > 240)
            {
                data[i] = Color.Transparent;
            }
        }

        texture.SetData(data);
    }

    /// <summary>
    /// Preload sprites for a set of Pokemon (e.g., both battle participants).
    /// </summary>
    public void Preload(params int[] dexNumbers)
    {
        foreach (var dex in dexNumbers)
        {
            GetFrontSprite(dex);
            GetBackSprite(dex);
        }
    }
}
