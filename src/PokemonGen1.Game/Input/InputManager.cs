using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PokemonGen1.Game.Input;

public enum InputAction
{
    Up, Down, Left, Right,
    Confirm,   // A / Z / Enter
    Cancel,    // B / X / Escape
    Start,     // Start / Enter
    Select     // Select / BackSpace
}

public class InputManager
{
    private KeyboardState _currentKb, _previousKb;
    private GamePadState _currentGp, _previousGp;

    public void Update()
    {
        _previousKb = _currentKb;
        _previousGp = _currentGp;
        _currentKb = Keyboard.GetState();
        _currentGp = GamePad.GetState(PlayerIndex.One);
    }

    public bool IsPressed(InputAction action)
    {
        return IsKeyPressed(action) || IsButtonPressed(action);
    }

    public bool IsHeld(InputAction action)
    {
        return IsKeyHeld(action) || IsButtonHeld(action);
    }

    private bool IsKeyPressed(InputAction action)
    {
        foreach (var key in GetKeys(action))
            if (_currentKb.IsKeyDown(key) && !_previousKb.IsKeyDown(key))
                return true;
        return false;
    }

    private bool IsKeyHeld(InputAction action)
    {
        foreach (var key in GetKeys(action))
            if (_currentKb.IsKeyDown(key))
                return true;
        return false;
    }

    private bool IsButtonPressed(InputAction action)
    {
        var btn = GetButton(action);
        return btn.HasValue && _currentGp.IsButtonDown(btn.Value) && !_previousGp.IsButtonDown(btn.Value);
    }

    private bool IsButtonHeld(InputAction action)
    {
        var btn = GetButton(action);
        return btn.HasValue && _currentGp.IsButtonDown(btn.Value);
    }

    private static Keys[] GetKeys(InputAction action) => action switch
    {
        InputAction.Up => new[] { Keys.Up, Keys.W },
        InputAction.Down => new[] { Keys.Down, Keys.S },
        InputAction.Left => new[] { Keys.Left, Keys.A },
        InputAction.Right => new[] { Keys.Right, Keys.D },
        InputAction.Confirm => new[] { Keys.Z, Keys.Enter, Keys.Space },
        InputAction.Cancel => new[] { Keys.X, Keys.Back },
        InputAction.Start => new[] { Keys.Enter },
        InputAction.Select => new[] { Keys.RightShift },
        _ => Array.Empty<Keys>()
    };

    private static Buttons? GetButton(InputAction action) => action switch
    {
        InputAction.Up => Buttons.DPadUp,
        InputAction.Down => Buttons.DPadDown,
        InputAction.Left => Buttons.DPadLeft,
        InputAction.Right => Buttons.DPadRight,
        InputAction.Confirm => Buttons.A,
        InputAction.Cancel => Buttons.B,
        InputAction.Start => Buttons.Start,
        InputAction.Select => Buttons.Back,
        _ => null
    };
}
