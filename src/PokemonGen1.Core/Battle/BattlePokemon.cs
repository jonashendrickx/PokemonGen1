using PokemonGen1.Core.Moves;
using PokemonGen1.Core.Pokemon;
using PokemonGen1.Core.Types;

namespace PokemonGen1.Core.Battle;

public class BattlePokemon
{
    public PokemonInstance Pokemon { get; }
    public PokemonSpecies Species { get; }

    // Stat stages (-6 to +6)
    public int AttackStage { get; set; }
    public int DefenseStage { get; set; }
    public int SpecialStage { get; set; }
    public int SpeedStage { get; set; }
    public int AccuracyStage { get; set; }
    public int EvasionStage { get; set; }

    // Volatile status (cleared on switch)
    public bool IsConfused { get; set; }
    public int ConfusionTurns { get; set; }
    public bool IsFlinched { get; set; }
    public bool IsCharging { get; set; }
    public int ChargingMoveId { get; set; }
    public bool MustRecharge { get; set; }
    public bool HasSubstitute { get; set; }
    public int SubstituteHp { get; set; }
    public bool HasReflect { get; set; }
    public bool HasLightScreen { get; set; }
    public bool IsSeeded { get; set; }
    public int ToxicCounter { get; set; }
    public int DisabledMoveId { get; set; }
    public int DisabledTurns { get; set; }
    public bool IsTrapped { get; set; }
    public int TrapTurns { get; set; }
    public int TrapDamage { get; set; }
    public int LastMoveUsed { get; set; }
    public bool IsRaging { get; set; }
    public bool IsBiding { get; set; }
    public int BideTurns { get; set; }
    public int BideDamage { get; set; }
    public int SleepTurns { get; set; }
    public bool IsThrashing { get; set; }
    public int ThrashTurns { get; set; }
    public int ThrashMoveId { get; set; }

    // Type can change (Transform)
    public PokemonType Type1 { get; set; }
    public PokemonType? Type2 { get; set; }

    public int Level => Pokemon.Level;

    public BattlePokemon(PokemonInstance pokemon, PokemonSpecies species)
    {
        Pokemon = pokemon;
        Species = species;
        Type1 = species.Type1;
        Type2 = species.Type2;

        if (pokemon.Status == StatusCondition.Sleep)
            SleepTurns = new Random().Next(1, 8);
        if (pokemon.Status == StatusCondition.BadlyPoisoned)
            ToxicCounter = 1;
    }

    // Base calculated stats (without battle modifiers)
    public int BaseAttack => Pokemon.Attack(Species);
    public int BaseDefense => Pokemon.Defense(Species);
    public int BaseSpecial => Pokemon.Special(Species);
    public int BaseSpeed => Pokemon.Speed(Species);
    public int MaxHp => Pokemon.MaxHp(Species);

    // Unmodified stats for critical hit calculations
    public int UnmodifiedAttack
    {
        get
        {
            int stat = BaseAttack;
            if (Pokemon.Status == StatusCondition.Burn) stat /= 2;
            return Math.Max(1, stat);
        }
    }

    public int UnmodifiedDefense => Math.Max(1, BaseDefense);

    public int UnmodifiedSpecial => Math.Max(1, BaseSpecial);

    public int UnmodifiedSpeed
    {
        get
        {
            int stat = BaseSpeed;
            if (Pokemon.Status == StatusCondition.Paralysis) stat /= 4;
            return Math.Max(1, stat);
        }
    }

    // Effective stats with stage modifiers
    public int EffectiveAttack => Math.Max(1, ApplyStage(UnmodifiedAttack, AttackStage));
    public int EffectiveDefense => Math.Max(1, ApplyStage(UnmodifiedDefense, DefenseStage));
    public int EffectiveSpecial => Math.Max(1, ApplyStage(UnmodifiedSpecial, SpecialStage));
    public int EffectiveSpeed => Math.Max(1, ApplyStage(UnmodifiedSpeed, SpeedStage));

    /// <summary>
    /// Gen 1 stat stage multipliers.
    /// Stages: -6=-75%, -5=-71%, -4=-67%, -3=-60%, -2=-50%, -1=-33%, 0=100%, +1=+50%, +2=+100%, +3=+150%, +4=+200%, +5=+250%, +6=+300%
    /// </summary>
    private static int ApplyStage(int stat, int stage)
    {
        int[] numerators = { 25, 28, 33, 40, 50, 66, 100, 150, 200, 250, 300, 350, 400 };
        int idx = Math.Clamp(stage + 6, 0, 12);
        return stat * numerators[idx] / 100;
    }

    /// <summary>
    /// Modify a stat stage, clamping to -6..+6. Returns actual change applied.
    /// </summary>
    public int ModifyStage(string stat, int stages)
    {
        int oldStage = GetStage(stat);
        int newStage = Math.Clamp(oldStage + stages, -6, 6);
        SetStage(stat, newStage);
        return newStage - oldStage;
    }

    private int GetStage(string stat) => stat.ToLower() switch
    {
        "attack" => AttackStage,
        "defense" => DefenseStage,
        "special" => SpecialStage,
        "speed" => SpeedStage,
        "accuracy" => AccuracyStage,
        "evasion" => EvasionStage,
        _ => 0
    };

    private void SetStage(string stat, int value)
    {
        switch (stat.ToLower())
        {
            case "attack": AttackStage = value; break;
            case "defense": DefenseStage = value; break;
            case "special": SpecialStage = value; break;
            case "speed": SpeedStage = value; break;
            case "accuracy": AccuracyStage = value; break;
            case "evasion": EvasionStage = value; break;
        }
    }

    public void ResetVolatile()
    {
        AttackStage = DefenseStage = SpecialStage = SpeedStage = 0;
        AccuracyStage = EvasionStage = 0;
        IsConfused = false; ConfusionTurns = 0;
        IsFlinched = false;
        IsCharging = false; ChargingMoveId = 0;
        MustRecharge = false;
        HasSubstitute = false; SubstituteHp = 0;
        HasReflect = false; HasLightScreen = false;
        IsSeeded = false;
        ToxicCounter = 0;
        DisabledMoveId = 0; DisabledTurns = 0;
        IsTrapped = false; TrapTurns = 0;
        LastMoveUsed = 0;
        IsRaging = false;
        IsBiding = false; BideTurns = 0; BideDamage = 0;
        IsThrashing = false; ThrashTurns = 0;
    }
}
