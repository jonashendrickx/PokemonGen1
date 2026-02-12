using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Game.Input;

namespace PokemonGen1.Game.Screens;

public interface IScreen
{
    bool IsOverlay { get; }
    bool BlocksUpdate { get; }
    void Enter(ScreenManager manager);
    void Exit();
    void Update(GameTime gameTime, InputManager input);
    void Draw(SpriteBatch spriteBatch);
}
