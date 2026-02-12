using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.Data;
using PokemonGen1.Game.Input;
using PokemonGen1.Game.Rendering;
using PokemonGen1.Game.Screens;

namespace PokemonGen1.Game;

public class PokemonGame : Microsoft.Xna.Framework.Game
{
    public const int VirtualWidth = 240;
    public const int VirtualHeight = 160;
    public const int Scale = 3;

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private RenderTarget2D _renderTarget = null!;
    private InputManager _input = null!;
    private ScreenManager _screens = null!;

    public GameData GameData { get; private set; } = null!;
    public SpriteFont Font { get; private set; } = null!;
    public Texture2D Pixel { get; private set; } = null!;
    public SpriteManager Sprites { get; private set; } = null!;

    public PokemonGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = VirtualWidth * Scale;
        _graphics.PreferredBackBufferHeight = VirtualHeight * Scale;
    }

    protected override void Initialize()
    {
        _input = new InputManager();
        _screens = new ScreenManager();
        Window.Title = "Pokemon Gen 1";
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);

        // Create 1x1 white pixel texture for drawing primitives
        Pixel = new Texture2D(GraphicsDevice, 1, 1);
        Pixel.SetData(new[] { Color.White });

        // Load game data
        string dataDir = FindDataDirectory();
        GameData = GameData.LoadFromDirectory(dataDir);

        // Initialize sprite manager
        var spritesDir = Path.Combine(dataDir, "sprites");
        Sprites = new SpriteManager(GraphicsDevice, spritesDir);

        // Load font
        Font = Content.Load<SpriteFont>("PokemonFont");

        // Start with title screen
        _screens.Push(new TitleScreen(this));
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();
        _screens.Update(gameTime, _input);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        // Draw to virtual resolution render target
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null, null, null, null);
        _screens.Draw(_spriteBatch);
        _spriteBatch.End();

        // Scale to window
        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
            SamplerState.PointClamp, null, null, null, null);
        _spriteBatch.Draw(_renderTarget, new Rectangle(0, 0,
            VirtualWidth * Scale, VirtualHeight * Scale), Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private string FindDataDirectory()
    {
        // Search for data directory relative to executable
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "data"),
            Path.Combine(baseDir, "..", "..", "..", "..", "..", "data"),
            Path.Combine(baseDir, "..", "..", "..", "data"),
            Path.Combine(Directory.GetCurrentDirectory(), "data"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "data")),
        };

        foreach (var dir in candidates)
        {
            var fullPath = Path.GetFullPath(dir);
            if (Directory.Exists(fullPath) && File.Exists(Path.Combine(fullPath, "pokemon", "species.json")))
                return fullPath;
        }

        throw new DirectoryNotFoundException(
            $"Could not find 'data' directory with species.json. Searched: {string.Join(", ", candidates.Select(Path.GetFullPath))}");
    }

    public ScreenManager Screens => _screens;

    public void DrawRect(SpriteBatch sb, Rectangle rect, Color color)
    {
        sb.Draw(Pixel, rect, color);
    }

    public void DrawBorder(SpriteBatch sb, Rectangle rect, Color color, int thickness = 1)
    {
        sb.Draw(Pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        sb.Draw(Pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        sb.Draw(Pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        sb.Draw(Pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}
