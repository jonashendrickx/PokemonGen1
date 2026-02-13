using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PokemonGen1.Core.Save;
using PokemonGen1.Core.World;
using PokemonGen1.Game.Input;

namespace PokemonGen1.Game.Screens;

public class ShopScreen : IScreen
{
    private static readonly Color BgColor = new(248, 248, 248);
    private static readonly Color TextColor = new(40, 40, 40);
    private static readonly Color BorderColor = new(40, 40, 40);
    private static readonly Color HighlightColor = new(200, 228, 200);

    private readonly PokemonGame _game;
    private readonly SaveData _save;
    private readonly ShopData _shop;
    private ScreenManager _manager = null!;
    private int _cursor;
    private int _scroll;
    private string _message = "";
    private float _messageTimer;
    private bool _showMessage;

    public bool IsOverlay => true;
    public bool BlocksUpdate => true;

    public ShopScreen(PokemonGame game, SaveData save, ShopData shop)
    {
        _game = game;
        _save = save;
        _shop = shop;
    }

    public void Enter(ScreenManager manager) => _manager = manager;
    public void Exit() { }

    public void Update(GameTime gameTime, InputManager input)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (_showMessage)
        {
            _messageTimer += dt;
            if (_messageTimer > 0.5f && input.IsPressed(InputAction.Confirm))
                _showMessage = false;
            return;
        }

        int itemCount = _shop.Items.Length + 1; // +1 for "Exit"

        if (input.IsPressed(InputAction.Up))
        {
            _cursor = Math.Max(0, _cursor - 1);
            if (_cursor < _scroll) _scroll = _cursor;
        }
        if (input.IsPressed(InputAction.Down))
        {
            _cursor = Math.Min(itemCount - 1, _cursor + 1);
            if (_cursor >= _scroll + 8) _scroll = _cursor - 7;
        }

        if (input.IsPressed(InputAction.Cancel))
        {
            _manager.Pop();
            return;
        }

        if (input.IsPressed(InputAction.Confirm))
        {
            if (_cursor >= _shop.Items.Length)
            {
                // Exit
                _manager.Pop();
            }
            else
            {
                BuyItem(_cursor);
            }
        }
    }

    private void BuyItem(int index)
    {
        var shopItem = _shop.Items[index];
        var itemData = _game.GameData.GetItem(shopItem.ItemId);

        if (_save.Money < shopItem.Price)
        {
            _message = "Not enough money!";
            _showMessage = true;
            _messageTimer = 0;
            return;
        }

        _save.Money -= shopItem.Price;
        _save.Inventory.AddItem(shopItem.ItemId, 1);
        _message = $"Bought {itemData.Name}!";
        _showMessage = true;
        _messageTimer = 0;
    }

    public void Draw(SpriteBatch sb)
    {
        // Full screen overlay
        _game.DrawRect(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, PokemonGame.VirtualHeight), BgColor);

        // Title bar
        _game.DrawRect(sb, new Rectangle(0, 0, PokemonGame.VirtualWidth, 20), new Color(80, 120, 200));
        sb.DrawString(_game.Font, _shop.Name, new Vector2(6, 3), Color.White);

        // Money display
        string money = $"Money: ${_save.Money}";
        var moneySize = _game.Font.MeasureString(money);
        sb.DrawString(_game.Font, money, new Vector2(PokemonGame.VirtualWidth - moneySize.X - 6, 3), Color.Yellow);

        // Item list
        int y = 24;
        int maxVisible = 8;
        for (int i = _scroll; i < Math.Min(_shop.Items.Length, _scroll + maxVisible); i++)
        {
            var shopItem = _shop.Items[i];
            var itemData = _game.GameData.GetItem(shopItem.ItemId);
            bool selected = i == _cursor;

            if (selected)
                _game.DrawRect(sb, new Rectangle(4, y - 1, PokemonGame.VirtualWidth - 8, 14), HighlightColor);

            string prefix = selected ? "> " : "  ";
            sb.DrawString(_game.Font, $"{prefix}{itemData.Name}", new Vector2(6, y), TextColor);

            string price = $"${shopItem.Price}";
            var priceSize = _game.Font.MeasureString(price);
            sb.DrawString(_game.Font, price, new Vector2(PokemonGame.VirtualWidth - priceSize.X - 6, y), TextColor);

            y += 15;
        }

        // Exit option
        int exitIndex = _shop.Items.Length;
        if (exitIndex >= _scroll && exitIndex < _scroll + maxVisible)
        {
            bool selected = exitIndex == _cursor;
            if (selected)
                _game.DrawRect(sb, new Rectangle(4, y - 1, PokemonGame.VirtualWidth - 8, 14), HighlightColor);

            string prefix = selected ? "> " : "  ";
            sb.DrawString(_game.Font, $"{prefix}Exit", new Vector2(6, y), TextColor);
        }

        // Message overlay
        if (_showMessage)
        {
            var msgBox = new Rectangle(20, 60, PokemonGame.VirtualWidth - 40, 40);
            _game.DrawRect(sb, msgBox, BgColor);
            _game.DrawBorder(sb, msgBox, BorderColor, 2);
            sb.DrawString(_game.Font, _message, new Vector2(28, 68), TextColor);
        }

        // Hint
        sb.DrawString(_game.Font, "X:Back  Z:Buy", new Vector2(6, PokemonGame.VirtualHeight - 14),
            new Color(120, 120, 120));
    }
}
