using PokemonGen1.Core.Moves;
using PokemonGen1.Core.Types;

namespace PokemonGen1.Core.Battle;

public record DamageResult(int Damage, bool IsCritical, float Effectiveness);

public class DamageCalculator
{
    private readonly Random _rng;

    public DamageCalculator(Random rng) => _rng = rng;

    /// <summary>
    /// Gen 1 damage formula:
    /// Damage = ((((2 * Level * Critical / 5 + 2) * Power * A / D) / 50 + 2) * STAB * Type1 * Type2 * Random) / 255
    /// All divisions are integer (floor). Random is 217-255 inclusive.
    /// </summary>
    public DamageResult Calculate(
        BattlePokemon attacker,
        BattlePokemon defender,
        MoveData move,
        TypeChart typeChart)
    {
        bool isPhysical = TypeCategory.IsPhysical(move.Type);

        // Critical hit check
        bool isCritical = CriticalHitCalculator.RollCritical(
            attacker.Species.BaseSpeed,
            move.HighCritRate,
            false, // FocusEnergy tracked elsewhere
            _rng);

        int critical = isCritical ? 2 : 1;
        int level = attacker.Level;

        int attackStat, defenseStat;
        if (isCritical)
        {
            // Critical hits ignore stat stages, use unmodified stats
            attackStat = isPhysical ? attacker.UnmodifiedAttack : attacker.UnmodifiedSpecial;
            defenseStat = isPhysical ? defender.UnmodifiedDefense : defender.UnmodifiedSpecial;
        }
        else
        {
            attackStat = isPhysical ? attacker.EffectiveAttack : attacker.EffectiveSpecial;
            defenseStat = isPhysical ? defender.EffectiveDefense : defender.EffectiveSpecial;
        }

        // Gen 1: if either stat > 255, divide both by 4
        if (attackStat > 255 || defenseStat > 255)
        {
            attackStat = Math.Max(1, attackStat / 4);
            defenseStat = Math.Max(1, defenseStat / 4);
        }

        // Reflect halves physical damage, Light Screen halves special damage
        if (!isCritical)
        {
            if (isPhysical && defender.HasReflect)
                defenseStat *= 2;
            if (!isPhysical && defender.HasLightScreen)
                defenseStat *= 2;
        }

        // Explosion / Self-Destruct halve defense
        if (move.Effect == MoveEffect.Explosion)
            defenseStat = Math.Max(1, defenseStat / 2);

        defenseStat = Math.Max(1, defenseStat);

        // Core damage formula (integer arithmetic)
        int damage = (2 * level * critical) / 5 + 2;
        damage = damage * move.Power * attackStat / defenseStat;
        damage = damage / 50 + 2;

        // STAB (Same Type Attack Bonus)
        if (attacker.Type1 == move.Type || attacker.Type2 == move.Type)
        {
            damage = damage * 3 / 2; // 1.5x as integer math
        }

        // Type effectiveness
        float type1Eff = typeChart.GetEffectiveness(move.Type, defender.Type1);
        float type2Eff = defender.Type2.HasValue
            ? typeChart.GetEffectiveness(move.Type, defender.Type2.Value)
            : 1.0f;

        float totalEffectiveness = type1Eff * type2Eff;

        // Apply type effectiveness as integer multiplications
        damage = (int)(damage * type1Eff);
        if (defender.Type2.HasValue)
            damage = (int)(damage * type2Eff);

        // Random factor: integer 217-255 inclusive
        if (damage > 1)
        {
            int randomFactor = _rng.Next(217, 256);
            damage = damage * randomFactor / 255;
        }

        // Minimum 1 damage if not immune
        if (totalEffectiveness > 0 && damage == 0) damage = 1;
        if (totalEffectiveness == 0f) damage = 0;

        return new DamageResult(damage, isCritical, totalEffectiveness);
    }
}
