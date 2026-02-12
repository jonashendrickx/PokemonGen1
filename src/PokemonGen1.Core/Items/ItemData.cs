namespace PokemonGen1.Core.Items;

public enum ItemCategory { Pokeball, Medicine, TmHm, KeyItem, BattleItem, Misc }

public enum ItemEffect
{
    None, HealHp, HealStatus, HealAll, Revive, FullRevive, CatchPokemon,
    Ether, MaxEther, Elixir, MaxElixir,
    XAttack, XDefend, XSpecial, XSpeed, XAccuracy, DireHit, GuardSpec,
    EvoStone, RareCandy, PPUp, PPMax, TeachMove,
    Repel, SuperRepel, MaxRepel,
    EscapeRope, PokeDoll, PokeFlute
}

public class ItemData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ItemCategory Category { get; set; }
    public int Price { get; set; }
    public string Description { get; set; } = "";
    public ItemEffect Effect { get; set; }
    public int EffectValue { get; set; }
    public bool Usable { get; set; }
    public bool UsableInBattle { get; set; }
    public bool IsKeyItem { get; set; }
    public bool IsTM { get; set; }
    public int? TMMoveId { get; set; }
}

public class Inventory
{
    public Dictionary<int, int> Items { get; set; } = new(); // ItemId -> count

    public void AddItem(int itemId, int count = 1)
    {
        Items.TryGetValue(itemId, out int current);
        Items[itemId] = current + count;
    }

    public bool RemoveItem(int itemId, int count = 1)
    {
        if (!Items.TryGetValue(itemId, out int current) || current < count)
            return false;
        Items[itemId] = current - count;
        if (Items[itemId] <= 0) Items.Remove(itemId);
        return true;
    }

    public int GetCount(int itemId) => Items.TryGetValue(itemId, out int c) ? c : 0;
}
