using PokemonGen1.Core.Battle;
using PokemonGen1.Core.Data;
using PokemonGen1.Core.Moves;
using PokemonGen1.Core.Types;

namespace PokemonGen1.Core.Trainers;

public static class TrainerAI
{
    public static BattleAction ChooseAction(BattleState state, GameData data, AIBehavior behavior, Random rng)
    {
        var pokemon = state.OpponentActive;
        var moves = pokemon.Pokemon.Moves;

        if (moves.Length == 0)
            return new FightAction(0); // Struggle

        return behavior switch
        {
            AIBehavior.Random => ChooseRandom(moves, rng),
            AIBehavior.Smart or AIBehavior.GymLeader or AIBehavior.EliteFour or AIBehavior.Champion
                => ChooseSmart(state, data, rng),
            _ => ChooseRandom(moves, rng)
        };
    }

    private static BattleAction ChooseRandom(MoveInstance[] moves, Random rng)
    {
        // Pick a random move that has PP
        var available = new List<int>();
        for (int i = 0; i < moves.Length; i++)
            if (moves[i].CurrentPP > 0) available.Add(i);

        if (available.Count == 0) return new FightAction(-1); // Struggle
        return new FightAction(available[rng.Next(available.Count)]);
    }

    private static BattleAction ChooseSmart(BattleState state, GameData data, Random rng)
    {
        var attacker = state.OpponentActive;
        var defender = state.PlayerActive;
        var moves = attacker.Pokemon.Moves;

        int bestIndex = 0;
        float bestScore = -1;

        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].CurrentPP <= 0) continue;

            var move = data.GetMove(moves[i].MoveId);
            float score = ScoreMove(attacker, defender, move, data.TypeChart);

            // Add small random factor
            score += rng.NextSingle() * 10f;

            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return new FightAction(bestIndex);
    }

    private static float ScoreMove(BattlePokemon attacker, BattlePokemon defender,
        MoveData move, TypeChart typeChart)
    {
        if (move.Power > 0)
        {
            float effectiveness = typeChart.GetTotalEffectiveness(move.Type, defender.Type1, defender.Type2);
            if (effectiveness == 0) return 0;

            float stab = (attacker.Type1 == move.Type || attacker.Type2 == move.Type) ? 1.5f : 1f;
            float score = move.Power * effectiveness * stab;

            // Penalize low accuracy
            if (move.Accuracy > 0)
                score *= move.Accuracy / 100f;

            return score;
        }

        // Status moves get a base score
        return move.Effect switch
        {
            MoveEffect.Sleep => 80,
            MoveEffect.Paralysis => 60,
            MoveEffect.AttackUp2 or MoveEffect.SpecialUp2 => 50,
            MoveEffect.DefenseDown2 => 45,
            _ => 20
        };
    }
}
