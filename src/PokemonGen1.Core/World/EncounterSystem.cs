using PokemonGen1.Core.Data;
using PokemonGen1.Core.Pokemon;

namespace PokemonGen1.Core.World;

public class EncounterSystem
{
    private readonly GameData _data;
    private readonly Random _rng;

    public EncounterSystem(GameData data, Random? rng = null)
    {
        _data = data;
        _rng = rng ?? new Random();
    }

    /// <summary>
    /// Roll for a wild encounter. Returns true if an encounter occurs.
    /// </summary>
    public bool RollEncounter(WildEncounterTable table)
    {
        return _rng.Next(100) < table.EncounterRate;
    }

    /// <summary>
    /// Pick a random encounter slot based on weights and generate a wild Pokemon.
    /// </summary>
    public PokemonInstance GenerateWildPokemon(EncounterSlot[] slots)
    {
        var slot = PickSlot(slots);
        return GenerateFromSlot(slot);
    }

    /// <summary>
    /// Try to trigger a wild encounter. Returns null if no encounter happens.
    /// </summary>
    public PokemonInstance? TryEncounter(WildEncounterTable table)
    {
        if (!RollEncounter(table))
            return null;

        var slots = table.GrassEncounters;
        if (slots == null || slots.Length == 0)
            return null;

        return GenerateWildPokemon(slots);
    }

    /// <summary>
    /// Generate a wild Pokemon for a surf encounter.
    /// </summary>
    public PokemonInstance? TrySurfEncounter(WildEncounterTable table)
    {
        if (!RollEncounter(table))
            return null;

        var slots = table.SurfEncounters;
        if (slots == null || slots.Length == 0)
            return null;

        return GenerateWildPokemon(slots);
    }

    /// <summary>
    /// Generate a wild Pokemon for a fishing encounter (always triggers if slots exist).
    /// </summary>
    public PokemonInstance? TryFishingEncounter(WildEncounterTable table)
    {
        var slots = table.FishingEncounters;
        if (slots == null || slots.Length == 0)
            return null;

        return GenerateWildPokemon(slots);
    }

    private EncounterSlot PickSlot(EncounterSlot[] slots)
    {
        int totalWeight = 0;
        foreach (var slot in slots)
            totalWeight += slot.Weight;

        int roll = _rng.Next(totalWeight);
        int cumulative = 0;
        foreach (var slot in slots)
        {
            cumulative += slot.Weight;
            if (roll < cumulative)
                return slot;
        }

        return slots[^1];
    }

    private PokemonInstance GenerateFromSlot(EncounterSlot slot)
    {
        int level = slot.MinLevel == slot.MaxLevel
            ? slot.MinLevel
            : _rng.Next(slot.MinLevel, slot.MaxLevel + 1);

        var species = _data.GetSpecies(slot.SpeciesId);
        var pokemon = PokemonInstance.Create(species, level, _rng);
        pokemon.Moves = _data.GetDefaultMoves(slot.SpeciesId, level);

        // If no learnset moves, give Tackle as fallback
        if (pokemon.Moves.Length == 0)
        {
            pokemon.Moves = new[]
            {
                new Core.Moves.MoveInstance { MoveId = 33, CurrentPP = 35, MaxPP = 35 }
            };
        }

        pokemon.CurrentHp = pokemon.MaxHp(species);
        return pokemon;
    }
}
