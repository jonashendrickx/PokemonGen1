using PokemonGen1.Core.Pokemon;
using PokemonGen1.Core.Trainers;

namespace PokemonGen1.Core.Battle;

public enum BattleType { Wild, Trainer }

public class BattleState
{
    public BattleType Type { get; init; }

    // Player side
    public PokemonInstance[] PlayerParty { get; init; } = Array.Empty<PokemonInstance>();
    public int PlayerActiveIndex { get; set; }
    public BattlePokemon PlayerActive { get; set; } = null!;

    // Opponent side
    public PokemonInstance[] OpponentParty { get; init; } = Array.Empty<PokemonInstance>();
    public int OpponentActiveIndex { get; set; }
    public BattlePokemon OpponentActive { get; set; } = null!;
    public TrainerData? OpponentTrainer { get; init; }

    public int TurnNumber { get; set; }
    public bool IsOver { get; set; }
    public BattleOutcome? Outcome { get; set; }
    public int EscapeAttempts { get; set; }

    public bool PlayerHasAlivePokemon()
    {
        for (int i = 0; i < PlayerParty.Length; i++)
            if (!PlayerParty[i].IsFainted) return true;
        return false;
    }

    public bool OpponentHasAlivePokemon()
    {
        for (int i = 0; i < OpponentParty.Length; i++)
            if (!OpponentParty[i].IsFainted) return true;
        return false;
    }

    public int? GetNextAliveOpponent()
    {
        for (int i = 0; i < OpponentParty.Length; i++)
            if (!OpponentParty[i].IsFainted && i != OpponentActiveIndex) return i;
        return null;
    }
}
