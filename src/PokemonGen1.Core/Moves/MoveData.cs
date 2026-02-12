using PokemonGen1.Core.Types;

namespace PokemonGen1.Core.Moves;

public enum MoveEffect
{
    None,
    Burn,
    Freeze,
    Paralysis,
    Poison,
    Sleep,
    Confusion,
    Flinch,
    StatUp,
    StatDown,
    AttackUp1,
    AttackUp2,
    DefenseUp1,
    DefenseUp2,
    SpecialUp1,
    SpecialUp2,
    SpeedUp2,
    EvasionUp1,
    AccuracyUp1, // unused in Gen 1 but included for completeness
    AttackDown1,
    AttackDown2,
    DefenseDown1,
    DefenseDown2,
    SpecialDown1,
    SpecialDown2,
    SpeedDown1,
    SpeedDown2,
    AccuracyDown1,
    EvasionDown1, // unused in Gen 1 but included
    DefenseDown1Self, // for moves like Close Combat-style (not gen1, but Skull Bash uses DefenseUp)
    Recoil,
    RecoilThird,
    Drain,
    FixedDamage20, // Sonic Boom
    FixedDamage40, // Dragon Rage
    LevelDamage, // Seismic Toss, Night Shade
    Psywave,
    SuperFang, // Half remaining HP
    OHKO,
    MultiHit, // 2-5 hits
    DoubleHit, // exactly 2 hits
    HighCrit,
    Charge, // Fly, Dig, SolarBeam, Skull Bash, Sky Attack, Razor Wind
    Recharge, // Hyper Beam
    Trapping, // Wrap, Bind, Fire Spin, Clamp
    Explosion, // Self-destruct and Explosion
    Recover, // Recover, Softboiled
    Rest,
    LeechSeed,
    Reflect,
    LightScreen,
    Haze,
    Mist,
    FocusEnergy,
    Substitute,
    Transform,
    Mimic,
    Metronome,
    MirrorMove,
    Counter,
    Bide,
    Disable,
    Encore, // not in Gen 1
    PayDay,
    Conversion,
    TriAttack,
    Swift, // never misses
    DreamEater,
    Thrash, // 2-3 turns, then confusion
    PetalDance, // same as Thrash
    Rage,
    Teleport,
    Splash,
    Growth, // Special +1 in Gen 1
    Minimize,
    SelfAttackDown1, // Overheat-style not gen1, but for Superpower etc
    DefenseUpCharge, // Skull Bash
    SpeedDownBurn, // not used, but placeholder
}

public enum MoveTarget
{
    SingleOpponent,
    Self,
    AllOpponents,
    UserSide
}

public class MoveData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public PokemonType Type { get; set; }
    public int Power { get; set; }
    public int Accuracy { get; set; }
    public int MaxPP { get; set; }
    public MoveEffect Effect { get; set; }
    public int EffectChance { get; set; }
    public int Priority { get; set; }
    public bool HighCritRate { get; set; }
    public MoveTarget Target { get; set; }

    /// <summary>
    /// In Gen 1, physical/special is determined by the move's type, not a per-move attribute.
    /// </summary>
    public bool IsPhysical => TypeCategory.IsPhysical(Type);
    public bool IsSpecial => TypeCategory.IsSpecial(Type);
    public bool IsStatus => Power == 0 && Effect != MoveEffect.FixedDamage20
                                       && Effect != MoveEffect.FixedDamage40
                                       && Effect != MoveEffect.LevelDamage
                                       && Effect != MoveEffect.OHKO
                                       && Effect != MoveEffect.Psywave
                                       && Effect != MoveEffect.SuperFang
                                       && Effect != MoveEffect.Counter
                                       && Effect != MoveEffect.Bide;
}
