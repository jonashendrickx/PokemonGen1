using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.Battle;
using PokemonGen1.Core.Data;
using PokemonGen1.Core.Moves;
using PokemonGen1.Core.Trainers;
using PokemonGen1.Game.Input;

namespace PokemonGen1.Game.Screens;

public enum BattlePhase
{
    Intro,
    ActionSelect,
    MoveSelect,
    Animating,
    EventDisplay,
    BattleOver,
    WaitingForSwitch
}

public class BattleScreen : IScreen
{
    private readonly PokemonGame _game;
    private readonly BattleState _state;
    private readonly BattleEngine _engine;
    private readonly Random _rng = new();

    private BattlePhase _phase = BattlePhase.Intro;
    private int _menuCursor;
    private int _moveCursor;
    private float _introTimer;

    // Event animation
    private Queue<BattleEvent> _eventQueue = new();
    private BattleEvent? _currentEvent;
    private string _currentText = "";
    private int _textCharIndex;
    private float _textTimer;
    private const float TextSpeed = 0.03f;
    private bool _textComplete;
    private float _eventDisplayTimer;

    // HP bar animation
    private float _playerHpDisplay;
    private float _opponentHpDisplay;

    // Colors
    private static readonly Color BgColor = new(248, 248, 248);
    private static readonly Color BoxColor = new(248, 248, 248);
    private static readonly Color BorderColor = new(40, 40, 40);
    private static readonly Color TextColor = new(40, 40, 40);
    private static readonly Color HpGreen = new(0, 200, 0);
    private static readonly Color HpYellow = new(200, 200, 0);
    private static readonly Color HpRed = new(200, 0, 0);
    private static readonly Color ArrowColor = new(40, 40, 40);
    private static readonly Color EnemyPlatform = new(120, 180, 120);
    private static readonly Color PlayerPlatform = new(100, 160, 100);
    private static readonly Color TypeBoxColor = new(200, 200, 200);

    public bool IsOverlay => false;
    public bool BlocksUpdate => true;

    public BattleScreen(PokemonGame game, BattleState state)
    {
        _game = game;
        _state = state;
        _engine = new BattleEngine(state, game.GameData, _rng);

        _playerHpDisplay = GetHpPercent(state.PlayerActive);
        _opponentHpDisplay = GetHpPercent(state.OpponentActive);
    }

    private ScreenManager _manager = null!;
    public void Enter(ScreenManager manager)
    {
        _manager = manager;
        _phase = BattlePhase.Intro;
        _introTimer = 0;
        SetText($"A wild {_state.OpponentActive.Species.Name} appeared!");
    }

    public void Exit() { }

    public void Update(GameTime gameTime, InputManager input)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Animate HP bars
        AnimateHpBars(dt);

        switch (_phase)
        {
            case BattlePhase.Intro:
                UpdateText(dt);
                if (_textComplete && input.IsPressed(InputAction.Confirm))
                {
                    SetText($"Go! {GetPlayerName()}!");
                    _phase = BattlePhase.EventDisplay;
                    _eventQueue.Clear();
                }
                break;

            case BattlePhase.EventDisplay:
                UpdateText(dt);
                if (_textComplete)
                {
                    if (input.IsPressed(InputAction.Confirm))
                    {
                        if (_eventQueue.Count > 0)
                            ProcessNextEvent();
                        else if (_state.IsOver)
                            _phase = BattlePhase.BattleOver;
                        else if (_state.PlayerActive.Pokemon.IsFainted)
                            _phase = BattlePhase.BattleOver; // Simplified: no switch support yet
                        else
                            _phase = BattlePhase.ActionSelect;
                    }
                }
                break;

            case BattlePhase.ActionSelect:
                UpdateActionSelect(input);
                break;

            case BattlePhase.MoveSelect:
                UpdateMoveSelect(input);
                break;

            case BattlePhase.BattleOver:
                if (input.IsPressed(InputAction.Confirm))
                {
                    _manager.Replace(new TitleScreen(_game));
                }
                break;
        }
    }

    private void UpdateActionSelect(InputManager input)
    {
        if (input.IsPressed(InputAction.Up)) _menuCursor = Math.Max(0, _menuCursor - 2);
        if (input.IsPressed(InputAction.Down)) _menuCursor = Math.Min(3, _menuCursor + 2);
        if (input.IsPressed(InputAction.Left)) _menuCursor = Math.Max(0, _menuCursor - 1);
        if (input.IsPressed(InputAction.Right)) _menuCursor = Math.Min(3, _menuCursor + 1);

        if (input.IsPressed(InputAction.Confirm))
        {
            switch (_menuCursor)
            {
                case 0: // FIGHT
                    _phase = BattlePhase.MoveSelect;
                    _moveCursor = 0;
                    break;
                case 1: // BAG
                    SetText("No items in bag!");
                    _phase = BattlePhase.EventDisplay;
                    break;
                case 2: // POKEMON
                    SetText("No other Pokemon!");
                    _phase = BattlePhase.EventDisplay;
                    break;
                case 3: // RUN
                    ExecuteTurn(new RunAction());
                    break;
            }
        }
    }

    private void UpdateMoveSelect(InputManager input)
    {
        var moves = _state.PlayerActive.Pokemon.Moves;
        int moveCount = moves.Length;

        if (input.IsPressed(InputAction.Up)) _moveCursor = Math.Max(0, _moveCursor - 2);
        if (input.IsPressed(InputAction.Down)) _moveCursor = Math.Min(moveCount - 1, _moveCursor + 2);
        if (input.IsPressed(InputAction.Left)) _moveCursor = Math.Max(0, _moveCursor - 1);
        if (input.IsPressed(InputAction.Right)) _moveCursor = Math.Min(moveCount - 1, _moveCursor + 1);

        if (input.IsPressed(InputAction.Cancel))
        {
            _phase = BattlePhase.ActionSelect;
            return;
        }

        if (input.IsPressed(InputAction.Confirm))
        {
            if (_moveCursor < moveCount && moves[_moveCursor].CurrentPP > 0)
            {
                ExecuteTurn(new FightAction(_moveCursor));
            }
            else
            {
                SetText("No PP left!");
                _phase = BattlePhase.EventDisplay;
            }
        }
    }

    private void ExecuteTurn(BattleAction playerAction)
    {
        // AI chooses action
        var aiAction = TrainerAI.ChooseAction(_state, _game.GameData,
            _state.OpponentTrainer?.AiBehavior ?? AIBehavior.Random, _rng);

        var events = _engine.ExecuteTurn(playerAction, aiAction);

        _eventQueue = new Queue<BattleEvent>(events);
        _phase = BattlePhase.EventDisplay;

        if (_eventQueue.Count > 0)
            ProcessNextEvent();
        else
            SetText("...");
    }

    private void ProcessNextEvent()
    {
        while (_eventQueue.Count > 0)
        {
            _currentEvent = _eventQueue.Dequeue();

            switch (_currentEvent)
            {
                case HpChangedEvent hp:
                    // Update target HP display
                    if (hp.Side == BattleSide.Player)
                        _playerHpDisplay = hp.MaxHp > 0 ? (float)hp.NewHp / hp.MaxHp : 0;
                    else
                        _opponentHpDisplay = hp.MaxHp > 0 ? (float)hp.NewHp / hp.MaxHp : 0;
                    continue; // Don't show text, process next event

                case DamageDealtEvent:
                case StatChangedEvent:
                case MultiHitEvent:
                case RecoilEvent:
                case DrainEvent:
                    continue; // Skip these, the text events that follow are enough

                case MoveUsedEvent moveUsed:
                    string who = moveUsed.Attacker == BattleSide.Player
                        ? GetPlayerName() : $"Enemy {_state.OpponentActive.Species.Name}";
                    SetText($"{who} used {moveUsed.MoveName}!");
                    return;

                case TextEvent text:
                    SetText(text.Message);
                    return;

                case FaintedEvent fainted:
                    SetText($"{fainted.PokemonName} fainted!");
                    return;

                case BattleEndedEvent ended:
                    SetText(ended.Outcome switch
                    {
                        BattleOutcome.PlayerWin => "You won the battle!",
                        BattleOutcome.PlayerLose => "You blacked out!",
                        BattleOutcome.PlayerFled => "Got away safely!",
                        _ => "Battle over!"
                    });
                    return;

                case MoveMissedEvent missed:
                    string attacker = missed.Attacker == BattleSide.Player
                        ? GetPlayerName() : $"Enemy {_state.OpponentActive.Species.Name}";
                    SetText($"{attacker}'s attack missed!");
                    return;

                case StatusAppliedEvent:
                case StatusPreventedMoveEvent:
                case StatusDamageEvent:
                case SwitchEvent:
                case ExperienceGainedEvent:
                case ChargingEvent:
                case RechargeEvent:
                case ConfusionHitSelfEvent:
                case SubstituteCreatedEvent:
                case SubstituteBrokeEvent:
                case OhkoEvent:
                case MoveFailedEvent:
                    continue; // Text events handle the messaging

                default:
                    continue;
            }
        }

        // No more events with text
        if (_state.IsOver)
        {
            _phase = BattlePhase.BattleOver;
        }
    }

    public void Draw(SpriteBatch sb)
    {
        // Background
        _game.DrawRect(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, PokemonGame.VirtualHeight), BgColor);

        // Battle field (top area)
        DrawBattleField(sb);

        // Bottom area depends on phase
        switch (_phase)
        {
            case BattlePhase.Intro:
            case BattlePhase.EventDisplay:
            case BattlePhase.BattleOver:
                DrawTextBox(sb);
                break;
            case BattlePhase.ActionSelect:
                DrawActionMenu(sb);
                break;
            case BattlePhase.MoveSelect:
                DrawMoveMenu(sb);
                break;
        }
    }

    private void DrawBattleField(SpriteBatch sb)
    {
        int fieldHeight = 96;

        // Battle area background - light grassy color
        _game.DrawRect(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, fieldHeight),
            new Color(200, 228, 200));

        // Enemy platform (top-right)
        DrawPlatform(sb, 140, 32, 80, 12, EnemyPlatform);

        // Player platform (bottom-left)
        DrawPlatform(sb, 20, 72, 80, 12, PlayerPlatform);

        // Enemy Pokemon sprite (front-facing, top-right area)
        DrawPokemonSprite(sb, _state.OpponentActive.Species.DexNumber, isFront: true,
            destRect: new Rectangle(152, -8, 48, 48));

        // Player Pokemon sprite (back-facing, bottom-left area, larger since closer)
        DrawPokemonSprite(sb, _state.PlayerActive.Species.DexNumber, isFront: false,
            destRect: new Rectangle(24, 28, 56, 56));

        // Enemy info box (top-left area)
        DrawEnemyInfoBox(sb);

        // Player info box (bottom-right area)
        DrawPlayerInfoBox(sb);
    }

    private void DrawPlatform(SpriteBatch sb, int x, int y, int width, int height, Color color)
    {
        // Simple ellipse-like platform
        _game.DrawRect(sb, new Rectangle(x, y, width, height / 2), color);
        _game.DrawRect(sb, new Rectangle(x + 2, y + height / 2, width - 4, height / 2),
            new Color(color.R * 3 / 4, color.G * 3 / 4, color.B * 3 / 4));
    }

    private void DrawPokemonSprite(SpriteBatch sb, int dexNumber, bool isFront, Rectangle destRect)
    {
        var sprite = isFront
            ? _game.Sprites.GetFrontSprite(dexNumber)
            : _game.Sprites.GetBackSprite(dexNumber);

        if (sprite != null)
        {
            sb.Draw(sprite, destRect, Color.White);
        }
        else
        {
            // Fallback: colored rectangle if sprite not found
            var species = _game.GameData.GetSpecies(dexNumber);
            Color fallbackColor = GetTypeColor(species.Type1);
            _game.DrawRect(sb, destRect, fallbackColor);
            _game.DrawBorder(sb, destRect, BorderColor);
        }
    }

    private void DrawEnemyInfoBox(SpriteBatch sb)
    {
        var box = new Rectangle(2, 2, 110, 30);
        _game.DrawRect(sb, box, BoxColor);
        _game.DrawBorder(sb, box, BorderColor, 2);

        // Name and level
        string name = _state.OpponentActive.Species.Name;
        sb.DrawString(_game.Font, name, new Vector2(6, 4), TextColor);

        string level = $"Lv{_state.OpponentActive.Level}";
        var lvSize = _game.Font.MeasureString(level);
        sb.DrawString(_game.Font, level, new Vector2(box.Right - lvSize.X - 4, 4), TextColor);

        // HP bar
        DrawHpBar(sb, 6, 16, 100, 6, _opponentHpDisplay);

        // Status condition
        DrawStatusBadge(sb, 6, 24, _state.OpponentActive.Pokemon.Status);
    }

    private void DrawPlayerInfoBox(SpriteBatch sb)
    {
        var box = new Rectangle(118, 56, 120, 40);
        _game.DrawRect(sb, box, BoxColor);
        _game.DrawBorder(sb, box, BorderColor, 2);

        // Name and level
        string name = _state.PlayerActive.Pokemon.Nickname ?? _state.PlayerActive.Species.Name;
        sb.DrawString(_game.Font, name, new Vector2(122, 58), TextColor);

        string level = $"Lv{_state.PlayerActive.Level}";
        var lvSize = _game.Font.MeasureString(level);
        sb.DrawString(_game.Font, level, new Vector2(box.Right - lvSize.X - 4, 58), TextColor);

        // HP bar
        DrawHpBar(sb, 122, 70, 112, 6, _playerHpDisplay);

        // HP numbers
        int currentHp = _state.PlayerActive.Pokemon.CurrentHp;
        int maxHp = _state.PlayerActive.MaxHp;
        string hpText = $"{currentHp}/{maxHp}";
        var hpSize = _game.Font.MeasureString(hpText);
        sb.DrawString(_game.Font, hpText, new Vector2(box.Right - hpSize.X - 4, 80), TextColor);

        // Status condition
        DrawStatusBadge(sb, 122, 80, _state.PlayerActive.Pokemon.Status);
    }

    private void DrawHpBar(SpriteBatch sb, int x, int y, int width, int height, float percent)
    {
        // Background
        _game.DrawRect(sb, new Rectangle(x, y, width, height), new Color(80, 80, 80));

        // HP fill
        int fillWidth = (int)(width * Math.Clamp(percent, 0, 1));
        Color hpColor = percent > 0.5f ? HpGreen : (percent > 0.2f ? HpYellow : HpRed);
        if (fillWidth > 0)
            _game.DrawRect(sb, new Rectangle(x, y, fillWidth, height), hpColor);

        // Label
        sb.DrawString(_game.Font, "HP", new Vector2(x - 18, y - 2), TextColor);
    }

    private void DrawStatusBadge(SpriteBatch sb, int x, int y, Core.Battle.StatusCondition status)
    {
        if (status == Core.Battle.StatusCondition.None) return;

        string text = status switch
        {
            Core.Battle.StatusCondition.Burn => "BRN",
            Core.Battle.StatusCondition.Freeze => "FRZ",
            Core.Battle.StatusCondition.Paralysis => "PAR",
            Core.Battle.StatusCondition.Poison => "PSN",
            Core.Battle.StatusCondition.BadlyPoisoned => "PSN",
            Core.Battle.StatusCondition.Sleep => "SLP",
            _ => ""
        };

        if (!string.IsNullOrEmpty(text))
        {
            Color badgeColor = status switch
            {
                Core.Battle.StatusCondition.Burn => Color.OrangeRed,
                Core.Battle.StatusCondition.Freeze => Color.LightBlue,
                Core.Battle.StatusCondition.Paralysis => Color.Gold,
                Core.Battle.StatusCondition.Poison or Core.Battle.StatusCondition.BadlyPoisoned => Color.Purple,
                Core.Battle.StatusCondition.Sleep => Color.Gray,
                _ => Color.White
            };
            var size = _game.Font.MeasureString(text);
            _game.DrawRect(sb, new Rectangle(x, y, (int)size.X + 4, (int)size.Y + 2), badgeColor);
            sb.DrawString(_game.Font, text, new Vector2(x + 2, y + 1), Color.White);
        }
    }

    private void DrawTextBox(SpriteBatch sb)
    {
        var box = new Rectangle(0, 96, PokemonGame.VirtualWidth, 64);
        _game.DrawRect(sb, box, BoxColor);
        _game.DrawBorder(sb, box, BorderColor, 2);

        // Draw current text with character reveal
        string displayText = _currentText.Substring(0, Math.Min(_textCharIndex, _currentText.Length));
        sb.DrawString(_game.Font, displayText, new Vector2(8, 104), TextColor);

        // Blinking advance arrow
        if (_textComplete)
        {
            float blink = (float)Math.Sin(DateTime.Now.Millisecond / 200.0 * Math.PI);
            if (blink > 0)
            {
                sb.DrawString(_game.Font, "v", new Vector2(224, 148), ArrowColor);
            }
        }
    }

    private void DrawActionMenu(SpriteBatch sb)
    {
        // Text box on left
        var textBox = new Rectangle(0, 96, 120, 64);
        _game.DrawRect(sb, textBox, BoxColor);
        _game.DrawBorder(sb, textBox, BorderColor, 2);
        sb.DrawString(_game.Font, $"What will\n{GetPlayerName()} do?",
            new Vector2(8, 104), TextColor);

        // Action menu on right
        var menuBox = new Rectangle(120, 96, 120, 64);
        _game.DrawRect(sb, menuBox, BoxColor);
        _game.DrawBorder(sb, menuBox, BorderColor, 2);

        string[] options = { "FIGHT", "BAG", "PKMN", "RUN" };
        int[] colX = { 132, 192 };
        int[] rowY = { 104, 128 };

        for (int i = 0; i < 4; i++)
        {
            int col = i % 2;
            int row = i / 2;
            Color color = TextColor;
            sb.DrawString(_game.Font, options[i], new Vector2(colX[col], rowY[row]), color);
        }

        // Draw cursor arrow
        int curCol = _menuCursor % 2;
        int curRow = _menuCursor / 2;
        sb.DrawString(_game.Font, ">", new Vector2(colX[curCol] - 10, rowY[curRow]), ArrowColor);
    }

    private void DrawMoveMenu(SpriteBatch sb)
    {
        var moves = _state.PlayerActive.Pokemon.Moves;

        // Move list box (left side)
        var moveBox = new Rectangle(0, 96, 160, 64);
        _game.DrawRect(sb, moveBox, BoxColor);
        _game.DrawBorder(sb, moveBox, BorderColor, 2);

        int[] colX = { 16, 84 };
        int[] rowY = { 104, 128 };

        for (int i = 0; i < moves.Length && i < 4; i++)
        {
            var move = _game.GameData.GetMove(moves[i].MoveId);
            int col = i % 2;
            int row = i / 2;
            Color color = moves[i].CurrentPP > 0 ? TextColor : Color.Gray;
            sb.DrawString(_game.Font, move.Name, new Vector2(colX[col], rowY[row]), color);
        }

        // Cursor
        int curCol = _moveCursor % 2;
        int curRow = _moveCursor / 2;
        sb.DrawString(_game.Font, ">", new Vector2(colX[curCol] - 10, rowY[curRow]), ArrowColor);

        // PP and type info box (right side)
        if (_moveCursor < moves.Length)
        {
            var ppBox = new Rectangle(160, 96, 80, 64);
            _game.DrawRect(sb, ppBox, BoxColor);
            _game.DrawBorder(sb, ppBox, BorderColor, 2);

            var moveData = _game.GameData.GetMove(moves[_moveCursor].MoveId);
            var moveInst = moves[_moveCursor];

            // Type
            sb.DrawString(_game.Font, $"TYPE/", new Vector2(166, 104), TextColor);
            Color typeColor = GetTypeColor(moveData.Type);
            _game.DrawRect(sb, new Rectangle(166, 116, 68, 10), typeColor);
            sb.DrawString(_game.Font, moveData.Type.ToString(), new Vector2(168, 116), Color.White);

            // PP
            sb.DrawString(_game.Font, $"PP {moveInst.CurrentPP}/{moveInst.MaxPP}",
                new Vector2(166, 140), TextColor);
        }
    }

    // Text rendering helpers
    private void SetText(string text)
    {
        _currentText = text;
        _textCharIndex = 0;
        _textTimer = 0;
        _textComplete = false;
    }

    private void UpdateText(float dt)
    {
        if (_textComplete) return;

        _textTimer += dt;
        while (_textTimer >= TextSpeed && _textCharIndex < _currentText.Length)
        {
            _textCharIndex++;
            _textTimer -= TextSpeed;
        }

        if (_textCharIndex >= _currentText.Length)
            _textComplete = true;
    }

    private void AnimateHpBars(float dt)
    {
        float targetPlayer = GetHpPercent(_state.PlayerActive);
        float targetOpp = GetHpPercent(_state.OpponentActive);

        _playerHpDisplay = MoveToward(_playerHpDisplay, targetPlayer, dt * 2f);
        _opponentHpDisplay = MoveToward(_opponentHpDisplay, targetOpp, dt * 2f);
    }

    private static float MoveToward(float current, float target, float maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta) return target;
        return current + Math.Sign(target - current) * maxDelta;
    }

    private float GetHpPercent(BattlePokemon pokemon)
    {
        int max = pokemon.MaxHp;
        return max > 0 ? (float)pokemon.Pokemon.CurrentHp / max : 0;
    }

    private string GetPlayerName()
    {
        return _state.PlayerActive.Pokemon.Nickname ?? _state.PlayerActive.Species.Name;
    }

    private static Color GetTypeColor(Core.Types.PokemonType type) => type switch
    {
        Core.Types.PokemonType.Normal => new Color(168, 168, 120),
        Core.Types.PokemonType.Fire => new Color(240, 128, 48),
        Core.Types.PokemonType.Water => new Color(104, 144, 240),
        Core.Types.PokemonType.Electric => new Color(248, 208, 48),
        Core.Types.PokemonType.Grass => new Color(120, 200, 80),
        Core.Types.PokemonType.Ice => new Color(152, 216, 216),
        Core.Types.PokemonType.Fighting => new Color(192, 48, 40),
        Core.Types.PokemonType.Poison => new Color(160, 64, 160),
        Core.Types.PokemonType.Ground => new Color(224, 192, 104),
        Core.Types.PokemonType.Flying => new Color(168, 144, 240),
        Core.Types.PokemonType.Psychic => new Color(248, 88, 136),
        Core.Types.PokemonType.Bug => new Color(168, 184, 32),
        Core.Types.PokemonType.Rock => new Color(184, 160, 56),
        Core.Types.PokemonType.Ghost => new Color(112, 88, 152),
        Core.Types.PokemonType.Dragon => new Color(112, 56, 248),
        _ => Color.Gray
    };
}
