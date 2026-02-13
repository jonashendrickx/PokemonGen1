using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.Battle;
using PokemonGen1.Core.Data;
using PokemonGen1.Core.Pokemon;
using PokemonGen1.Core.Save;
using PokemonGen1.Core.Trainers;
using PokemonGen1.Core.World;
using PokemonGen1.Game.Entities;
using PokemonGen1.Game.Input;
using PokemonGen1.Game.Rendering;

namespace PokemonGen1.Game.Screens;

public enum OverworldState
{
    Walking,
    Dialog,
    AreaTransition
}

public class OverworldScreen : IScreen
{
    private static readonly Color TextColor = new(40, 40, 40);
    private static readonly Color BoxColor = new(248, 248, 248);
    private static readonly Color BorderColor = new(40, 40, 40);
    private static readonly Color StatusBarBg = new(40, 40, 40);

    private readonly PokemonGame _game;
    private readonly SaveData _save;
    private readonly EncounterSystem _encounters;
    private readonly MapGenerator _mapGenerator;
    private readonly Random _rng = new();
    private ScreenManager _manager = null!;

    // Rendering
    private readonly ProceduralTileset _tileset;
    private readonly ProceduralSprites _sprites;
    private readonly TileRenderer _tileRenderer;

    // World state
    private MapData _currentMap = null!;
    private PlayerEntity _player = null!;

    // Dialog
    private OverworldState _state = OverworldState.Walking;
    private readonly List<string> _dialogLines = new();
    private int _dialogIndex;
    private Action? _dialogCallback;

    // Area transition
    private float _transitionTimer;
    private float _transitionAlpha;
    private string? _pendingMapId;
    private int _pendingX = -1, _pendingY = -1;

    // Area name popup
    private float _areaNameTimer;
    private string _areaName = "";

    public bool IsOverlay => false;
    public bool BlocksUpdate => true;

    public OverworldScreen(PokemonGame game, SaveData save)
    {
        _game = game;
        _save = save;
        _encounters = new EncounterSystem(game.GameData, _rng);
        _mapGenerator = new MapGenerator(_rng);
        _tileset = new ProceduralTileset(game.GraphicsDevice);
        _sprites = new ProceduralSprites(game.GraphicsDevice);
        _tileRenderer = new TileRenderer(_tileset, _sprites);
    }

    public void Enter(ScreenManager manager)
    {
        _manager = manager;
        LoadMap(_save.CurrentMapId, _save.PlayerX, _save.PlayerY);
    }

    public void Exit() { }

    private void LoadMap(string mapId, int playerX, int playerY)
    {
        var area = _game.GameData.GetArea(mapId);
        if (area == null) return;

        _save.CurrentMapId = mapId;
        _currentMap = _mapGenerator.Generate(area, _game.GameData);

        // Resolve player position — also check collision so we don't spawn inside walls
        bool needsSpawn = playerX < 0 || playerY < 0
            || playerX >= _currentMap.Width || playerY >= _currentMap.Height
            || _currentMap.CollisionLayer[playerY * _currentMap.Width + playerX];
        if (needsSpawn)
        {
            var pos = FindSpawnPoint(_currentMap);
            playerX = pos.x;
            playerY = pos.y;
        }

        _player = new PlayerEntity(playerX, playerY, _save.PlayerFacing);
        _save.PlayerX = playerX;
        _save.PlayerY = playerY;

        _areaName = area.Name;
        _areaNameTimer = 3f;

        GrantAreaFlags(area);
    }

    private (int x, int y) FindSpawnPoint(MapData map)
    {
        // Try center path tiles
        int cx = map.Width / 2, cy = map.Height / 2;
        for (int r = 0; r < Math.Max(map.Width, map.Height); r++)
        {
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || x >= map.Width || y < 0 || y >= map.Height) continue;
                    int idx = y * map.Width + x;
                    if (!map.CollisionLayer[idx])
                        return (x, y);
                }
        }
        return (cx, cy);
    }

    #region Update

    public void Update(GameTime gameTime, InputManager input)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_areaNameTimer > 0) _areaNameTimer -= dt;

        switch (_state)
        {
            case OverworldState.Walking:
                UpdateWalking(dt, input);
                break;
            case OverworldState.Dialog:
                UpdateDialog(input);
                break;
            case OverworldState.AreaTransition:
                UpdateTransition(dt);
                break;
        }
    }

    private void UpdateWalking(float dt, InputManager input)
    {
        _player.Update(dt);

        if (!_player.IsMoving)
        {
            // Process input
            Direction? moveDir = null;
            if (input.IsHeld(InputAction.Up)) moveDir = Direction.Up;
            else if (input.IsHeld(InputAction.Down)) moveDir = Direction.Down;
            else if (input.IsHeld(InputAction.Left)) moveDir = Direction.Left;
            else if (input.IsHeld(InputAction.Right)) moveDir = Direction.Right;

            if (moveDir.HasValue)
            {
                if (_player.TryMove(moveDir.Value, _currentMap))
                {
                    // Movement started - will check triggers on arrival
                }
                else
                {
                    // Blocked - just face direction
                    _player.Facing = moveDir.Value;
                }
            }

            // Interact with facing tile
            if (input.IsPressed(InputAction.Confirm))
            {
                TryInteract();
            }

            // Party summary
            if (input.IsPressed(InputAction.Start))
            {
                ShowParty();
            }
        }
        // Check if movement just completed
        if (!_player.IsMoving && (_player.TileX != _save.PlayerX || _player.TileY != _save.PlayerY))
        {
            _save.PlayerX = _player.TileX;
            _save.PlayerY = _player.TileY;
            _save.PlayerFacing = _player.Facing;
            OnStepComplete();
        }
    }

    private void OnStepComplete()
    {
        // Check warps
        foreach (var warp in _currentMap.Warps)
        {
            if (warp.X == _player.TileX && warp.Y == _player.TileY)
            {
                StartTransition(warp.TargetMapId, warp.TargetX, warp.TargetY);
                return;
            }
        }

        // Check edge transitions
        if (_player.TileX <= 0 || _player.TileX >= _currentMap.Width - 1 ||
            _player.TileY <= 0 || _player.TileY >= _currentMap.Height - 1)
        {
            foreach (var conn in _currentMap.Connections)
            {
                bool matches = conn.Direction switch
                {
                    Direction.Up => _player.TileY <= 0,
                    Direction.Down => _player.TileY >= _currentMap.Height - 1,
                    Direction.Left => _player.TileX <= 0,
                    Direction.Right => _player.TileX >= _currentMap.Width - 1,
                    _ => false
                };

                if (matches)
                {
                    // Check required flag
                    var area = _game.GameData.GetArea(_save.CurrentMapId);
                    if (area != null)
                    {
                        var areaConn = area.Connections.FirstOrDefault(c => c.AreaId == conn.TargetMapId);
                        if (areaConn?.RequiredFlag != null && !_save.StoryFlags.Contains(areaConn.RequiredFlag))
                        {
                            ShowDialog(new[] { $"The way is blocked! ({areaConn.RequiredFlag})" });
                            return;
                        }
                    }

                    int targetX = -1, targetY = -1;
                    // Place at opposite edge of target map
                    switch (conn.Direction)
                    {
                        case Direction.Up: targetY = -2; break;    // will resolve to bottom
                        case Direction.Down: targetY = -3; break;  // will resolve to top
                        case Direction.Left: targetX = -2; break;  // will resolve to right
                        case Direction.Right: targetX = -3; break; // will resolve to left
                    }
                    StartTransition(conn.TargetMapId, targetX, targetY);
                    return;
                }
            }
        }

        // Check wild encounters
        int idx = _player.TileY * _currentMap.Width + _player.TileX;
        if (idx >= 0 && idx < _currentMap.EncounterLayer.Length && _currentMap.EncounterLayer[idx])
        {
            var table = _game.GameData.GetEncounterTable(_save.CurrentMapId);
            if (table != null)
            {
                var wild = _encounters.TryEncounter(table);
                if (wild != null)
                {
                    StartWildBattle(wild);
                    return;
                }
            }
        }

        // Check item pickups (step on)
        foreach (var evt in _currentMap.Events)
        {
            if (evt.X == _player.TileX && evt.Y == _player.TileY && evt.Type == "item")
            {
                if (evt.ItemId.HasValue && !_save.CollectedItemIds.Contains(evt.ItemId.Value))
                {
                    PickUpItem(evt);
                    return;
                }
            }
        }
    }

    private void TryInteract()
    {
        var (fx, fy) = _player.GetFacingTile();

        foreach (var evt in _currentMap.Events)
        {
            if (evt.X != fx || evt.Y != fy) continue;

            switch (evt.Type)
            {
                case "npc":
                    InteractNPC(evt);
                    return;
                case "sign":
                    if (evt.Dialog.Length > 0)
                        ShowDialog(evt.Dialog);
                    return;
                case "item":
                    if (evt.ItemId.HasValue && !_save.CollectedItemIds.Contains(evt.ItemId.Value))
                        PickUpItem(evt);
                    return;
            }
        }
    }

    private void InteractNPC(EventTrigger npc)
    {
        // Skip defeated trainers
        if (npc.TrainerId.HasValue && _save.DefeatedTrainerIds.Contains(npc.TrainerId.Value))
        {
            // Show after-battle dialog if available
            if (npc.TrainerId.HasValue)
            {
                var trainer = _game.GameData.GetTrainer(npc.TrainerId.Value);
                if (trainer?.AfterBattleDialog.Length > 0)
                {
                    ShowDialog(trainer.AfterBattleDialog);
                    return;
                }
            }
            ShowDialog(new[] { "..." });
            return;
        }

        // Nurse heals
        if (npc.SpriteColor == "pink" && npc.ScriptId == null)
        {
            ShowDialog(npc.Dialog, () =>
            {
                HealAllPokemon();
                ShowDialog(new[] { "Your Pokemon have been fully healed!" });
            });
            return;
        }

        // Shop clerk
        if (npc.ScriptId == "shop")
        {
            var area = _game.GameData.GetArea(_save.CurrentMapId);
            if (area?.ShopId != null)
            {
                ShowDialog(npc.Dialog, () =>
                {
                    var shop = _game.GameData.GetShop(area.ShopId);
                    if (shop != null)
                        _manager.Push(new ShopScreen(_game, _save, shop));
                });
                return;
            }
        }

        // Trainer battle
        if (npc.TrainerId.HasValue)
        {
            var trainer = _game.GameData.GetTrainer(npc.TrainerId.Value);
            if (trainer != null)
            {
                // Check required flag
                if (trainer.RequiredFlag != null && !_save.StoryFlags.Contains(trainer.RequiredFlag))
                {
                    ShowDialog(new[] { $"Can't challenge {trainer.Name} yet." });
                    return;
                }

                ShowDialog(npc.Dialog, () => StartTrainerBattle(trainer));
                return;
            }
        }

        // Regular NPC dialog
        if (npc.Dialog.Length > 0)
            ShowDialog(npc.Dialog);
    }

    private void PickUpItem(EventTrigger evt)
    {
        if (!evt.ItemId.HasValue) return;

        var itemData = _game.GameData.GetItem(evt.ItemId.Value);
        if (itemData == null) return;

        // Find matching area item for quantity
        var area = _game.GameData.GetArea(_save.CurrentMapId);
        int qty = 1;
        if (area != null)
        {
            var areaItem = area.Items.FirstOrDefault(i => i.ItemId == evt.ItemId.Value);
            if (areaItem != null) qty = areaItem.Quantity;
        }

        _save.Inventory.AddItem(evt.ItemId.Value, qty);
        _save.CollectedItemIds.Add(evt.ItemId.Value);
        ShowDialog(new[] { $"Found {itemData.Name}" + (qty > 1 ? $" x{qty}" : "") + "!" });
    }

    #endregion

    #region Dialog

    private void ShowDialog(string[] lines, Action? callback = null)
    {
        _dialogLines.Clear();
        _dialogLines.AddRange(lines);
        _dialogIndex = 0;
        _dialogCallback = callback;
        _state = OverworldState.Dialog;
    }

    private void UpdateDialog(InputManager input)
    {
        if (input.IsPressed(InputAction.Confirm))
        {
            _dialogIndex++;
            if (_dialogIndex >= _dialogLines.Count)
            {
                _state = OverworldState.Walking;
                _dialogCallback?.Invoke();
                _dialogCallback = null;
            }
        }

        if (input.IsPressed(InputAction.Cancel))
        {
            _state = OverworldState.Walking;
            _dialogCallback?.Invoke();
            _dialogCallback = null;
        }
    }

    #endregion

    #region Transitions

    private void StartTransition(string mapId, int targetX, int targetY)
    {
        _pendingMapId = mapId;
        _pendingX = targetX;
        _pendingY = targetY;
        _transitionTimer = 0;
        _transitionAlpha = 0;
        _state = OverworldState.AreaTransition;
    }

    private void UpdateTransition(float dt)
    {
        _transitionTimer += dt;

        if (_transitionTimer < 0.3f)
        {
            // Fade out
            _transitionAlpha = _transitionTimer / 0.3f;
        }
        else if (_transitionTimer < 0.35f && _pendingMapId != null)
        {
            // Load new map at midpoint
            ResolveTransition();
            _pendingMapId = null;
        }
        else if (_transitionTimer < 0.65f)
        {
            // Fade in
            _transitionAlpha = 1f - (_transitionTimer - 0.35f) / 0.3f;
        }
        else
        {
            _transitionAlpha = 0;
            _state = OverworldState.Walking;
        }
    }

    private void ResolveTransition()
    {
        if (_pendingMapId == null) return;

        var area = _game.GameData.GetArea(_pendingMapId);
        if (area == null) return;

        int px = _pendingX, py = _pendingY;

        // Generate target map to resolve positions
        var targetMap = _mapGenerator.Generate(area, _game.GameData);

        // Resolve special position codes
        if (px == -2) px = targetMap.Width - 2;  // came from left → place at right
        if (px == -3) px = 1;                     // came from right → place at left
        if (py == -2) py = targetMap.Height - 2;  // came from top → place at bottom
        if (py == -3) py = 1;                     // came from bottom → place at top

        if (px < 0 || py < 0)
        {
            var spawn = FindSpawnPoint(targetMap);
            px = spawn.x;
            py = spawn.y;
        }

        // Clamp
        px = Math.Clamp(px, 0, targetMap.Width - 1);
        py = Math.Clamp(py, 0, targetMap.Height - 1);

        LoadMap(_pendingMapId, px, py);
    }

    #endregion

    #region Battle

    private void StartWildBattle(PokemonInstance wild)
    {
        var playerPokemon = GetFirstAlivePokemon();
        if (playerPokemon == null)
        {
            ShowDialog(new[] { "All your Pokemon have fainted!" });
            return;
        }

        var species = _game.GameData.GetSpecies(wild.SpeciesId);
        var playerSpecies = _game.GameData.GetSpecies(playerPokemon.SpeciesId);

        _save.PokedexSeen.Add(wild.SpeciesId);

        var battleState = new BattleState
        {
            Type = BattleType.Wild,
            PlayerParty = _save.Party,
            PlayerActiveIndex = Array.IndexOf(_save.Party, playerPokemon),
            PlayerActive = new BattlePokemon(playerPokemon, playerSpecies),
            OpponentParty = new[] { wild },
            OpponentActiveIndex = 0,
            OpponentActive = new BattlePokemon(wild, species)
        };

        _manager.Push(new BattleScreen(_game, battleState, OnBattleEnd));
    }

    private void StartTrainerBattle(TrainerData trainer)
    {
        var playerPokemon = GetFirstAlivePokemon();
        if (playerPokemon == null)
        {
            ShowDialog(new[] { "All your Pokemon have fainted!" });
            return;
        }

        var trainerParty = new List<PokemonInstance>();
        foreach (var tp in trainer.Party)
        {
            var species = _game.GameData.GetSpecies(tp.SpeciesId);
            var pokemon = PokemonInstance.Create(species, tp.Level, _rng);
            if (tp.MoveOverrides != null && tp.MoveOverrides.Length > 0)
            {
                pokemon.Moves = tp.MoveOverrides.Select(moveId => new Core.Moves.MoveInstance
                {
                    MoveId = moveId,
                    CurrentPP = _game.GameData.GetMove(moveId).MaxPP,
                    MaxPP = _game.GameData.GetMove(moveId).MaxPP
                }).ToArray();
            }
            else
            {
                pokemon.Moves = _game.GameData.GetDefaultMoves(tp.SpeciesId, tp.Level);
                if (pokemon.Moves.Length == 0)
                {
                    pokemon.Moves = new[]
                    {
                        new Core.Moves.MoveInstance { MoveId = 33, CurrentPP = 35, MaxPP = 35 }
                    };
                }
            }
            pokemon.CurrentHp = pokemon.MaxHp(species);
            trainerParty.Add(pokemon);
            _save.PokedexSeen.Add(tp.SpeciesId);
        }

        var firstTrainerPokemon = trainerParty[0];
        var firstTrainerSpecies = _game.GameData.GetSpecies(firstTrainerPokemon.SpeciesId);
        var playerSpecies = _game.GameData.GetSpecies(playerPokemon.SpeciesId);

        var battleState = new BattleState
        {
            Type = BattleType.Trainer,
            PlayerParty = _save.Party,
            PlayerActiveIndex = Array.IndexOf(_save.Party, playerPokemon),
            PlayerActive = new BattlePokemon(playerPokemon, playerSpecies),
            OpponentParty = trainerParty.ToArray(),
            OpponentActiveIndex = 0,
            OpponentActive = new BattlePokemon(firstTrainerPokemon, firstTrainerSpecies),
            OpponentTrainer = trainer
        };

        _manager.Push(new BattleScreen(_game, battleState, outcome => OnTrainerBattleEnd(outcome, trainer)));
    }

    private void OnBattleEnd(BattleOutcome outcome)
    {
        switch (outcome)
        {
            case BattleOutcome.PlayerWin:
            case BattleOutcome.PlayerFled:
                break;
            case BattleOutcome.PlayerLose:
                HealAllPokemon();
                ReturnToLastPokemonCenter();
                ShowDialog(new[] { "You blacked out!", "Returned to the last Pokemon Center." });
                break;
        }
    }

    private void OnTrainerBattleEnd(BattleOutcome outcome, TrainerData trainer)
    {
        if (outcome == BattleOutcome.PlayerWin)
        {
            _save.DefeatedTrainerIds.Add(trainer.Id);
            _save.Money += trainer.RewardMoney;

            if (trainer.SetsFlag != null)
                _save.StoryFlags.Add(trainer.SetsFlag);

            if (trainer.IsGymLeader && trainer.BadgeIndex.HasValue)
            {
                int idx = trainer.BadgeIndex.Value;
                if (idx >= 0 && idx < _save.BadgesObtained.Length)
                {
                    _save.BadgesObtained[idx] = true;
                    _save.BadgeCount = _save.BadgesObtained.Count(b => b);
                }

                if (_save.BadgeCount >= 7 && !_save.StoryFlags.Contains("badge_earth_unlocked"))
                    _save.StoryFlags.Add("badge_earth_unlocked");
            }

            if (trainer.SetsFlag == "champion_defeated")
            {
                ShowDialog(new[] { "Congratulations!", "You are the new Pokemon Champion!" });
            }
            else
            {
                string reward = trainer.RewardMoney > 0 ? $" Got ${trainer.RewardMoney}!" : "";
                ShowDialog(new[] { $"Defeated {trainer.Name}!{reward}" });
            }
        }
        else
        {
            HealAllPokemon();
            ReturnToLastPokemonCenter();
            ShowDialog(new[] { "You blacked out!", "Returned to the last Pokemon Center." });
        }
    }

    #endregion

    #region Helpers

    private void ShowParty()
    {
        if (_save.Party.Length == 0)
        {
            ShowDialog(new[] { "No Pokemon in party!" });
            return;
        }

        var lines = new List<string>();
        foreach (var p in _save.Party)
        {
            var sp = _game.GameData.GetSpecies(p.SpeciesId);
            string name = p.Nickname ?? sp.Name;
            string status = p.Status != StatusCondition.None ? $" [{p.Status}]" : "";
            lines.Add($"{name} Lv{p.Level} {p.CurrentHp}/{p.MaxHp(sp)}HP{status}");
        }

        ShowDialog(lines.ToArray());
    }

    private void HealAllPokemon()
    {
        foreach (var pokemon in _save.Party)
        {
            var species = _game.GameData.GetSpecies(pokemon.SpeciesId);
            pokemon.CurrentHp = pokemon.MaxHp(species);
            pokemon.Status = StatusCondition.None;
            foreach (var move in pokemon.Moves)
                move.CurrentPP = move.MaxPP;
        }
    }

    private void ReturnToLastPokemonCenter()
    {
        string[] centerAreas = { "indigo_plateau", "cinnabar_island", "fuchsia_city", "saffron_city",
            "celadon_city", "lavender_town", "vermilion_city", "cerulean_city",
            "pewter_city", "viridian_city", "pallet_town" };

        foreach (var areaId in centerAreas)
        {
            var area = _game.GameData.GetArea(areaId);
            if (area != null && area.HasPokemonCenter)
            {
                LoadMap(areaId, -1, -1);
                return;
            }
        }
        LoadMap("pallet_town", -1, -1);
    }

    private PokemonInstance? GetFirstAlivePokemon()
    {
        return _save.Party.FirstOrDefault(p => !p.IsFainted);
    }

    private void GrantAreaFlags(AreaData area)
    {
        // Auto-grant visit flags
    }

    #endregion

    #region Drawing

    public void Draw(SpriteBatch sb)
    {
        // Draw tile map with player
        _tileRenderer.Draw(sb, _currentMap, _player, _save);

        // Area name popup
        if (_areaNameTimer > 0)
        {
            float alpha = _areaNameTimer > 2.5f
                ? (3f - _areaNameTimer) * 2f  // fade in
                : Math.Min(1f, _areaNameTimer / 0.5f); // fade out

            var nameSize = _game.Font.MeasureString(_areaName);
            int boxW = (int)nameSize.X + 16;
            int boxX = (PokemonGame.VirtualWidth - boxW) / 2;
            int boxY = 8;

            _game.DrawRect(sb, new Rectangle(boxX, boxY, boxW, 18),
                new Color(0, 0, 0, (int)(180 * alpha)));
            sb.DrawString(_game.Font, _areaName,
                new Vector2(boxX + 8, boxY + 3),
                new Color(255, 255, 255, (int)(255 * alpha)));
        }

        // Dialog box
        if (_state == OverworldState.Dialog && _dialogIndex < _dialogLines.Count)
        {
            DrawDialogBox(sb);
        }

        // Transition fade
        if (_state == OverworldState.AreaTransition && _transitionAlpha > 0)
        {
            _game.DrawRect(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, PokemonGame.VirtualHeight),
                new Color(0, 0, 0, (int)(255 * _transitionAlpha)));
        }

        // Status bar
        DrawStatusBar(sb);
    }

    private void DrawDialogBox(SpriteBatch sb)
    {
        int boxH = 40;
        int boxY = PokemonGame.VirtualHeight - boxH - 14; // above status bar
        var box = new Rectangle(4, boxY, PokemonGame.VirtualWidth - 8, boxH);
        _game.DrawRect(sb, box, BoxColor);
        _game.DrawBorder(sb, box, BorderColor, 2);

        string text = _dialogIndex < _dialogLines.Count ? _dialogLines[_dialogIndex] : "";
        sb.DrawString(_game.Font, text, new Vector2(12, boxY + 6), TextColor);

        // Advance indicator
        if (_dialogIndex < _dialogLines.Count - 1)
        {
            float blink = (float)Math.Sin(DateTime.Now.Millisecond / 200.0 * Math.PI);
            if (blink > 0)
                sb.DrawString(_game.Font, "v", new Vector2(PokemonGame.VirtualWidth - 16, boxY + boxH - 14), TextColor);
        }
    }

    private void DrawStatusBar(SpriteBatch sb)
    {
        int y = PokemonGame.VirtualHeight - 14;
        _game.DrawRect(sb, new Rectangle(0, y, PokemonGame.VirtualWidth, 14), StatusBarBg);

        // Badges
        string badges = $"Badges:{_save.BadgeCount}";
        sb.DrawString(_game.Font, badges, new Vector2(4, y + 1), Color.White);

        // Money
        string money = $"${_save.Money}";
        var moneySize = _game.Font.MeasureString(money);
        sb.DrawString(_game.Font, money, new Vector2(PokemonGame.VirtualWidth / 2 - moneySize.X / 2, y + 1), Color.Yellow);

        // First Pokemon HP
        var lead = GetFirstAlivePokemon();
        if (lead != null)
        {
            var sp = _game.GameData.GetSpecies(lead.SpeciesId);
            string hp = $"{sp.Name} {lead.CurrentHp}/{lead.MaxHp(sp)}";
            var hpSize = _game.Font.MeasureString(hp);
            sb.DrawString(_game.Font, hp, new Vector2(PokemonGame.VirtualWidth - hpSize.X - 4, y + 1), Color.White);
        }
    }

    #endregion
}
