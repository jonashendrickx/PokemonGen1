using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.Pokemon;
using PokemonGen1.Core.Save;
using PokemonGen1.Game.Input;

namespace PokemonGen1.Game.Screens;

public class StarterSelectScreen : IScreen
{
    private readonly PokemonGame _game;
    private readonly SaveData _save;
    private ScreenManager _manager = null!;
    private int _cursor;
    private bool _confirmed;
    private float _confirmTimer;

    // Bulbasaur=1, Charmander=4, Squirtle=7
    private static readonly int[] StarterIds = { 1, 4, 7 };
    private static readonly string[] StarterNames = { "Bulbasaur", "Charmander", "Squirtle" };
    private static readonly string[] StarterTypes = { "Grass/Poison", "Fire", "Water" };
    private static readonly string[] StarterDesc =
    {
        "A strange seed was planted on its back at birth.",
        "Obviously prefers hot places. Its tail flame burns vigorously.",
        "Shoots water at prey while underwater. Withdraws into its shell."
    };
    private static readonly Color[] StarterColors =
    {
        new(120, 200, 80),   // Green
        new(240, 128, 48),   // Orange
        new(104, 144, 240)   // Blue
    };

    public bool IsOverlay => false;
    public bool BlocksUpdate => true;

    public StarterSelectScreen(PokemonGame game, SaveData save)
    {
        _game = game;
        _save = save;
    }

    public void Enter(ScreenManager manager) => _manager = manager;
    public void Exit() { }

    public void Update(GameTime gameTime, InputManager input)
    {
        if (_confirmed)
        {
            _confirmTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_confirmTimer > 1.5f)
            {
                // Create the starter Pokemon
                var species = _game.GameData.GetSpecies(StarterIds[_cursor]);
                var starter = PokemonInstance.Create(species, 5);
                starter.OtName = _save.PlayerName;
                starter.Moves = _game.GameData.GetDefaultMoves(StarterIds[_cursor], 5);
                if (starter.Moves.Length == 0)
                {
                    // Fallback: give Tackle and a type move
                    var moves = new List<Core.Moves.MoveInstance>
                    {
                        new() { MoveId = 33, CurrentPP = 35, MaxPP = 35 } // Tackle
                    };
                    int typeMove = _cursor switch
                    {
                        0 => 22,  // Vine Whip
                        1 => 52,  // Ember
                        2 => 55,  // Water Gun
                        _ => 33
                    };
                    moves.Add(new Core.Moves.MoveInstance
                    {
                        MoveId = typeMove,
                        CurrentPP = _game.GameData.GetMove(typeMove).MaxPP,
                        MaxPP = _game.GameData.GetMove(typeMove).MaxPP
                    });
                    starter.Moves = moves.ToArray();
                }
                starter.CurrentHp = starter.MaxHp(species);

                _save.Party = new[] { starter };
                _save.StoryFlags.Add("has_starter");
                _save.PokedexSeen.Add(StarterIds[_cursor]);
                _save.PokedexCaught.Add(StarterIds[_cursor]);

                _manager.Replace(new OverworldScreen(_game, _save));
            }
            return;
        }

        if (input.IsPressed(InputAction.Left)) _cursor = Math.Max(0, _cursor - 1);
        if (input.IsPressed(InputAction.Right)) _cursor = Math.Min(2, _cursor + 1);

        if (input.IsPressed(InputAction.Confirm))
        {
            _confirmed = true;
            _confirmTimer = 0;
        }
    }

    public void Draw(SpriteBatch sb)
    {
        // Background
        _game.DrawRect(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, PokemonGame.VirtualHeight),
            new Color(248, 248, 248));

        // Title
        string title = "Prof. Oak: Choose your Pokemon!";
        var titleSize = _game.Font.MeasureString(title);
        sb.DrawString(_game.Font, title,
            new Vector2((PokemonGame.VirtualWidth - titleSize.X) / 2, 8), new Color(40, 40, 40));

        // Draw three Pokeball options
        for (int i = 0; i < 3; i++)
        {
            int x = 16 + i * 76;
            int y = 30;
            bool selected = i == _cursor;

            // Pokeball background
            Color bgColor = selected ? StarterColors[i] : new Color(200, 200, 200);
            _game.DrawRect(sb, new Rectangle(x, y, 68, 80), bgColor);
            _game.DrawBorder(sb, new Rectangle(x, y, 68, 80), new Color(40, 40, 40), selected ? 2 : 1);

            // Pokemon sprite
            var sprite = _game.Sprites.GetFrontSprite(StarterIds[i]);
            if (sprite != null)
                sb.Draw(sprite, new Rectangle(x + 10, y + 4, 48, 48), Color.White);
            else
                _game.DrawRect(sb, new Rectangle(x + 10, y + 4, 48, 48), StarterColors[i]);

            // Name
            var nameSize = _game.Font.MeasureString(StarterNames[i]);
            sb.DrawString(_game.Font, StarterNames[i],
                new Vector2(x + (68 - nameSize.X) / 2, y + 55), selected ? Color.White : new Color(80, 80, 80));

            // Type
            var typeSize = _game.Font.MeasureString(StarterTypes[i]);
            sb.DrawString(_game.Font, StarterTypes[i],
                new Vector2(x + (68 - typeSize.X) / 2, y + 67), selected ? Color.White : new Color(120, 120, 120));
        }

        // Description box
        var descBox = new Rectangle(4, 116, 232, 40);
        _game.DrawRect(sb, descBox, new Color(248, 248, 248));
        _game.DrawBorder(sb, descBox, new Color(40, 40, 40), 2);

        if (_confirmed)
        {
            string confirmText = $"You chose {StarterNames[_cursor]}!";
            sb.DrawString(_game.Font, confirmText, new Vector2(12, 124), new Color(40, 40, 40));
        }
        else
        {
            sb.DrawString(_game.Font, StarterDesc[_cursor], new Vector2(12, 120), new Color(40, 40, 40));

            // Selection arrow
            int arrowX = 16 + _cursor * 76 + 30;
            sb.DrawString(_game.Font, "^", new Vector2(arrowX, 112), new Color(40, 40, 40));
        }
    }
}
