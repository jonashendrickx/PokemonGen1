using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Game.Input;

namespace PokemonGen1.Game.Screens;

public class ScreenManager
{
    private readonly Stack<IScreen> _screens = new();

    public IScreen? CurrentScreen => _screens.Count > 0 ? _screens.Peek() : null;

    public void Push(IScreen screen)
    {
        _screens.Push(screen);
        screen.Enter(this);
    }

    public void Pop()
    {
        if (_screens.Count > 0)
        {
            var screen = _screens.Pop();
            screen.Exit();
        }
    }

    public void Replace(IScreen screen)
    {
        Pop();
        Push(screen);
    }

    public void Update(GameTime gameTime, InputManager input)
    {
        var updateList = new List<IScreen>();
        foreach (var screen in _screens)
        {
            updateList.Add(screen);
            if (screen.BlocksUpdate) break;
        }
        updateList.Reverse();
        foreach (var screen in updateList)
            screen.Update(gameTime, input);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        var drawList = new List<IScreen>();
        foreach (var screen in _screens)
        {
            drawList.Add(screen);
            if (!screen.IsOverlay) break;
        }
        drawList.Reverse();
        foreach (var screen in drawList)
            screen.Draw(spriteBatch);
    }
}
