using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.Save;
using PokemonGen1.Core.World;
using PokemonGen1.Game.Entities;

namespace PokemonGen1.Game.Rendering;

public class TileRenderer
{
    private readonly ProceduralTileset _tileset;
    private readonly ProceduralSprites _sprites;

    public TileRenderer(ProceduralTileset tileset, ProceduralSprites sprites)
    {
        _tileset = tileset;
        _sprites = sprites;
    }

    public void Draw(SpriteBatch sb, MapData map, PlayerEntity player, SaveData save)
    {
        int tileSize = ProceduralTileset.TileSize;
        int viewW = PokemonGame.VirtualWidth;
        int viewH = PokemonGame.VirtualHeight;

        // Camera centered on player, clamped to map bounds
        float camX = player.VisualX * tileSize - viewW / 2f + tileSize / 2f;
        float camY = player.VisualY * tileSize - viewH / 2f + tileSize / 2f;

        float maxCamX = map.Width * tileSize - viewW;
        float maxCamY = map.Height * tileSize - viewH;
        camX = Math.Clamp(camX, 0, Math.Max(0, maxCamX));
        camY = Math.Clamp(camY, 0, Math.Max(0, maxCamY));

        int startTileX = Math.Max(0, (int)(camX / tileSize));
        int startTileY = Math.Max(0, (int)(camY / tileSize));
        int endTileX = Math.Min(map.Width, startTileX + viewW / tileSize + 2);
        int endTileY = Math.Min(map.Height, startTileY + viewH / tileSize + 2);

        // Ground layer
        for (int ty = startTileY; ty < endTileY; ty++)
            for (int tx = startTileX; tx < endTileX; tx++)
            {
                int tileId = map.GroundLayer[ty * map.Width + tx];
                var src = _tileset.GetSourceRect(tileId);
                var dest = new Rectangle(
                    (int)(tx * tileSize - camX),
                    (int)(ty * tileSize - camY),
                    tileSize, tileSize);
                sb.Draw(_tileset.Texture, dest, src, Color.White);
            }

        // Object layer
        for (int ty = startTileY; ty < endTileY; ty++)
            for (int tx = startTileX; tx < endTileX; tx++)
            {
                int tileId = map.ObjectLayer[ty * map.Width + tx];
                if (tileId == 0) continue; // Void = empty
                var src = _tileset.GetSourceRect(tileId);
                var dest = new Rectangle(
                    (int)(tx * tileSize - camX),
                    (int)(ty * tileSize - camY),
                    tileSize, tileSize);
                sb.Draw(_tileset.Texture, dest, src, Color.White);
            }

        // Events (NPCs, items) - sorted by Y for depth
        foreach (var evt in map.Events)
        {
            if (evt.X < startTileX || evt.X >= endTileX ||
                evt.Y < startTileY || evt.Y >= endTileY)
                continue;

            int screenX = (int)(evt.X * tileSize - camX);
            int screenY = (int)(evt.Y * tileSize - camY);
            var dest = new Rectangle(screenX, screenY, tileSize, tileSize);

            if (evt.Type == "npc")
            {
                // Skip defeated trainers
                if (evt.TrainerId.HasValue && save.DefeatedTrainerIds.Contains(evt.TrainerId.Value))
                    continue;

                var facing = evt.Facing ?? Direction.Down;
                string color = evt.SpriteColor ?? "gray";
                var sprite = _sprites.GetNPCSprite(color, facing);
                sb.Draw(sprite, dest, Color.White);
            }
            else if (evt.Type == "item")
            {
                // Skip collected items
                if (evt.ItemId.HasValue && save.CollectedItemIds.Contains(evt.ItemId.Value))
                    continue;

                var sprite = _sprites.GetItemSprite();
                sb.Draw(sprite, dest, Color.White);
            }
        }

        // Player
        {
            int screenX = (int)(player.VisualX * tileSize - camX);
            int screenY = (int)(player.VisualY * tileSize - camY);
            var dest = new Rectangle(screenX, screenY, tileSize, tileSize);
            var sprite = _sprites.GetPlayerSprite(player.Facing, player.AnimFrame);
            sb.Draw(sprite, dest, Color.White);
        }

        // Overhead layer
        for (int ty = startTileY; ty < endTileY; ty++)
            for (int tx = startTileX; tx < endTileX; tx++)
            {
                int tileId = map.OverheadLayer[ty * map.Width + tx];
                if (tileId == 0) continue;
                var src = _tileset.GetSourceRect(tileId);
                var dest = new Rectangle(
                    (int)(tx * tileSize - camX),
                    (int)(ty * tileSize - camY),
                    tileSize, tileSize);
                sb.Draw(_tileset.Texture, dest, src, Color.White);
            }
    }
}
