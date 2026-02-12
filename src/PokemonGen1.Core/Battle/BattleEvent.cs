namespace PokemonGen1.Core.Battle;

public enum BattleSide { Player, Opponent }
public enum BattleOutcome { PlayerWin, PlayerLose, PlayerFled }

public abstract record BattleEvent;
public record MoveUsedEvent(BattleSide Attacker, string MoveName) : BattleEvent;
public record DamageDealtEvent(BattleSide Target, int Damage, bool IsCritical, float Effectiveness) : BattleEvent;
public record MoveFailedEvent(BattleSide Attacker, string Reason) : BattleEvent;
public record MoveMissedEvent(BattleSide Attacker, string MoveName) : BattleEvent;
public record StatusAppliedEvent(BattleSide Target, StatusCondition Status) : BattleEvent;
public record StatusDamageEvent(BattleSide Target, StatusCondition Status, int Damage) : BattleEvent;
public record StatChangedEvent(BattleSide Target, string StatName, int Stages) : BattleEvent;
public record FaintedEvent(BattleSide Side, string PokemonName) : BattleEvent;
public record HpChangedEvent(BattleSide Side, int OldHp, int NewHp, int MaxHp) : BattleEvent;
public record SwitchEvent(BattleSide Side, string PokemonName) : BattleEvent;
public record TextEvent(string Message) : BattleEvent;
public record BattleEndedEvent(BattleOutcome Outcome) : BattleEvent;
public record ExperienceGainedEvent(string PokemonName, int Exp) : BattleEvent;
public record RecoilEvent(BattleSide Side, int Damage) : BattleEvent;
public record DrainEvent(BattleSide Attacker, int HpRestored) : BattleEvent;
public record OhkoEvent(BattleSide Target) : BattleEvent;
public record MultiHitEvent(int HitCount) : BattleEvent;
public record ChargingEvent(BattleSide Side, string MoveName) : BattleEvent;
public record RechargeEvent(BattleSide Side) : BattleEvent;
public record ConfusionHitSelfEvent(BattleSide Side, int Damage) : BattleEvent;
public record StatusPreventedMoveEvent(BattleSide Side, StatusCondition Status) : BattleEvent;
public record SubstituteCreatedEvent(BattleSide Side) : BattleEvent;
public record SubstituteBrokeEvent(BattleSide Side) : BattleEvent;
