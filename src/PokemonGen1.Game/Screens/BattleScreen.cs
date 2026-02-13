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
    Animating,
    ActionSelect,
    MoveSelect,
    BattleOver
}

public class BattleScreen : IScreen
{
    // Animation command types - queued and played sequentially
    private abstract record AnimCmd;
    private record TypeTextCmd(string Text, float PostDelay) : AnimCmd;
    private record HitEffectCmd(BattleSide Target, float Duration) : AnimCmd;
    private record AnimateHpCmd(BattleSide Side, float TargetPercent) : AnimCmd;
    private record PauseCmd(float Duration) : AnimCmd;

    private readonly PokemonGame _game;
    private readonly BattleState _state;
    private readonly BattleEngine _engine;
    private readonly Random _rng = new();
    private readonly Action<BattleOutcome>? _onBattleEnd;

    private BattlePhase _phase = BattlePhase.Animating;
    private int _menuCursor;
    private int _moveCursor;

    // Animation queue
    private readonly Queue<AnimCmd> _animQueue = new();
    private AnimCmd? _activeCmd;
    private float _animTimer;

    // Text display
    private string _currentText = "";
    private int _textCharIndex;
    private float _textTimer;
    private const float TextSpeed = 0.02f;
    private bool _textComplete;

    // HP bar animation
    private float _playerHpDisplay;
    private float _opponentHpDisplay;
    private float _playerHpTarget;
    private float _opponentHpTarget;
    private const float HpBarSpeed = 1.5f;

    // Sprite hit effects (flash + shake)
    private float _playerFlashTimer;
    private float _opponentFlashTimer;

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

    public bool IsOverlay => false;
    public bool BlocksUpdate => true;

    public BattleScreen(PokemonGame game, BattleState state, Action<BattleOutcome>? onBattleEnd = null)
    {
        _game = game;
        _state = state;
        _engine = new BattleEngine(state, game.GameData, _rng);
        _onBattleEnd = onBattleEnd;

        _playerHpDisplay = _playerHpTarget = GetHpPercent(state.PlayerActive);
        _opponentHpDisplay = _opponentHpTarget = GetHpPercent(state.OpponentActive);
    }

    private ScreenManager _manager = null!;

    public void Enter(ScreenManager manager)
    {
        _manager = manager;

        // Queue intro animation - auto-advances, no button press needed
        if (_state.Type == BattleType.Trainer && _state.OpponentTrainer != null)
        {
            string trainerName = _state.OpponentTrainer.Title ?? _state.OpponentTrainer.Name;
            _animQueue.Enqueue(new TypeTextCmd($"{trainerName} wants to fight!", 0.5f));
            if (_state.OpponentTrainer.BeforeBattleDialog.Length > 0)
            {
                foreach (var line in _state.OpponentTrainer.BeforeBattleDialog)
                    _animQueue.Enqueue(new TypeTextCmd(line, 0.5f));
            }
            _animQueue.Enqueue(new TypeTextCmd(
                $"{trainerName} sent out {_state.OpponentActive.Species.Name}!", 0.5f));
        }
        else
        {
            _animQueue.Enqueue(new TypeTextCmd(
                $"A wild {_state.OpponentActive.Species.Name} appeared!", 0.7f));
        }
        _animQueue.Enqueue(new TypeTextCmd(
            $"Go! {GetPlayerName()}!", 0.5f));
        _phase = BattlePhase.Animating;
    }

    public void Exit() { }

    public void Update(GameTime gameTime, InputManager input)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        AnimateHpBars(dt);
        UpdateSpriteEffects(dt);

        switch (_phase)
        {
            case BattlePhase.Animating:
                UpdateAnimating(dt, input);
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
                    if (_onBattleEnd != null)
                    {
                        _manager.Pop();
                        _onBattleEnd(_state.Outcome ?? BattleOutcome.PlayerWin);
                    }
                    else
                    {
                        _manager.Replace(new TitleScreen(_game));
                    }
                }
                break;
        }
    }

    #region Animation System

    private void UpdateAnimating(float dt, InputManager input)
    {
        UpdateText(dt);

        if (_activeCmd == null)
        {
            if (_animQueue.Count == 0)
            {
                // All animations complete - transition to next phase
                if (_state.IsOver || _state.PlayerActive.Pokemon.IsFainted)
                    _phase = BattlePhase.BattleOver;
                else
                    _phase = BattlePhase.ActionSelect;
                return;
            }

            _activeCmd = _animQueue.Dequeue();
            _animTimer = 0;
            InitCommand(_activeCmd);
        }

        if (UpdateCommand(_activeCmd, dt, input))
            _activeCmd = null;
    }

    private void InitCommand(AnimCmd cmd)
    {
        switch (cmd)
        {
            case TypeTextCmd txt:
                SetText(txt.Text);
                break;
            case HitEffectCmd hit:
                if (hit.Target == BattleSide.Player)
                    _playerFlashTimer = hit.Duration;
                else
                    _opponentFlashTimer = hit.Duration;
                break;
            case AnimateHpCmd hp:
                if (hp.Side == BattleSide.Player)
                    _playerHpTarget = hp.TargetPercent;
                else
                    _opponentHpTarget = hp.TargetPercent;
                break;
        }
    }

    private bool UpdateCommand(AnimCmd cmd, float dt, InputManager input)
    {
        bool confirm = input.IsPressed(InputAction.Confirm);

        switch (cmd)
        {
            case TypeTextCmd txt:
                if (!_textComplete)
                {
                    if (confirm)
                    {
                        _textCharIndex = _currentText.Length;
                        _textComplete = true;
                    }
                    return false;
                }
                // Text done - count post-delay before auto-advancing
                _animTimer += dt;
                return confirm || _animTimer >= txt.PostDelay;

            case HitEffectCmd hit:
                if (confirm)
                {
                    _playerFlashTimer = 0;
                    _opponentFlashTimer = 0;
                    return true;
                }
                float flashTimer = hit.Target == BattleSide.Player
                    ? _playerFlashTimer : _opponentFlashTimer;
                return flashTimer <= 0;

            case AnimateHpCmd hp:
                if (confirm)
                {
                    // Skip HP animation to target
                    if (hp.Side == BattleSide.Player)
                        _playerHpDisplay = hp.TargetPercent;
                    else
                        _opponentHpDisplay = hp.TargetPercent;
                    return true;
                }
                float current = hp.Side == BattleSide.Player
                    ? _playerHpDisplay : _opponentHpDisplay;
                return Math.Abs(current - hp.TargetPercent) < 0.005f;

            case PauseCmd pause:
                _animTimer += dt;
                return confirm || _animTimer >= pause.Duration;
        }
        return true;
    }

    private void EnqueueBattleEvents(IEnumerable<BattleEvent> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case MoveUsedEvent moveUsed:
                    string who = moveUsed.Attacker == BattleSide.Player
                        ? GetPlayerName()
                        : $"Enemy {_state.OpponentActive.Species.Name}";
                    _animQueue.Enqueue(new TypeTextCmd($"{who} used {moveUsed.MoveName}!", 0.2f));
                    break;

                case DamageDealtEvent dmg:
                    _animQueue.Enqueue(new HitEffectCmd(dmg.Target, 0.3f));
                    break;

                case HpChangedEvent hp:
                    float pct = hp.MaxHp > 0 ? (float)hp.NewHp / hp.MaxHp : 0;
                    _animQueue.Enqueue(new AnimateHpCmd(hp.Side, pct));
                    break;

                case TextEvent text:
                    _animQueue.Enqueue(new TypeTextCmd(text.Message, 0.3f));
                    break;

                case FaintedEvent fainted:
                    _animQueue.Enqueue(new TypeTextCmd($"{fainted.PokemonName} fainted!", 0.5f));
                    break;

                case BattleEndedEvent ended:
                    string msg = ended.Outcome switch
                    {
                        BattleOutcome.PlayerWin => "You won the battle!",
                        BattleOutcome.PlayerLose => "You blacked out!",
                        BattleOutcome.PlayerFled => "Got away safely!",
                        _ => "Battle over!"
                    };
                    _animQueue.Enqueue(new TypeTextCmd(msg, 0f));
                    break;

                case MoveMissedEvent missed:
                    string atkr = missed.Attacker == BattleSide.Player
                        ? GetPlayerName()
                        : $"Enemy {_state.OpponentActive.Species.Name}";
                    _animQueue.Enqueue(new TypeTextCmd($"{atkr}'s attack missed!", 0.3f));
                    break;

                // All other events (StatChangedEvent, RecoilEvent, DrainEvent, etc.)
                // are covered by the TextEvents the engine generates alongside them
            }
        }
    }

    #endregion

    #region Input Handling

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
                    _animQueue.Enqueue(new TypeTextCmd("No items in bag!", 0.3f));
                    _phase = BattlePhase.Animating;
                    break;
                case 2: // POKEMON
                    _animQueue.Enqueue(new TypeTextCmd("No other Pokemon!", 0.3f));
                    _phase = BattlePhase.Animating;
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
                _animQueue.Enqueue(new TypeTextCmd("No PP left!", 0.3f));
                _phase = BattlePhase.Animating;
            }
        }
    }

    private void ExecuteTurn(BattleAction playerAction)
    {
        var aiAction = TrainerAI.ChooseAction(_state, _game.GameData,
            _state.OpponentTrainer?.AiBehavior ?? AIBehavior.Random, _rng);

        var events = _engine.ExecuteTurn(playerAction, aiAction);
        EnqueueBattleEvents(events);
        _phase = BattlePhase.Animating;
    }

    #endregion

    #region Text & HP Animation

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
        _playerHpDisplay = MoveToward(_playerHpDisplay, _playerHpTarget, dt * HpBarSpeed);
        _opponentHpDisplay = MoveToward(_opponentHpDisplay, _opponentHpTarget, dt * HpBarSpeed);
    }

    private void UpdateSpriteEffects(float dt)
    {
        if (_playerFlashTimer > 0) _playerFlashTimer = Math.Max(0, _playerFlashTimer - dt);
        if (_opponentFlashTimer > 0) _opponentFlashTimer = Math.Max(0, _opponentFlashTimer - dt);
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

    #endregion

    #region Drawing

    public void Draw(SpriteBatch sb)
    {
        // Background
        _game.DrawRect(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, PokemonGame.VirtualHeight), BgColor);

        // Battle field (top area)
        DrawBattleField(sb);

        // Bottom area depends on phase
        switch (_phase)
        {
            case BattlePhase.Animating:
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

        // Battle area background
        _game.DrawRect(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, fieldHeight),
            new Color(200, 228, 200));

        // Platforms
        DrawPlatform(sb, 140, 32, 80, 12, EnemyPlatform);
        DrawPlatform(sb, 20, 72, 80, 12, PlayerPlatform);

        // Enemy Pokemon sprite with hit effects (flash + shake)
        {
            bool visible = _opponentFlashTimer <= 0 ||
                ((int)(_opponentFlashTimer / 0.05f) % 2 == 0);
            int shakeX = _opponentFlashTimer > 0
                ? (int)(Math.Sin(_opponentFlashTimer * 40) * 3) : 0;

            if (visible)
            {
                DrawPokemonSprite(sb, _state.OpponentActive.Species.DexNumber, isFront: true,
                    destRect: new Rectangle(152 + shakeX, -8, 48, 48));
            }
        }

        // Player Pokemon sprite with hit effects
        {
            bool visible = _playerFlashTimer <= 0 ||
                ((int)(_playerFlashTimer / 0.05f) % 2 == 0);
            int shakeX = _playerFlashTimer > 0
                ? (int)(Math.Sin(_playerFlashTimer * 40) * 3) : 0;

            if (visible)
            {
                DrawPokemonSprite(sb, _state.PlayerActive.Species.DexNumber, isFront: false,
                    destRect: new Rectangle(24 + shakeX, 28, 56, 56));
            }
        }

        // Info boxes
        DrawEnemyInfoBox(sb);
        DrawPlayerInfoBox(sb);
    }

    private void DrawPlatform(SpriteBatch sb, int x, int y, int width, int height, Color color)
    {
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

        string name = _state.OpponentActive.Species.Name;
        sb.DrawString(_game.Font, name, new Vector2(6, 4), TextColor);

        string level = $"Lv{_state.OpponentActive.Level}";
        var lvSize = _game.Font.MeasureString(level);
        sb.DrawString(_game.Font, level, new Vector2(box.Right - lvSize.X - 4, 4), TextColor);

        DrawHpBar(sb, 6, 16, 100, 6, _opponentHpDisplay);
        DrawStatusBadge(sb, 6, 24, _state.OpponentActive.Pokemon.Status);
    }

    private void DrawPlayerInfoBox(SpriteBatch sb)
    {
        var box = new Rectangle(118, 56, 120, 40);
        _game.DrawRect(sb, box, BoxColor);
        _game.DrawBorder(sb, box, BorderColor, 2);

        string name = _state.PlayerActive.Pokemon.Nickname ?? _state.PlayerActive.Species.Name;
        sb.DrawString(_game.Font, name, new Vector2(122, 58), TextColor);

        string level = $"Lv{_state.PlayerActive.Level}";
        var lvSize = _game.Font.MeasureString(level);
        sb.DrawString(_game.Font, level, new Vector2(box.Right - lvSize.X - 4, 58), TextColor);

        DrawHpBar(sb, 122, 70, 112, 6, _playerHpDisplay);

        // HP numbers animate with bar
        int maxHp = _state.PlayerActive.MaxHp;
        int displayHp = (int)Math.Ceiling(_playerHpDisplay * maxHp);
        displayHp = Math.Clamp(displayHp, 0, maxHp);
        string hpText = $"{displayHp}/{maxHp}";
        var hpSize = _game.Font.MeasureString(hpText);
        sb.DrawString(_game.Font, hpText, new Vector2(box.Right - hpSize.X - 4, 80), TextColor);

        DrawStatusBadge(sb, 122, 80, _state.PlayerActive.Pokemon.Status);
    }

    private void DrawHpBar(SpriteBatch sb, int x, int y, int width, int height, float percent)
    {
        _game.DrawRect(sb, new Rectangle(x, y, width, height), new Color(80, 80, 80));

        int fillWidth = (int)(width * Math.Clamp(percent, 0, 1));
        Color hpColor = percent > 0.5f ? HpGreen : (percent > 0.2f ? HpYellow : HpRed);
        if (fillWidth > 0)
            _game.DrawRect(sb, new Rectangle(x, y, fillWidth, height), hpColor);

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

        // Text with character reveal
        string displayText = _currentText[..Math.Min(_textCharIndex, _currentText.Length)];
        sb.DrawString(_game.Font, displayText, new Vector2(8, 104), TextColor);

        // Blinking advance arrow only when waiting for player input
        if (_phase == BattlePhase.BattleOver && _textComplete)
        {
            float blink = (float)Math.Sin(DateTime.Now.Millisecond / 200.0 * Math.PI);
            if (blink > 0)
                sb.DrawString(_game.Font, "v", new Vector2(224, 148), ArrowColor);
        }
    }

    private void DrawActionMenu(SpriteBatch sb)
    {
        var textBox = new Rectangle(0, 96, 120, 64);
        _game.DrawRect(sb, textBox, BoxColor);
        _game.DrawBorder(sb, textBox, BorderColor, 2);
        sb.DrawString(_game.Font, $"What will\n{GetPlayerName()} do?",
            new Vector2(8, 104), TextColor);

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
            sb.DrawString(_game.Font, options[i], new Vector2(colX[col], rowY[row]), TextColor);
        }

        int curCol = _menuCursor % 2;
        int curRow = _menuCursor / 2;
        sb.DrawString(_game.Font, ">", new Vector2(colX[curCol] - 10, rowY[curRow]), ArrowColor);
    }

    private void DrawMoveMenu(SpriteBatch sb)
    {
        var moves = _state.PlayerActive.Pokemon.Moves;

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

        int curCol = _moveCursor % 2;
        int curRow = _moveCursor / 2;
        sb.DrawString(_game.Font, ">", new Vector2(colX[curCol] - 10, rowY[curRow]), ArrowColor);

        if (_moveCursor < moves.Length)
        {
            var ppBox = new Rectangle(160, 96, 80, 64);
            _game.DrawRect(sb, ppBox, BoxColor);
            _game.DrawBorder(sb, ppBox, BorderColor, 2);

            var moveData = _game.GameData.GetMove(moves[_moveCursor].MoveId);
            var moveInst = moves[_moveCursor];

            sb.DrawString(_game.Font, "TYPE/", new Vector2(166, 104), TextColor);
            Color typeColor = GetTypeColor(moveData.Type);
            _game.DrawRect(sb, new Rectangle(166, 116, 68, 10), typeColor);
            sb.DrawString(_game.Font, moveData.Type.ToString(), new Vector2(168, 116), Color.White);

            sb.DrawString(_game.Font, $"PP {moveInst.CurrentPP}/{moveInst.MaxPP}",
                new Vector2(166, 140), TextColor);
        }
    }

    #endregion

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
