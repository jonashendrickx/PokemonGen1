namespace PokemonGen1.Core.World;

public class ShopData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public ShopItem[] Items { get; set; } = Array.Empty<ShopItem>();
    public int RequiredBadges { get; set; }
}

public class ShopItem
{
    public int ItemId { get; set; }
    public int Price { get; set; }
}
