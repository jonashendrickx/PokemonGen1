using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.Save;
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
            StartNewGame();
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

    private void StartNewGame()
    {
        var save = new SaveData();
        _manager.Replace(new StarterSelectScreen(_game, save));
    }
}
