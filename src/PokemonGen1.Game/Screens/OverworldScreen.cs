using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.Battle;
using PokemonGen1.Core.Data;
using PokemonGen1.Core.Pokemon;
using PokemonGen1.Core.Save;
using PokemonGen1.Core.Trainers;
using PokemonGen1.Core.World;
using PokemonGen1.Game.Input;

namespace PokemonGen1.Game.Screens;

public enum OverworldMode
{
    Navigation,    // Choosing where to go
    AreaMenu,      // In-area actions (trainers, items, etc.)
    Message        // Showing a message
}

public class OverworldScreen : IScreen
{
    private static readonly Color BgColor = new(248, 248, 248);
    private static readonly Color TextColor = new(40, 40, 40);
    private static readonly Color BorderColor = new(40, 40, 40);
    private static readonly Color BoxColor = new(248, 248, 248);
    private static readonly Color HighlightColor = new(200, 228, 200);
    private static readonly Color LockedColor = new(180, 180, 180);
    private static readonly Color TownColor = new(120, 160, 220);
    private static readonly Color RouteColor = new(120, 200, 80);
    private static readonly Color CaveColor = new(160, 140, 120);
    private static readonly Color BuildingColor = new(200, 180, 140);

    private readonly PokemonGame _game;
    private readonly SaveData _save;
    private readonly EncounterSystem _encounters;
    private readonly Random _rng = new();
    private ScreenManager _manager = null!;

    private OverworldMode _mode = OverworldMode.Navigation;
    private int _navCursor;
    private int _menuCursor;
    private int _navScroll;

    // Message display
    private string _message = "";
    private float _messageTimer;

    // Area menu items
    private readonly List<string> _menuOptions = new();
    private readonly List<Action> _menuActions = new();

    public bool IsOverlay => false;
    public bool BlocksUpdate => true;

    public OverworldScreen(PokemonGame game, SaveData save)
    {
        _game = game;
        _save = save;
        _encounters = new EncounterSystem(game.GameData, _rng);
    }

    public void Enter(ScreenManager manager)
    {
        _manager = manager;
        _mode = OverworldMode.AreaMenu;
        _menuCursor = 0;
        BuildAreaMenu();
    }

    public void Exit() { }

    private AreaData? CurrentArea => _game.GameData.GetArea(_save.CurrentMapId);

    public void Update(GameTime gameTime, InputManager input)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_mode)
        {
            case OverworldMode.Navigation:
                UpdateNavigation(input);
                break;
            case OverworldMode.AreaMenu:
                UpdateAreaMenu(input);
                break;
            case OverworldMode.Message:
                _messageTimer += dt;
                if (_messageTimer > 0.5f && input.IsPressed(InputAction.Confirm))
                {
                    _mode = OverworldMode.AreaMenu;
                    BuildAreaMenu();
                }
                break;
        }
    }

    private void UpdateNavigation(InputManager input)
    {
        var area = CurrentArea;
        if (area == null) return;

        var connections = GetAccessibleConnections(area);
        if (connections.Count == 0) return;

        if (input.IsPressed(InputAction.Up))
        {
            _navCursor = Math.Max(0, _navCursor - 1);
            if (_navCursor < _navScroll) _navScroll = _navCursor;
        }
        if (input.IsPressed(InputAction.Down))
        {
            _navCursor = Math.Min(connections.Count - 1, _navCursor + 1);
            if (_navCursor >= _navScroll + 6) _navScroll = _navCursor - 5;
        }

        if (input.IsPressed(InputAction.Cancel))
        {
            _mode = OverworldMode.AreaMenu;
            BuildAreaMenu();
            return;
        }

        if (input.IsPressed(InputAction.Confirm))
        {
            var conn = connections[_navCursor];
            if (conn.locked)
            {
                ShowMessage($"The way is blocked! ({conn.conn.RequiredFlag})");
            }
            else
            {
                TravelTo(conn.conn.AreaId);
            }
        }
    }

    private void UpdateAreaMenu(InputManager input)
    {
        if (_menuOptions.Count == 0) return;

        if (input.IsPressed(InputAction.Up)) _menuCursor = Math.Max(0, _menuCursor - 1);
        if (input.IsPressed(InputAction.Down)) _menuCursor = Math.Min(_menuOptions.Count - 1, _menuCursor + 1);

        if (input.IsPressed(InputAction.Confirm))
        {
            if (_menuCursor < _menuActions.Count)
                _menuActions[_menuCursor]();
        }
    }

    private void BuildAreaMenu()
    {
        _menuOptions.Clear();
        _menuActions.Clear();
        _menuCursor = 0;

        var area = CurrentArea;
        if (area == null) return;

        // Travel
        _menuOptions.Add("Travel");
        _menuActions.Add(() =>
        {
            _mode = OverworldMode.Navigation;
            _navCursor = 0;
            _navScroll = 0;
        });

        // Walk (wild encounters)
        if (area.HasWildEncounters)
        {
            _menuOptions.Add("Walk in grass");
            _menuActions.Add(WalkInGrass);
        }

        // Trainers
        var undefeatedTrainers = GetUndefeatedTrainers(area);
        if (undefeatedTrainers.Count > 0)
        {
            _menuOptions.Add($"Trainers ({undefeatedTrainers.Count})");
            _menuActions.Add(() => ChallengeNextTrainer(undefeatedTrainers));
        }

        // Items
        var availableItems = GetAvailableItems(area);
        if (availableItems.Count > 0)
        {
            _menuOptions.Add($"Pick up items ({availableItems.Count})");
            _menuActions.Add(() => PickUpItem(availableItems));
        }

        // Pokemon Center
        if (area.HasPokemonCenter)
        {
            _menuOptions.Add("Pokemon Center");
            _menuActions.Add(HealParty);
        }

        // Poke Mart
        if (area.HasPokeMart && area.ShopId != null)
        {
            _menuOptions.Add("Poke Mart");
            _menuActions.Add(() =>
            {
                var shop = _game.GameData.GetShop(area.ShopId);
                if (shop != null)
                    _manager.Push(new ShopScreen(_game, _save, shop));
            });
        }

        // Party
        _menuOptions.Add("Party");
        _menuActions.Add(ShowParty);

        // Area story flags (auto-grant on first visit)
        GrantAreaFlags(area);
    }

    private void TravelTo(string areaId)
    {
        _save.CurrentMapId = areaId;
        _mode = OverworldMode.AreaMenu;
        _menuCursor = 0;
        BuildAreaMenu();

        var newArea = CurrentArea;
        if (newArea != null)
        {
            // Check for wild encounter during travel
            if (newArea.HasWildEncounters)
            {
                var table = _game.GameData.GetEncounterTable(areaId);
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

            ShowMessage($"Arrived at {newArea.Name}.");
        }
    }

    private void WalkInGrass()
    {
        var area = CurrentArea;
        if (area == null) return;

        var table = _game.GameData.GetEncounterTable(_save.CurrentMapId);
        if (table == null)
        {
            ShowMessage("No wild Pokemon here.");
            return;
        }

        var wild = _encounters.TryEncounter(table);
        if (wild != null)
        {
            StartWildBattle(wild);
        }
        else
        {
            ShowMessage("Nothing appeared... Keep walking!");
        }
    }

    private void StartWildBattle(PokemonInstance wild)
    {
        var playerPokemon = GetFirstAlivePokemon();
        if (playerPokemon == null)
        {
            ShowMessage("All your Pokemon have fainted!");
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
            ShowMessage("All your Pokemon have fainted!");
            return;
        }

        // Build trainer party
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
                _mode = OverworldMode.AreaMenu;
                BuildAreaMenu();
                break;
            case BattleOutcome.PlayerLose:
                // Heal party and return to last Pokemon Center
                HealAllPokemon();
                ReturnToLastPokemonCenter();
                ShowMessage("You blacked out! Returned to the last Pokemon Center.");
                break;
            case BattleOutcome.PlayerFled:
                _mode = OverworldMode.AreaMenu;
                BuildAreaMenu();
                break;
        }
    }

    private void OnTrainerBattleEnd(BattleOutcome outcome, TrainerData trainer)
    {
        if (outcome == BattleOutcome.PlayerWin)
        {
            _save.DefeatedTrainerIds.Add(trainer.Id);
            _save.Money += trainer.RewardMoney;

            // Set story flags
            if (trainer.SetsFlag != null)
                _save.StoryFlags.Add(trainer.SetsFlag);

            // Track badges
            if (trainer.IsGymLeader && trainer.BadgeIndex.HasValue)
            {
                int idx = trainer.BadgeIndex.Value;
                if (idx >= 0 && idx < _save.BadgesObtained.Length)
                {
                    _save.BadgesObtained[idx] = true;
                    _save.BadgeCount = _save.BadgesObtained.Count(b => b);
                }

                // Check if all 7 non-Earth badges → unlock Viridian Gym
                if (_save.BadgeCount >= 7 && !_save.StoryFlags.Contains("badge_earth_unlocked"))
                    _save.StoryFlags.Add("badge_earth_unlocked");
            }

            // Champion defeated
            if (trainer.SetsFlag == "champion_defeated")
            {
                ShowMessage("Congratulations! You are the new Pokemon Champion!");
            }
            else
            {
                string reward = trainer.RewardMoney > 0 ? $" Got ${trainer.RewardMoney}!" : "";
                ShowMessage($"Defeated {trainer.Name}!{reward}");
            }
        }
        else
        {
            HealAllPokemon();
            ReturnToLastPokemonCenter();
            ShowMessage("You blacked out! Returned to the last Pokemon Center.");
        }

        BuildAreaMenu();
    }

    private void ChallengeNextTrainer(List<TrainerData> trainers)
    {
        if (trainers.Count == 0) return;
        var trainer = trainers[0];

        // Check required flags
        if (trainer.RequiredFlag != null && !_save.StoryFlags.Contains(trainer.RequiredFlag))
        {
            ShowMessage($"Can't challenge {trainer.Name} yet.");
            return;
        }

        StartTrainerBattle(trainer);
    }

    private void PickUpItem(List<AreaItem> items)
    {
        if (items.Count == 0) return;

        var item = items[0];
        var itemData = _game.GameData.GetItem(item.ItemId);

        _save.Inventory.AddItem(item.ItemId, item.Quantity);
        _save.CollectedItemIds.Add(item.ItemId);

        ShowMessage($"Found {itemData.Name} x{item.Quantity}!");
        BuildAreaMenu();
    }

    private void HealParty()
    {
        HealAllPokemon();
        ShowMessage("Your Pokemon have been healed!");
    }

    private void ShowParty()
    {
        if (_save.Party.Length == 0)
        {
            ShowMessage("No Pokemon in party!");
            return;
        }

        var lines = new List<string>();
        foreach (var p in _save.Party)
        {
            var sp = _game.GameData.GetSpecies(p.SpeciesId);
            string name = p.Nickname ?? sp.Name;
            string status = p.Status != Core.Battle.StatusCondition.None ? $" [{p.Status}]" : "";
            lines.Add($"{name} Lv{p.Level} {p.CurrentHp}/{p.MaxHp(sp)}HP{status}");
        }

        ShowMessage(string.Join("\n", lines));
    }

    private void ShowMessage(string msg)
    {
        _message = msg;
        _messageTimer = 0;
        _mode = OverworldMode.Message;
    }

    private void HealAllPokemon()
    {
        foreach (var pokemon in _save.Party)
        {
            var species = _game.GameData.GetSpecies(pokemon.SpeciesId);
            pokemon.CurrentHp = pokemon.MaxHp(species);
            pokemon.Status = Core.Battle.StatusCondition.None;
            foreach (var move in pokemon.Moves)
                move.CurrentPP = move.MaxPP;
        }
    }

    private void ReturnToLastPokemonCenter()
    {
        // Find the nearest visited Pokemon Center area
        string fallback = "pallet_town";
        string[] centerAreas = { "indigo_plateau", "cinnabar_island", "fuchsia_city", "saffron_city",
            "celadon_city", "lavender_town", "vermilion_city", "cerulean_city",
            "pewter_city", "viridian_city", "pallet_town" };

        // Return to the last one the player has likely been to
        foreach (var areaId in centerAreas)
        {
            var area = _game.GameData.GetArea(areaId);
            if (area != null && area.HasPokemonCenter)
            {
                // Simple heuristic: return to pallet_town or last city with center
                _save.CurrentMapId = areaId;
                return;
            }
        }
        _save.CurrentMapId = fallback;
    }

    private PokemonInstance? GetFirstAlivePokemon()
    {
        return _save.Party.FirstOrDefault(p => !p.IsFainted);
    }

    private List<(AreaConnection conn, bool locked)> GetAccessibleConnections(AreaData area)
    {
        var result = new List<(AreaConnection, bool)>();
        foreach (var conn in area.Connections)
        {
            bool locked = conn.RequiredFlag != null && !_save.StoryFlags.Contains(conn.RequiredFlag);
            result.Add((conn, locked));
        }
        return result;
    }

    private List<TrainerData> GetUndefeatedTrainers(AreaData area)
    {
        var result = new List<TrainerData>();
        foreach (var trainerId in area.Trainers)
        {
            if (_save.DefeatedTrainerIds.Contains(trainerId)) continue;
            var trainer = _game.GameData.GetTrainer(trainerId);
            if (trainer != null)
                result.Add(trainer);
        }
        return result;
    }

    private List<AreaItem> GetAvailableItems(AreaData area)
    {
        var result = new List<AreaItem>();
        foreach (var item in area.Items)
        {
            if (_save.CollectedItemIds.Contains(item.ItemId)) continue;
            if (item.RequiredFlag != null && !_save.StoryFlags.Contains(item.RequiredFlag)) continue;
            result.Add(item);
        }
        return result;
    }

    private void GrantAreaFlags(AreaData area)
    {
        // Some areas grant flags on visit (like getting Tea in Celadon)
        // This is handled by progression.json story events
        // For simplicity, check if the area has implicit flags defined in areas.json
        // (The "flags" array in areas.json is for auto-granted flags)
    }

    #region Drawing

    public void Draw(SpriteBatch sb)
    {
        _game.DrawRect(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, PokemonGame.VirtualHeight), BgColor);

        DrawAreaHeader(sb);

        switch (_mode)
        {
            case OverworldMode.Navigation:
                DrawNavigation(sb);
                break;
            case OverworldMode.AreaMenu:
                DrawAreaMenu(sb);
                break;
            case OverworldMode.Message:
                DrawMessage(sb);
                break;
        }

        DrawStatusBar(sb);
    }

    private void DrawAreaHeader(SpriteBatch sb)
    {
        var area = CurrentArea;
        if (area == null) return;

        // Area type color bar
        Color typeColor = area.Type switch
        {
            AreaType.Town or AreaType.City => TownColor,
            AreaType.Route => RouteColor,
            AreaType.Cave or AreaType.DungeonFloor => CaveColor,
            AreaType.Building => BuildingColor,
            _ => new Color(180, 180, 180)
        };

        _game.DrawRect(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, 30), typeColor);
        _game.DrawBorder(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, 30), BorderColor, 1);

        // Area name
        sb.DrawString(_game.Font, area.Name, new Vector2(6, 2), Color.White);

        // Area type tag
        string typeTag = area.Type.ToString();
        var tagSize = _game.Font.MeasureString(typeTag);
        sb.DrawString(_game.Font, typeTag, new Vector2(PokemonGame.VirtualWidth - tagSize.X - 6, 2), Color.White);

        // Description
        sb.DrawString(_game.Font, area.Description, new Vector2(6, 16), new Color(240, 240, 240));
    }

    private void DrawNavigation(SpriteBatch sb)
    {
        var area = CurrentArea;
        if (area == null) return;

        var connections = GetAccessibleConnections(area);

        // Title
        sb.DrawString(_game.Font, "Where to go?", new Vector2(6, 34), TextColor);

        // Connection list
        int y = 48;
        int maxVisible = 6;
        for (int i = _navScroll; i < Math.Min(connections.Count, _navScroll + maxVisible); i++)
        {
            var (conn, locked) = connections[i];
            var targetArea = _game.GameData.GetArea(conn.AreaId);
            string areaName = targetArea?.Name ?? conn.AreaId;
            string direction = conn.Direction;

            bool selected = i == _navCursor;
            Color textCol = locked ? LockedColor : (selected ? TextColor : new Color(80, 80, 80));

            if (selected)
                _game.DrawRect(sb, new Rectangle(4, y - 1, PokemonGame.VirtualWidth - 8, 14), HighlightColor);

            string lockIcon = locked ? " [LOCKED]" : "";
            string prefix = selected ? "> " : "  ";
            sb.DrawString(_game.Font, $"{prefix}{direction}: {areaName}{lockIcon}", new Vector2(6, y), textCol);
            y += 15;
        }

        // Scroll indicators
        if (_navScroll > 0)
            sb.DrawString(_game.Font, "^", new Vector2(PokemonGame.VirtualWidth - 14, 48), TextColor);
        if (_navScroll + maxVisible < connections.Count)
            sb.DrawString(_game.Font, "v", new Vector2(PokemonGame.VirtualWidth - 14, y - 15), TextColor);

        // Hint
        sb.DrawString(_game.Font, "X:Back  Z:Go", new Vector2(6, 140), new Color(120, 120, 120));
    }

    private void DrawAreaMenu(SpriteBatch sb)
    {
        int y = 36;
        for (int i = 0; i < _menuOptions.Count; i++)
        {
            bool selected = i == _menuCursor;
            if (selected)
                _game.DrawRect(sb, new Rectangle(4, y - 1, PokemonGame.VirtualWidth - 8, 14), HighlightColor);

            string prefix = selected ? "> " : "  ";
            sb.DrawString(_game.Font, $"{prefix}{_menuOptions[i]}", new Vector2(6, y), TextColor);
            y += 15;
        }
    }

    private void DrawMessage(SpriteBatch sb)
    {
        var box = new Rectangle(4, 36, PokemonGame.VirtualWidth - 8, PokemonGame.VirtualHeight - 56);
        _game.DrawRect(sb, box, BoxColor);
        _game.DrawBorder(sb, box, BorderColor, 2);

        sb.DrawString(_game.Font, _message, new Vector2(12, 42), TextColor);

        // Advance prompt
        if (_messageTimer > 0.5f)
        {
            float blink = (float)Math.Sin(DateTime.Now.Millisecond / 200.0 * Math.PI);
            if (blink > 0)
                sb.DrawString(_game.Font, "v", new Vector2(PokemonGame.VirtualWidth - 16, PokemonGame.VirtualHeight - 24), TextColor);
        }
    }

    private void DrawStatusBar(SpriteBatch sb)
    {
        int y = PokemonGame.VirtualHeight - 14;
        _game.DrawRect(sb, new Rectangle(0, y, PokemonGame.VirtualWidth, 14), new Color(40, 40, 40));

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
