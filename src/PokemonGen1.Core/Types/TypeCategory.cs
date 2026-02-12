namespace PokemonGen1.Core.Types;

/// <summary>
/// In Gen 1, whether a move is Physical or Special is determined by its type, not the move itself.
/// </summary>
public static class TypeCategory
{
    private static readonly HashSet<PokemonType> PhysicalTypes = new()
    {
        PokemonType.Normal,
        PokemonType.Fighting,
        PokemonType.Flying,
        PokemonType.Ground,
        PokemonType.Rock,
        PokemonType.Bug,
        PokemonType.Ghost,
        PokemonType.Poison
    };

    public static bool IsPhysical(PokemonType type) => PhysicalTypes.Contains(type);
    public static bool IsSpecial(PokemonType type) => !PhysicalTypes.Contains(type);
}
