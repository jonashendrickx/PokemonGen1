using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.Battle;
using PokemonGen1.Core.Pokemon;
using PokemonGen1.Core.Trainers;
using PokemonGen1.Game.Input;

namespace PokemonGen1.Game.Screens;

public class TitleScreen : IScreen
{
    private readonly PokemonGame _game;
    private ScreenManager _manager = null!;
    private float _blinkTimer;
    private bool _showPress = true;

    public bool IsOverlay => false;
    public bool BlocksUpdate => true;

    public TitleScreen(PokemonGame game)
    {
        _game = game;
    }

    public void Enter(ScreenManager manager) => _manager = manager;
    public void Exit() { }

    public void Update(GameTime gameTime, InputManager input)
    {
        _blinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_blinkTimer > 0.5f)
        {
            _blinkTimer = 0;
            _showPress = !_showPress;
        }

        if (input.IsPressed(InputAction.Confirm) || input.IsPressed(InputAction.Start))
        {
            StartBattleDemo();
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Title background
        _game.DrawRect(spriteBatch, new Rectangle(0, 0, PokemonGame.VirtualWidth, PokemonGame.VirtualHeight),
            new Color(24, 24, 80));

        // Title text
        var titleText = "POKEMON";
        var titleSize = _game.Font.MeasureString(titleText);
        spriteBatch.DrawString(_game.Font, titleText,
            new Vector2((PokemonGame.VirtualWidth - titleSize.X) / 2, 30), Color.Yellow);

        var subText = "Generation I";
        var subSize = _game.Font.MeasureString(subText);
        spriteBatch.DrawString(_game.Font, subText,
            new Vector2((PokemonGame.VirtualWidth - subSize.X) / 2, 45), Color.White);

        var versionText = "Red + Blue + Yellow";
        var versionSize = _game.Font.MeasureString(versionText);
        spriteBatch.DrawString(_game.Font, versionText,
            new Vector2((PokemonGame.VirtualWidth - versionSize.X) / 2, 58), Color.LightGray);

        // Pokemon showcase
        DrawPokemonList(spriteBatch);

        // Press Start
        if (_showPress)
        {
            var pressText = "Press Z or ENTER to start";
            var pressSize = _game.Font.MeasureString(pressText);
            spriteBatch.DrawString(_game.Font, pressText,
                new Vector2((PokemonGame.VirtualWidth - pressSize.X) / 2, 135), Color.White);
        }
    }

    private void DrawPokemonList(SpriteBatch spriteBatch)
    {
        // Show the three starters
        string[] starters = { "Bulbasaur", "Charmander", "Squirtle" };
        Color[] colors = { Color.Green, Color.OrangeRed, Color.CornflowerBlue };
        int y = 80;
        for (int i = 0; i < 3; i++)
        {
            var text = starters[i];
            var size = _game.Font.MeasureString(text);
            // Draw small colored square as pokemon icon placeholder
            _game.DrawRect(spriteBatch, new Rectangle(
                (int)(PokemonGame.VirtualWidth / 2 - size.X / 2 - 12), y + i * 14, 8, 8), colors[i]);
            spriteBatch.DrawString(_game.Font, text,
                new Vector2(PokemonGame.VirtualWidth / 2 - size.X / 2, y + i * 14), Color.White);
        }
    }

    private void StartBattleDemo()
    {
        // Create a demo battle: Pikachu vs Geodude
        var pikachu = _game.GameData.GetSpecies(25);
        var geodude = _game.GameData.GetSpecies(74);

        var playerPokemon = PokemonInstance.Create(pikachu, 25);
        playerPokemon.Nickname = null;
        playerPokemon.OtName = "RED";
        // Give Pikachu some moves
        playerPokemon.Moves = _game.GameData.GetDefaultMoves(25, 25);
        // If no moves from learnset, give some manually
        if (playerPokemon.Moves.Length == 0)
        {
            playerPokemon.Moves = new[]
            {
                new Core.Moves.MoveInstance { MoveId = 85, CurrentPP = 15, MaxPP = 15 },  // Thunderbolt
                new Core.Moves.MoveInstance { MoveId = 98, CurrentPP = 30, MaxPP = 30 },  // Quick Attack
                new Core.Moves.MoveInstance { MoveId = 86, CurrentPP = 20, MaxPP = 20 },  // Thunder Wave
                new Core.Moves.MoveInstance { MoveId = 57, CurrentPP = 15, MaxPP = 15 },  // Surf
            };
        }
        playerPokemon.CurrentHp = playerPokemon.MaxHp(pikachu);

        var wildPokemon = PokemonInstance.Create(geodude, 22);
        wildPokemon.Moves = _game.GameData.GetDefaultMoves(74, 22);
        if (wildPokemon.Moves.Length == 0)
        {
            wildPokemon.Moves = new[]
            {
                new Core.Moves.MoveInstance { MoveId = 33, CurrentPP = 35, MaxPP = 35 },  // Tackle
                new Core.Moves.MoveInstance { MoveId = 111, CurrentPP = 40, MaxPP = 40 }, // Defense Curl (Harden-like)
                new Core.Moves.MoveInstance { MoveId = 88, CurrentPP = 15, MaxPP = 15 },  // Rock Throw
                new Core.Moves.MoveInstance { MoveId = 89, CurrentPP = 10, MaxPP = 10 },  // Earthquake
            };
        }
        wildPokemon.CurrentHp = wildPokemon.MaxHp(geodude);

        var battleState = new BattleState
        {
            Type = BattleType.Wild,
            PlayerParty = new[] { playerPokemon },
            PlayerActiveIndex = 0,
            PlayerActive = new BattlePokemon(playerPokemon, pikachu),
            OpponentParty = new[] { wildPokemon },
            OpponentActiveIndex = 0,
            OpponentActive = new BattlePokemon(wildPokemon, geodude)
        };

        _manager.Replace(new BattleScreen(_game, battleState));
    }
}
