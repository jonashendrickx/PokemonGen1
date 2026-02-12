using PokemonGen1.Core.Data;
using PokemonGen1.Core.Moves;
using PokemonGen1.Core.Pokemon;
using PokemonGen1.Core.Types;

namespace PokemonGen1.Core.Battle;

public class BattleEngine
{
    private readonly BattleState _state;
    private readonly GameData _data;
    private readonly DamageCalculator _damageCalc;
    private readonly Random _rng;

    public BattleState State => _state;

    public BattleEngine(BattleState state, GameData data, Random? rng = null)
    {
        _state = state;
        _data = data;
        _rng = rng ?? new Random();
        _damageCalc = new DamageCalculator(_rng);
    }

    public List<BattleEvent> ExecuteTurn(BattleAction playerAction, BattleAction opponentAction)
    {
        var events = new List<BattleEvent>();
        _state.TurnNumber++;

        // Reset flinch at start of turn
        _state.PlayerActive.IsFlinched = false;
        _state.OpponentActive.IsFlinched = false;

        // Determine turn order
        var (first, firstAction, second, secondAction) = DetermineTurnOrder(playerAction, opponentAction);
        var firstSide = first == _state.PlayerActive ? BattleSide.Player : BattleSide.Opponent;
        var secondSide = firstSide == BattleSide.Player ? BattleSide.Opponent : BattleSide.Player;

        // Execute first action
        ExecuteAction(first, firstAction, firstSide, second, events);

        if (CheckBattleEnd(events)) return events;

        // Execute second action
        ExecuteAction(second, secondAction, secondSide, first, events);

        if (CheckBattleEnd(events)) return events;

        // End of turn effects
        ProcessEndOfTurn(events);

        CheckBattleEnd(events);

        return events;
    }

    private (BattlePokemon first, BattleAction firstAction, BattlePokemon second, BattleAction secondAction)
        DetermineTurnOrder(BattleAction playerAction, BattleAction opponentAction)
    {
        // Switches always go first
        if (playerAction is SwitchAction && opponentAction is not SwitchAction)
            return (_state.PlayerActive, playerAction, _state.OpponentActive, opponentAction);
        if (opponentAction is SwitchAction && playerAction is not SwitchAction)
            return (_state.OpponentActive, opponentAction, _state.PlayerActive, playerAction);

        // Items go before attacks
        if (playerAction is UseItemAction && opponentAction is FightAction)
            return (_state.PlayerActive, playerAction, _state.OpponentActive, opponentAction);
        if (opponentAction is UseItemAction && playerAction is FightAction)
            return (_state.OpponentActive, opponentAction, _state.PlayerActive, playerAction);

        // Run goes first
        if (playerAction is RunAction)
            return (_state.PlayerActive, playerAction, _state.OpponentActive, opponentAction);

        // Priority moves
        int playerPriority = GetMovePriorityForPokemon(_state.PlayerActive, playerAction);
        int opponentPriority = GetMovePriorityForPokemon(_state.OpponentActive, opponentAction);

        if (playerPriority != opponentPriority)
        {
            return playerPriority > opponentPriority
                ? (_state.PlayerActive, playerAction, _state.OpponentActive, opponentAction)
                : (_state.OpponentActive, opponentAction, _state.PlayerActive, playerAction);
        }

        // Speed comparison
        int playerSpeed = _state.PlayerActive.EffectiveSpeed;
        int opponentSpeed = _state.OpponentActive.EffectiveSpeed;

        if (playerSpeed == opponentSpeed)
        {
            // Speed tie: random
            return _rng.Next(2) == 0
                ? (_state.PlayerActive, playerAction, _state.OpponentActive, opponentAction)
                : (_state.OpponentActive, opponentAction, _state.PlayerActive, playerAction);
        }

        return playerSpeed > opponentSpeed
            ? (_state.PlayerActive, playerAction, _state.OpponentActive, opponentAction)
            : (_state.OpponentActive, opponentAction, _state.PlayerActive, playerAction);
    }

    private int GetMovePriorityForPokemon(BattlePokemon pokemon, BattleAction action)
    {
        if (action is FightAction fight && fight.MoveIndex >= 0 && fight.MoveIndex < pokemon.Pokemon.Moves.Length)
        {
            var moveData = _data.GetMove(pokemon.Pokemon.Moves[fight.MoveIndex].MoveId);
            return moveData.Priority;
        }
        return 0;
    }

    private void ExecuteAction(BattlePokemon actor, BattleAction action, BattleSide side,
        BattlePokemon target, List<BattleEvent> events)
    {
        if (actor.Pokemon.IsFainted) return;

        switch (action)
        {
            case RunAction:
                ExecuteRun(actor, target, side, events);
                break;
            case SwitchAction switchAction:
                ExecuteSwitch(side, switchAction.PartyIndex, events);
                break;
            case FightAction fight:
                ExecuteFight(actor, target, side, fight, events);
                break;
            case UseItemAction:
                events.Add(new TextEvent("Item use not yet implemented in battle."));
                break;
        }
    }

    private void ExecuteRun(BattlePokemon actor, BattlePokemon opponent, BattleSide side, List<BattleEvent> events)
    {
        if (_state.Type == BattleType.Trainer)
        {
            events.Add(new TextEvent("Can't run from a trainer battle!"));
            return;
        }

        _state.EscapeAttempts++;
        int actorSpeed = actor.EffectiveSpeed;
        int oppSpeed = opponent.EffectiveSpeed;

        if (actorSpeed >= oppSpeed)
        {
            events.Add(new TextEvent("Got away safely!"));
            _state.IsOver = true;
            _state.Outcome = BattleOutcome.PlayerFled;
            events.Add(new BattleEndedEvent(BattleOutcome.PlayerFled));
            return;
        }

        // Gen 1 escape formula
        int f = actorSpeed * 128 / oppSpeed + 30 * _state.EscapeAttempts;
        if (_rng.Next(256) < f)
        {
            events.Add(new TextEvent("Got away safely!"));
            _state.IsOver = true;
            _state.Outcome = BattleOutcome.PlayerFled;
            events.Add(new BattleEndedEvent(BattleOutcome.PlayerFled));
        }
        else
        {
            events.Add(new TextEvent("Can't escape!"));
        }
    }

    private void ExecuteSwitch(BattleSide side, int partyIndex, List<BattleEvent> events)
    {
        var party = side == BattleSide.Player ? _state.PlayerParty : _state.OpponentParty;
        var species = _data.GetSpecies(party[partyIndex].SpeciesId);

        if (side == BattleSide.Player)
        {
            _state.PlayerActive.ResetVolatile();
            _state.PlayerActiveIndex = partyIndex;
            _state.PlayerActive = new BattlePokemon(party[partyIndex], species);
        }
        else
        {
            _state.OpponentActive.ResetVolatile();
            _state.OpponentActiveIndex = partyIndex;
            _state.OpponentActive = new BattlePokemon(party[partyIndex], species);
        }

        events.Add(new SwitchEvent(side, species.Name));
    }

    private void ExecuteFight(BattlePokemon attacker, BattlePokemon defender, BattleSide side,
        FightAction action, List<BattleEvent> events)
    {
        var otherSide = side == BattleSide.Player ? BattleSide.Opponent : BattleSide.Player;

        // Check if must recharge
        if (attacker.MustRecharge)
        {
            attacker.MustRecharge = false;
            events.Add(new RechargeEvent(side));
            events.Add(new TextEvent($"{GetPokemonName(attacker, side)} must recharge!"));
            return;
        }

        // Check if thrashing
        if (attacker.IsThrashing)
        {
            attacker.ThrashTurns--;
            var thrashMove = _data.GetMove(attacker.ThrashMoveId);
            ExecuteMove(attacker, defender, side, thrashMove, events);
            if (attacker.ThrashTurns <= 0)
            {
                attacker.IsThrashing = false;
                attacker.IsConfused = true;
                attacker.ConfusionTurns = _rng.Next(2, 6);
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} became confused due to fatigue!"));
            }
            return;
        }

        // Check status conditions that prevent moving
        if (!CanAct(attacker, side, events)) return;

        // Check flinch
        if (attacker.IsFlinched)
        {
            events.Add(new TextEvent($"{GetPokemonName(attacker, side)} flinched!"));
            return;
        }

        // Check confusion
        if (attacker.IsConfused)
        {
            attacker.ConfusionTurns--;
            if (attacker.ConfusionTurns <= 0)
            {
                attacker.IsConfused = false;
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} snapped out of confusion!"));
            }
            else
            {
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} is confused!"));
                if (_rng.Next(2) == 0) // 50% chance to hit self
                {
                    int confDamage = CalculateConfusionDamage(attacker);
                    int oldHp = attacker.Pokemon.CurrentHp;
                    attacker.Pokemon.CurrentHp = Math.Max(0, attacker.Pokemon.CurrentHp - confDamage);
                    events.Add(new ConfusionHitSelfEvent(side, confDamage));
                    events.Add(new HpChangedEvent(side, oldHp, attacker.Pokemon.CurrentHp, attacker.MaxHp));
                    if (attacker.Pokemon.IsFainted)
                        events.Add(new FaintedEvent(side, GetPokemonName(attacker, side)));
                    return;
                }
            }
        }

        // Get the move
        if (action.MoveIndex < 0 || action.MoveIndex >= attacker.Pokemon.Moves.Length)
        {
            // Use Struggle
            var struggle = _data.Moves.ContainsKey(165) ? _data.GetMove(165) : CreateStruggle();
            ExecuteMove(attacker, defender, side, struggle, events);
            return;
        }

        var moveInst = attacker.Pokemon.Moves[action.MoveIndex];

        // Check if disabled
        if (moveInst.MoveId == attacker.DisabledMoveId && attacker.DisabledTurns > 0)
        {
            events.Add(new TextEvent($"{_data.GetMove(moveInst.MoveId).Name} is disabled!"));
            return;
        }

        // Check PP
        if (moveInst.CurrentPP <= 0)
        {
            events.Add(new TextEvent("No PP left for this move!"));
            return;
        }

        moveInst.CurrentPP--;
        var move = _data.GetMove(moveInst.MoveId);
        attacker.LastMoveUsed = move.Id;

        ExecuteMove(attacker, defender, side, move, events);
    }

    private void ExecuteMove(BattlePokemon attacker, BattlePokemon defender, BattleSide side,
        MoveData move, List<BattleEvent> events)
    {
        var otherSide = side == BattleSide.Player ? BattleSide.Opponent : BattleSide.Player;
        events.Add(new MoveUsedEvent(side, move.Name));

        // Handle special non-damage moves first
        switch (move.Effect)
        {
            case MoveEffect.Splash:
                events.Add(new TextEvent("But nothing happened!"));
                return;
            case MoveEffect.Teleport:
                if (_state.Type == BattleType.Wild)
                {
                    events.Add(new TextEvent("Got away safely!"));
                    _state.IsOver = true;
                    _state.Outcome = BattleOutcome.PlayerFled;
                    events.Add(new BattleEndedEvent(BattleOutcome.PlayerFled));
                }
                else
                {
                    events.Add(new TextEvent("But it failed!"));
                }
                return;
            case MoveEffect.Haze:
                attacker.ResetVolatile();
                defender.ResetVolatile();
                attacker.Pokemon.Status = StatusCondition.None;
                defender.Pokemon.Status = StatusCondition.None;
                events.Add(new TextEvent("All stat changes were eliminated!"));
                return;
            case MoveEffect.Mist:
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} is shrouded in MIST!"));
                return;
        }

        // Charging moves
        if (move.Effect == MoveEffect.Charge && !attacker.IsCharging)
        {
            attacker.IsCharging = true;
            attacker.ChargingMoveId = move.Id;
            events.Add(new ChargingEvent(side, move.Name));
            string chargeText = move.Name switch
            {
                "Fly" => $"{GetPokemonName(attacker, side)} flew up high!",
                "Dig" => $"{GetPokemonName(attacker, side)} dug a hole!",
                "Solar Beam" or "SolarBeam" => $"{GetPokemonName(attacker, side)} took in sunlight!",
                "Skull Bash" => $"{GetPokemonName(attacker, side)} lowered its head!",
                "Sky Attack" => $"{GetPokemonName(attacker, side)} is glowing!",
                "Razor Wind" => $"{GetPokemonName(attacker, side)} made a whirlwind!",
                _ => $"{GetPokemonName(attacker, side)} is charging up!"
            };
            events.Add(new TextEvent(chargeText));
            return;
        }
        attacker.IsCharging = false;

        // Accuracy check (for moves that can miss)
        if (move.Accuracy > 0 && move.Effect != MoveEffect.Swift)
        {
            if (!AccuracyCalculator.RollAccuracy(move.Accuracy, attacker.AccuracyStage, defender.EvasionStage, _rng))
            {
                events.Add(new MoveMissedEvent(side, move.Name));
                // Crash damage for Jump Kick / High Jump Kick
                if (move.Id == 26 || move.Id == 136) // Jump Kick, High Jump Kick
                {
                    int crashDamage = 1;
                    int oldHp = attacker.Pokemon.CurrentHp;
                    attacker.Pokemon.CurrentHp = Math.Max(0, attacker.Pokemon.CurrentHp - crashDamage);
                    events.Add(new TextEvent($"{GetPokemonName(attacker, side)} kept going and crashed!"));
                    events.Add(new HpChangedEvent(side, oldHp, attacker.Pokemon.CurrentHp, attacker.MaxHp));
                }
                return;
            }
        }

        // Handle special damage moves
        switch (move.Effect)
        {
            case MoveEffect.FixedDamage20:
                ApplyDamage(defender, otherSide, 20, false, 1f, events);
                return;
            case MoveEffect.FixedDamage40:
                ApplyDamage(defender, otherSide, 40, false, 1f, events);
                return;
            case MoveEffect.LevelDamage:
                float eff = _data.TypeChart.GetTotalEffectiveness(move.Type, defender.Type1, defender.Type2);
                if (eff == 0)
                {
                    events.Add(new TextEvent($"It doesn't affect {GetPokemonName(defender, otherSide)}!"));
                    return;
                }
                ApplyDamage(defender, otherSide, attacker.Level, false, 1f, events);
                return;
            case MoveEffect.Psywave:
                int psyDmg = _rng.Next(1, (int)(attacker.Level * 1.5) + 1);
                ApplyDamage(defender, otherSide, psyDmg, false, 1f, events);
                return;
            case MoveEffect.SuperFang:
                int sfDmg = Math.Max(1, defender.Pokemon.CurrentHp / 2);
                ApplyDamage(defender, otherSide, sfDmg, false, 1f, events);
                return;
            case MoveEffect.OHKO:
                if (attacker.EffectiveSpeed < defender.EffectiveSpeed)
                {
                    events.Add(new MoveMissedEvent(side, move.Name));
                    return;
                }
                float ohkoEff = _data.TypeChart.GetTotalEffectiveness(move.Type, defender.Type1, defender.Type2);
                if (ohkoEff == 0)
                {
                    events.Add(new TextEvent($"It doesn't affect {GetPokemonName(defender, otherSide)}!"));
                    return;
                }
                events.Add(new OhkoEvent(otherSide));
                ApplyDamage(defender, otherSide, 65535, false, 1f, events);
                return;
            case MoveEffect.Counter:
                // Counter does 2x the last physical damage received
                events.Add(new TextEvent("Counter is complex - dealing base damage instead."));
                return;
            case MoveEffect.Bide:
                if (!attacker.IsBiding)
                {
                    attacker.IsBiding = true;
                    attacker.BideTurns = _rng.Next(2, 4);
                    attacker.BideDamage = 0;
                    events.Add(new TextEvent($"{GetPokemonName(attacker, side)} is storing energy!"));
                }
                else
                {
                    attacker.BideTurns--;
                    if (attacker.BideTurns <= 0)
                    {
                        attacker.IsBiding = false;
                        int bideDmg = attacker.BideDamage * 2;
                        events.Add(new TextEvent($"{GetPokemonName(attacker, side)} unleashed energy!"));
                        if (bideDmg > 0) ApplyDamage(defender, otherSide, bideDmg, false, 1f, events);
                    }
                    else
                    {
                        events.Add(new TextEvent($"{GetPokemonName(attacker, side)} is storing energy!"));
                    }
                }
                return;
        }

        // Self-targeting status moves
        if (move.Power == 0 && move.Target == MoveTarget.Self)
        {
            ExecuteSelfTargetMove(attacker, side, move, events);
            return;
        }

        // Status moves targeting opponent
        if (move.Power == 0)
        {
            ExecuteStatusMove(attacker, defender, side, otherSide, move, events);
            return;
        }

        // Damaging moves
        ExecuteDamagingMove(attacker, defender, side, otherSide, move, events);
    }

    private void ExecuteDamagingMove(BattlePokemon attacker, BattlePokemon defender,
        BattleSide side, BattleSide otherSide, MoveData move, List<BattleEvent> events)
    {
        // Multi-hit moves
        if (move.Effect == MoveEffect.MultiHit)
        {
            int hits = RollMultiHitCount();
            int totalDamage = 0;
            for (int i = 0; i < hits; i++)
            {
                var result = _damageCalc.Calculate(attacker, defender, move, _data.TypeChart);
                ApplyDamage(defender, otherSide, result.Damage, result.IsCritical, result.Effectiveness, events);
                totalDamage += result.Damage;
                if (defender.Pokemon.IsFainted) break;
            }
            events.Add(new MultiHitEvent(hits));
            return;
        }

        if (move.Effect == MoveEffect.DoubleHit)
        {
            for (int i = 0; i < 2; i++)
            {
                var result = _damageCalc.Calculate(attacker, defender, move, _data.TypeChart);
                ApplyDamage(defender, otherSide, result.Damage, result.IsCritical, result.Effectiveness, events);
                if (defender.Pokemon.IsFainted) break;
            }
            events.Add(new MultiHitEvent(2));
            return;
        }

        // Standard damage calculation
        var dmgResult = _damageCalc.Calculate(attacker, defender, move, _data.TypeChart);

        if (dmgResult.Effectiveness == 0f)
        {
            events.Add(new TextEvent($"It doesn't affect {GetPokemonName(defender, otherSide)}!"));
            return;
        }

        ApplyDamage(defender, otherSide, dmgResult.Damage, dmgResult.IsCritical, dmgResult.Effectiveness, events);

        // Recoil
        if (move.Effect == MoveEffect.RecoilThird)
        {
            int recoilDmg = Math.Max(1, dmgResult.Damage / 4);
            int oldHp = attacker.Pokemon.CurrentHp;
            attacker.Pokemon.CurrentHp = Math.Max(0, attacker.Pokemon.CurrentHp - recoilDmg);
            events.Add(new RecoilEvent(side, recoilDmg));
            events.Add(new HpChangedEvent(side, oldHp, attacker.Pokemon.CurrentHp, attacker.MaxHp));
            if (attacker.Pokemon.IsFainted)
                events.Add(new FaintedEvent(side, GetPokemonName(attacker, side)));
        }

        // Drain
        if (move.Effect == MoveEffect.Drain || move.Effect == MoveEffect.DreamEater)
        {
            int drainAmount = Math.Max(1, dmgResult.Damage / 2);
            int oldHp = attacker.Pokemon.CurrentHp;
            attacker.Pokemon.CurrentHp = Math.Min(attacker.MaxHp, attacker.Pokemon.CurrentHp + drainAmount);
            events.Add(new DrainEvent(side, attacker.Pokemon.CurrentHp - oldHp));
            events.Add(new HpChangedEvent(side, oldHp, attacker.Pokemon.CurrentHp, attacker.MaxHp));
        }

        // Explosion - attacker faints
        if (move.Effect == MoveEffect.Explosion)
        {
            int oldHp = attacker.Pokemon.CurrentHp;
            attacker.Pokemon.CurrentHp = 0;
            events.Add(new HpChangedEvent(side, oldHp, 0, attacker.MaxHp));
            events.Add(new FaintedEvent(side, GetPokemonName(attacker, side)));
        }

        // Recharge (Hyper Beam)
        if (move.Effect == MoveEffect.Recharge && !defender.Pokemon.IsFainted)
        {
            attacker.MustRecharge = true;
        }

        // Thrash / Petal Dance
        if (move.Effect == MoveEffect.Thrash || move.Effect == MoveEffect.PetalDance)
        {
            if (!attacker.IsThrashing)
            {
                attacker.IsThrashing = true;
                attacker.ThrashTurns = _rng.Next(1, 3); // 1-2 more turns
                attacker.ThrashMoveId = move.Id;
            }
        }

        // Secondary effects
        if (move.EffectChance > 0 && !defender.Pokemon.IsFainted)
        {
            if (_rng.Next(100) < move.EffectChance)
            {
                ApplySecondaryEffect(attacker, defender, side, otherSide, move, events);
            }
        }
        else if (move.EffectChance == 0 && move.Power > 0)
        {
            // Some moves have guaranteed secondary effects encoded in their Effect field
            switch (move.Effect)
            {
                case MoveEffect.HighCrit:
                    break; // Already handled in damage calc
                case MoveEffect.Trapping:
                    if (!defender.Pokemon.IsFainted)
                    {
                        defender.IsTrapped = true;
                        defender.TrapTurns = _rng.Next(2, 6);
                        events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} was trapped!"));
                    }
                    break;
            }
        }
    }

    private void ExecuteSelfTargetMove(BattlePokemon attacker, BattleSide side, MoveData move, List<BattleEvent> events)
    {
        switch (move.Effect)
        {
            case MoveEffect.AttackUp1: ApplyStatChange(attacker, side, "attack", 1, events); break;
            case MoveEffect.AttackUp2: ApplyStatChange(attacker, side, "attack", 2, events); break;
            case MoveEffect.DefenseUp1: ApplyStatChange(attacker, side, "defense", 1, events); break;
            case MoveEffect.DefenseUp2: ApplyStatChange(attacker, side, "defense", 2, events); break;
            case MoveEffect.SpecialUp1: ApplyStatChange(attacker, side, "special", 1, events); break;
            case MoveEffect.SpecialUp2: ApplyStatChange(attacker, side, "special", 2, events); break;
            case MoveEffect.SpeedUp2: ApplyStatChange(attacker, side, "speed", 2, events); break;
            case MoveEffect.EvasionUp1: ApplyStatChange(attacker, side, "evasion", 1, events); break;
            case MoveEffect.Growth: ApplyStatChange(attacker, side, "special", 1, events); break;
            case MoveEffect.Minimize: ApplyStatChange(attacker, side, "evasion", 1, events); break;
            case MoveEffect.Recover:
                int healAmount = attacker.MaxHp / 2;
                int oldHp = attacker.Pokemon.CurrentHp;
                if (attacker.Pokemon.CurrentHp >= attacker.MaxHp)
                {
                    events.Add(new TextEvent("HP is already full!"));
                    return;
                }
                attacker.Pokemon.CurrentHp = Math.Min(attacker.MaxHp, attacker.Pokemon.CurrentHp + healAmount);
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} recovered health!"));
                events.Add(new HpChangedEvent(side, oldHp, attacker.Pokemon.CurrentHp, attacker.MaxHp));
                break;
            case MoveEffect.Rest:
                if (attacker.Pokemon.CurrentHp >= attacker.MaxHp)
                {
                    events.Add(new TextEvent("But it failed!"));
                    return;
                }
                oldHp = attacker.Pokemon.CurrentHp;
                attacker.Pokemon.CurrentHp = attacker.MaxHp;
                attacker.Pokemon.Status = StatusCondition.Sleep;
                attacker.SleepTurns = 2;
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} went to sleep and became healthy!"));
                events.Add(new HpChangedEvent(side, oldHp, attacker.Pokemon.CurrentHp, attacker.MaxHp));
                events.Add(new StatusAppliedEvent(side, StatusCondition.Sleep));
                break;
            case MoveEffect.Reflect:
                if (attacker.HasReflect)
                {
                    events.Add(new TextEvent("But it failed!"));
                    return;
                }
                attacker.HasReflect = true;
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} gained armor!"));
                break;
            case MoveEffect.LightScreen:
                if (attacker.HasLightScreen)
                {
                    events.Add(new TextEvent("But it failed!"));
                    return;
                }
                attacker.HasLightScreen = true;
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)}'s protected against special attacks!"));
                break;
            case MoveEffect.FocusEnergy:
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} is getting pumped!"));
                break;
            case MoveEffect.Substitute:
                int subCost = attacker.MaxHp / 4;
                if (attacker.Pokemon.CurrentHp <= subCost)
                {
                    events.Add(new TextEvent("Too weak to make a SUBSTITUTE!"));
                    return;
                }
                if (attacker.HasSubstitute)
                {
                    events.Add(new TextEvent("Already has a SUBSTITUTE!"));
                    return;
                }
                oldHp = attacker.Pokemon.CurrentHp;
                attacker.Pokemon.CurrentHp -= subCost;
                attacker.HasSubstitute = true;
                attacker.SubstituteHp = subCost;
                events.Add(new SubstituteCreatedEvent(side));
                events.Add(new HpChangedEvent(side, oldHp, attacker.Pokemon.CurrentHp, attacker.MaxHp));
                break;
            case MoveEffect.Conversion:
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} transformed its type!"));
                break;
            case MoveEffect.Metronome:
                // Pick a random move (not Metronome or Struggle)
                var allMoves = _data.Moves.Values.Where(m => m.Id != 118 && m.Id != 165).ToList();
                var randomMove = allMoves[_rng.Next(allMoves.Count)];
                events.Add(new TextEvent($"Metronome became {randomMove.Name}!"));
                var otherSide = side == BattleSide.Player ? BattleSide.Opponent : BattleSide.Player;
                var defender = side == BattleSide.Player ? _state.OpponentActive : _state.PlayerActive;
                ExecuteMove(attacker, defender, side, randomMove, events);
                break;
            default:
                events.Add(new TextEvent("But nothing happened!"));
                break;
        }
    }

    private void ExecuteStatusMove(BattlePokemon attacker, BattlePokemon defender,
        BattleSide side, BattleSide otherSide, MoveData move, List<BattleEvent> events)
    {
        switch (move.Effect)
        {
            case MoveEffect.Sleep:
                if (defender.Pokemon.Status != StatusCondition.None)
                {
                    events.Add(new TextEvent("But it failed!"));
                    return;
                }
                defender.Pokemon.Status = StatusCondition.Sleep;
                defender.SleepTurns = _rng.Next(1, 8);
                events.Add(new StatusAppliedEvent(otherSide, StatusCondition.Sleep));
                events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} fell asleep!"));
                break;
            case MoveEffect.Poison:
                if (defender.Pokemon.Status != StatusCondition.None)
                {
                    events.Add(new TextEvent("But it failed!"));
                    return;
                }
                if (defender.Type1 == PokemonType.Poison || defender.Type2 == PokemonType.Poison)
                {
                    events.Add(new TextEvent("It doesn't affect the foe..."));
                    return;
                }
                // Toxic makes it BadlyPoisoned
                bool isToxic = move.Id == 92;
                defender.Pokemon.Status = isToxic ? StatusCondition.BadlyPoisoned : StatusCondition.Poison;
                if (isToxic) defender.ToxicCounter = 1;
                events.Add(new StatusAppliedEvent(otherSide, defender.Pokemon.Status));
                events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} was poisoned!"));
                break;
            case MoveEffect.Paralysis:
                if (defender.Pokemon.Status != StatusCondition.None)
                {
                    events.Add(new TextEvent("But it failed!"));
                    return;
                }
                if (move.Type == PokemonType.Electric &&
                    (defender.Type1 == PokemonType.Ground || defender.Type2 == PokemonType.Ground))
                {
                    events.Add(new TextEvent("It doesn't affect the foe..."));
                    return;
                }
                defender.Pokemon.Status = StatusCondition.Paralysis;
                events.Add(new StatusAppliedEvent(otherSide, StatusCondition.Paralysis));
                events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} is paralyzed! It may be unable to move!"));
                break;
            case MoveEffect.Confusion:
                if (defender.IsConfused)
                {
                    events.Add(new TextEvent("It's already confused!"));
                    return;
                }
                defender.IsConfused = true;
                defender.ConfusionTurns = _rng.Next(2, 6);
                events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} became confused!"));
                break;
            case MoveEffect.LeechSeed:
                if (defender.Type1 == PokemonType.Grass || defender.Type2 == PokemonType.Grass)
                {
                    events.Add(new TextEvent("It doesn't affect the foe..."));
                    return;
                }
                if (defender.IsSeeded)
                {
                    events.Add(new TextEvent("But it failed!"));
                    return;
                }
                defender.IsSeeded = true;
                events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} was seeded!"));
                break;
            case MoveEffect.Disable:
                if (defender.DisabledTurns > 0 || defender.LastMoveUsed == 0)
                {
                    events.Add(new TextEvent("But it failed!"));
                    return;
                }
                defender.DisabledMoveId = defender.LastMoveUsed;
                defender.DisabledTurns = _rng.Next(1, 9);
                events.Add(new TextEvent($"{_data.GetMove(defender.LastMoveUsed).Name} was disabled!"));
                break;
            case MoveEffect.AccuracyDown1:
                ApplyStatChange(defender, otherSide, "accuracy", -1, events);
                break;
            case MoveEffect.AttackDown1:
                ApplyStatChange(defender, otherSide, "attack", -1, events);
                break;
            case MoveEffect.DefenseDown1:
                ApplyStatChange(defender, otherSide, "defense", -1, events);
                break;
            case MoveEffect.DefenseDown2:
                ApplyStatChange(defender, otherSide, "defense", -2, events);
                break;
            case MoveEffect.SpeedDown1:
                ApplyStatChange(defender, otherSide, "speed", -1, events);
                break;
            case MoveEffect.SpecialDown1:
                ApplyStatChange(defender, otherSide, "special", -1, events);
                break;
            case MoveEffect.Transform:
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} transformed into {GetPokemonName(defender, otherSide)}!"));
                break;
            case MoveEffect.Mimic:
                events.Add(new TextEvent($"{GetPokemonName(attacker, side)} learned a move!"));
                break;
            case MoveEffect.MirrorMove:
                if (defender.LastMoveUsed > 0 && _data.Moves.ContainsKey(defender.LastMoveUsed))
                {
                    var mirroredMove = _data.GetMove(defender.LastMoveUsed);
                    events.Add(new TextEvent($"Mirror Move became {mirroredMove.Name}!"));
                    ExecuteMove(attacker, defender, side, mirroredMove, events);
                }
                else
                {
                    events.Add(new TextEvent("But it failed!"));
                }
                break;
            default:
                events.Add(new TextEvent("But nothing happened!"));
                break;
        }
    }

    private void ApplySecondaryEffect(BattlePokemon attacker, BattlePokemon defender,
        BattleSide side, BattleSide otherSide, MoveData move, List<BattleEvent> events)
    {
        switch (move.Effect)
        {
            case MoveEffect.Burn:
                if (defender.Pokemon.Status == StatusCondition.None &&
                    defender.Type1 != PokemonType.Fire && defender.Type2 != PokemonType.Fire)
                {
                    defender.Pokemon.Status = StatusCondition.Burn;
                    events.Add(new StatusAppliedEvent(otherSide, StatusCondition.Burn));
                    events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} was burned!"));
                }
                break;
            case MoveEffect.Freeze:
                if (defender.Pokemon.Status == StatusCondition.None &&
                    defender.Type1 != PokemonType.Ice && defender.Type2 != PokemonType.Ice)
                {
                    defender.Pokemon.Status = StatusCondition.Freeze;
                    events.Add(new StatusAppliedEvent(otherSide, StatusCondition.Freeze));
                    events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} was frozen solid!"));
                }
                break;
            case MoveEffect.Paralysis:
                if (defender.Pokemon.Status == StatusCondition.None)
                {
                    defender.Pokemon.Status = StatusCondition.Paralysis;
                    events.Add(new StatusAppliedEvent(otherSide, StatusCondition.Paralysis));
                    events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} is paralyzed!"));
                }
                break;
            case MoveEffect.Poison:
                if (defender.Pokemon.Status == StatusCondition.None &&
                    defender.Type1 != PokemonType.Poison && defender.Type2 != PokemonType.Poison)
                {
                    defender.Pokemon.Status = StatusCondition.Poison;
                    events.Add(new StatusAppliedEvent(otherSide, StatusCondition.Poison));
                    events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} was poisoned!"));
                }
                break;
            case MoveEffect.Confusion:
                if (!defender.IsConfused)
                {
                    defender.IsConfused = true;
                    defender.ConfusionTurns = _rng.Next(2, 6);
                    events.Add(new TextEvent($"{GetPokemonName(defender, otherSide)} became confused!"));
                }
                break;
            case MoveEffect.Flinch:
                defender.IsFlinched = true;
                break;
            case MoveEffect.AttackDown1:
                ApplyStatChange(defender, otherSide, "attack", -1, events);
                break;
            case MoveEffect.DefenseDown1:
                ApplyStatChange(defender, otherSide, "defense", -1, events);
                break;
            case MoveEffect.SpeedDown1:
                ApplyStatChange(defender, otherSide, "speed", -1, events);
                break;
            case MoveEffect.SpecialDown1:
                ApplyStatChange(defender, otherSide, "special", -1, events);
                break;
        }
    }

    private void ApplyDamage(BattlePokemon target, BattleSide targetSide, int damage,
        bool isCritical, float effectiveness, List<BattleEvent> events)
    {
        // Apply to substitute first
        if (target.HasSubstitute && damage > 0)
        {
            target.SubstituteHp -= damage;
            if (target.SubstituteHp <= 0)
            {
                target.HasSubstitute = false;
                target.SubstituteHp = 0;
                events.Add(new SubstituteBrokeEvent(targetSide));
            }
            events.Add(new DamageDealtEvent(targetSide, damage, isCritical, effectiveness));
            return;
        }

        int oldHp = target.Pokemon.CurrentHp;
        target.Pokemon.CurrentHp = Math.Max(0, target.Pokemon.CurrentHp - damage);

        events.Add(new DamageDealtEvent(targetSide, damage, isCritical, effectiveness));
        events.Add(new HpChangedEvent(targetSide, oldHp, target.Pokemon.CurrentHp, target.MaxHp));

        if (effectiveness > 1f)
            events.Add(new TextEvent("It's super effective!"));
        else if (effectiveness < 1f && effectiveness > 0f)
            events.Add(new TextEvent("It's not very effective..."));

        if (isCritical)
            events.Add(new TextEvent("A critical hit!"));

        if (target.Pokemon.IsFainted)
            events.Add(new FaintedEvent(targetSide, GetPokemonName(target, targetSide)));

        // Track damage for Bide
        if (target.IsBiding)
            target.BideDamage += damage;
    }

    private void ApplyStatChange(BattlePokemon target, BattleSide side, string stat, int stages, List<BattleEvent> events)
    {
        int actual = target.ModifyStage(stat, stages);
        if (actual == 0)
        {
            string msg = stages > 0
                ? $"{GetPokemonName(target, side)}'s {stat} won't go any higher!"
                : $"{GetPokemonName(target, side)}'s {stat} won't go any lower!";
            events.Add(new TextEvent(msg));
        }
        else
        {
            string change = Math.Abs(actual) switch
            {
                1 => stages > 0 ? "rose!" : "fell!",
                2 => stages > 0 ? "rose sharply!" : "fell harshly!",
                _ => stages > 0 ? "rose drastically!" : "fell severely!"
            };
            events.Add(new StatChangedEvent(side, stat, actual));
            events.Add(new TextEvent($"{GetPokemonName(target, side)}'s {stat} {change}"));
        }
    }

    private bool CanAct(BattlePokemon pokemon, BattleSide side, List<BattleEvent> events)
    {
        switch (pokemon.Pokemon.Status)
        {
            case StatusCondition.Sleep:
                pokemon.SleepTurns--;
                if (pokemon.SleepTurns <= 0)
                {
                    pokemon.Pokemon.Status = StatusCondition.None;
                    events.Add(new TextEvent($"{GetPokemonName(pokemon, side)} woke up!"));
                    return true;
                }
                events.Add(new StatusPreventedMoveEvent(side, StatusCondition.Sleep));
                events.Add(new TextEvent($"{GetPokemonName(pokemon, side)} is fast asleep!"));
                return false;

            case StatusCondition.Freeze:
                // In Gen 1, freeze is permanent unless hit by a Fire move
                events.Add(new StatusPreventedMoveEvent(side, StatusCondition.Freeze));
                events.Add(new TextEvent($"{GetPokemonName(pokemon, side)} is frozen solid!"));
                return false;

            case StatusCondition.Paralysis:
                if (_rng.Next(4) == 0) // 25% chance can't move
                {
                    events.Add(new StatusPreventedMoveEvent(side, StatusCondition.Paralysis));
                    events.Add(new TextEvent($"{GetPokemonName(pokemon, side)} is fully paralyzed!"));
                    return false;
                }
                return true;

            default:
                return true;
        }
    }

    private void ProcessEndOfTurn(List<BattleEvent> events)
    {
        ProcessEndOfTurnForSide(_state.PlayerActive, BattleSide.Player, _state.OpponentActive, events);
        if (!_state.PlayerActive.Pokemon.IsFainted)
            ProcessEndOfTurnForSide(_state.OpponentActive, BattleSide.Opponent, _state.PlayerActive, events);
    }

    private void ProcessEndOfTurnForSide(BattlePokemon pokemon, BattleSide side,
        BattlePokemon opponent, List<BattleEvent> events)
    {
        if (pokemon.Pokemon.IsFainted) return;

        // Burn damage: 1/16 max HP
        if (pokemon.Pokemon.Status == StatusCondition.Burn)
        {
            int burnDmg = Math.Max(1, pokemon.MaxHp / 16);
            int oldHp = pokemon.Pokemon.CurrentHp;
            pokemon.Pokemon.CurrentHp = Math.Max(0, pokemon.Pokemon.CurrentHp - burnDmg);
            events.Add(new StatusDamageEvent(side, StatusCondition.Burn, burnDmg));
            events.Add(new HpChangedEvent(side, oldHp, pokemon.Pokemon.CurrentHp, pokemon.MaxHp));
            if (pokemon.Pokemon.IsFainted)
            {
                events.Add(new FaintedEvent(side, GetPokemonName(pokemon, side)));
                return;
            }
        }

        // Poison damage: 1/16 max HP
        if (pokemon.Pokemon.Status == StatusCondition.Poison)
        {
            int poisonDmg = Math.Max(1, pokemon.MaxHp / 16);
            int oldHp = pokemon.Pokemon.CurrentHp;
            pokemon.Pokemon.CurrentHp = Math.Max(0, pokemon.Pokemon.CurrentHp - poisonDmg);
            events.Add(new StatusDamageEvent(side, StatusCondition.Poison, poisonDmg));
            events.Add(new HpChangedEvent(side, oldHp, pokemon.Pokemon.CurrentHp, pokemon.MaxHp));
            if (pokemon.Pokemon.IsFainted)
            {
                events.Add(new FaintedEvent(side, GetPokemonName(pokemon, side)));
                return;
            }
        }

        // Toxic damage: N/16 max HP, increasing each turn
        if (pokemon.Pokemon.Status == StatusCondition.BadlyPoisoned)
        {
            int toxicDmg = Math.Max(1, pokemon.MaxHp * pokemon.ToxicCounter / 16);
            pokemon.ToxicCounter++;
            int oldHp = pokemon.Pokemon.CurrentHp;
            pokemon.Pokemon.CurrentHp = Math.Max(0, pokemon.Pokemon.CurrentHp - toxicDmg);
            events.Add(new StatusDamageEvent(side, StatusCondition.BadlyPoisoned, toxicDmg));
            events.Add(new HpChangedEvent(side, oldHp, pokemon.Pokemon.CurrentHp, pokemon.MaxHp));
            if (pokemon.Pokemon.IsFainted)
            {
                events.Add(new FaintedEvent(side, GetPokemonName(pokemon, side)));
                return;
            }
        }

        // Leech Seed: drain 1/16 max HP
        if (pokemon.IsSeeded && !opponent.Pokemon.IsFainted)
        {
            int seedDmg = Math.Max(1, pokemon.MaxHp / 16);
            int oldHp = pokemon.Pokemon.CurrentHp;
            pokemon.Pokemon.CurrentHp = Math.Max(0, pokemon.Pokemon.CurrentHp - seedDmg);
            events.Add(new TextEvent($"Leech Seed saps {GetPokemonName(pokemon, side)}!"));
            events.Add(new HpChangedEvent(side, oldHp, pokemon.Pokemon.CurrentHp, pokemon.MaxHp));

            var oppSide = side == BattleSide.Player ? BattleSide.Opponent : BattleSide.Player;
            int oppOldHp = opponent.Pokemon.CurrentHp;
            opponent.Pokemon.CurrentHp = Math.Min(opponent.MaxHp, opponent.Pokemon.CurrentHp + seedDmg);
            events.Add(new HpChangedEvent(oppSide, oppOldHp, opponent.Pokemon.CurrentHp, opponent.MaxHp));

            if (pokemon.Pokemon.IsFainted)
            {
                events.Add(new FaintedEvent(side, GetPokemonName(pokemon, side)));
                return;
            }
        }

        // Trapping damage
        if (pokemon.IsTrapped)
        {
            pokemon.TrapTurns--;
            if (pokemon.TrapTurns <= 0)
            {
                pokemon.IsTrapped = false;
                events.Add(new TextEvent($"{GetPokemonName(pokemon, side)} was freed!"));
            }
        }

        // Disable countdown
        if (pokemon.DisabledTurns > 0)
        {
            pokemon.DisabledTurns--;
            if (pokemon.DisabledTurns <= 0)
            {
                pokemon.DisabledMoveId = 0;
                events.Add(new TextEvent($"{GetPokemonName(pokemon, side)}'s move is no longer disabled!"));
            }
        }
    }

    private bool CheckBattleEnd(List<BattleEvent> events)
    {
        if (_state.IsOver) return true;

        if (_state.PlayerActive.Pokemon.IsFainted)
        {
            if (!_state.PlayerHasAlivePokemon())
            {
                _state.IsOver = true;
                _state.Outcome = BattleOutcome.PlayerLose;
                events.Add(new TextEvent("You have no more Pokemon that can fight!"));
                events.Add(new BattleEndedEvent(BattleOutcome.PlayerLose));
                return true;
            }
            // Player needs to switch - handled by UI
        }

        if (_state.OpponentActive.Pokemon.IsFainted)
        {
            // Award experience
            int expGain = CalculateExpGain();
            if (expGain > 0)
                events.Add(new ExperienceGainedEvent(
                    GetPokemonName(_state.PlayerActive, BattleSide.Player), expGain));

            if (!_state.OpponentHasAlivePokemon())
            {
                _state.IsOver = true;
                _state.Outcome = BattleOutcome.PlayerWin;
                events.Add(new TextEvent("You won the battle!"));
                events.Add(new BattleEndedEvent(BattleOutcome.PlayerWin));
                return true;
            }

            // AI sends out next Pokemon
            var nextIdx = _state.GetNextAliveOpponent();
            if (nextIdx.HasValue)
            {
                var nextSpecies = _data.GetSpecies(_state.OpponentParty[nextIdx.Value].SpeciesId);
                _state.OpponentActiveIndex = nextIdx.Value;
                _state.OpponentActive = new BattlePokemon(_state.OpponentParty[nextIdx.Value], nextSpecies);
                events.Add(new SwitchEvent(BattleSide.Opponent, nextSpecies.Name));
            }
        }

        return false;
    }

    private int CalculateExpGain()
    {
        var defeated = _state.OpponentActive;
        int baseExp = defeated.Species.BaseExpYield;
        int level = defeated.Level;
        bool isTrainer = _state.Type == BattleType.Trainer;
        float trainerBonus = isTrainer ? 1.5f : 1.0f;
        return (int)(baseExp * level * trainerBonus / 7);
    }

    private int CalculateConfusionDamage(BattlePokemon pokemon)
    {
        // Confusion self-hit uses a typeless 40-power physical attack
        int level = pokemon.Level;
        int atk = pokemon.EffectiveAttack;
        int def = pokemon.EffectiveDefense;
        int damage = (2 * level / 5 + 2) * 40 * atk / def;
        damage = damage / 50 + 2;
        return Math.Max(1, damage);
    }

    private int RollMultiHitCount()
    {
        // Gen 1: 2 hits 37.5%, 3 hits 37.5%, 4 hits 12.5%, 5 hits 12.5%
        int roll = _rng.Next(8);
        return roll switch
        {
            0 or 1 or 2 => 2,
            3 or 4 or 5 => 3,
            6 => 4,
            _ => 5
        };
    }

    private string GetPokemonName(BattlePokemon pokemon, BattleSide side)
    {
        string name = pokemon.Pokemon.Nickname ?? pokemon.Species.Name;
        return side == BattleSide.Opponent ? $"Enemy {name}" : name;
    }

    private static MoveData CreateStruggle()
    {
        return new MoveData
        {
            Id = 165,
            Name = "Struggle",
            Type = PokemonType.Normal,
            Power = 50,
            Accuracy = 100,
            MaxPP = 1,
            Effect = MoveEffect.RecoilThird,
            Target = MoveTarget.SingleOpponent
        };
    }
}
