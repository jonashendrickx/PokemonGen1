namespace PokemonGen1.Core.Battle;

public abstract record BattleAction;
public record FightAction(int MoveIndex) : BattleAction;
public record SwitchAction(int PartyIndex) : BattleAction;
public record UseItemAction(int ItemId, int? TargetPartyIndex = null) : BattleAction;
public record RunAction() : BattleAction;
