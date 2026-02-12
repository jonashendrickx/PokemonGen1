using PokemonGen1.Core.Battle;
using PokemonGen1.Core.Data;
using PokemonGen1.Core.Moves;
using PokemonGen1.Core.Pokemon;
using PokemonGen1.Core.Types;

namespace PokemonGen1.Core.Tests.Battle;

public class DamageCalculatorTests
{
    private readonly GameData _data;
    private readonly DamageCalculator _calc;

    public DamageCalculatorTests()
    {
        _data = GameData.LoadFromDirectory(FindDataDir());
        // Fixed seed for deterministic tests
        _calc = new DamageCalculator(new Random(42));
    }

    [Fact]
    public void Thunderbolt_Vs_WaterType_SuperEffective()
    {
        var pikachu = _data.GetSpecies(25);
        var squirtle = _data.GetSpecies(7);

        var attacker = CreateBattlePokemon(pikachu, 25);
        var defender = CreateBattlePokemon(squirtle, 25);
        var thunderbolt = _data.GetMove(85);

        var result = _calc.Calculate(attacker, defender, thunderbolt, _data.TypeChart);

        Assert.True(result.Effectiveness > 1.0f, "Thunderbolt should be super effective against Water");
        Assert.True(result.Damage > 0, "Should deal damage");
    }

    [Fact]
    public void Ground_Move_Vs_Electric_NoEffect()
    {
        var geodude = _data.GetSpecies(74);
        var pikachu = _data.GetSpecies(25);

        var attacker = CreateBattlePokemon(geodude, 25);
        var defender = CreateBattlePokemon(pikachu, 25);
        var earthquake = _data.GetMove(89);

        var result = _calc.Calculate(attacker, defender, earthquake, _data.TypeChart);

        // Earthquake is Ground type, Pikachu is Electric - should be super effective
        Assert.True(result.Effectiveness >= 2.0f);
    }

    [Fact]
    public void STAB_IncrasesDamage()
    {
        var pikachu = _data.GetSpecies(25);
        var rattata = _data.GetSpecies(19);

        var attackerPikachu = CreateBattlePokemon(pikachu, 30);
        var defenderRattata = CreateBattlePokemon(rattata, 30);

        // Thunderbolt is Electric, Pikachu is Electric = STAB
        var thunderbolt = _data.GetMove(85);
        // Surf is Water, Pikachu is not Water = no STAB
        // But Surf doesn't exist as Pikachu STAB comparison, let's just verify STAB applies
        var resultWithStab = _calc.Calculate(attackerPikachu, defenderRattata, thunderbolt, _data.TypeChart);

        // Tackle is Normal, Pikachu is Electric = no STAB
        var tackle = _data.GetMove(33);
        var resultNoStab = _calc.Calculate(attackerPikachu, defenderRattata, tackle, _data.TypeChart);

        // Thunderbolt (95 power + STAB) should deal more than Tackle (35 power + no STAB)
        Assert.True(resultWithStab.Damage > resultNoStab.Damage,
            $"STAB Thunderbolt ({resultWithStab.Damage}) should deal more than Tackle ({resultNoStab.Damage})");
    }

    [Fact]
    public void Damage_IsNonNegative()
    {
        var species = _data.GetSpecies(1);
        var attacker = CreateBattlePokemon(species, 5);
        var defender = CreateBattlePokemon(species, 100);
        var tackle = _data.GetMove(33);

        var result = _calc.Calculate(attacker, defender, tackle, _data.TypeChart);
        Assert.True(result.Damage >= 0);
    }

    private static BattlePokemon CreateBattlePokemon(PokemonSpecies species, int level)
    {
        var instance = PokemonInstance.Create(species, level, new Random(1));
        instance.CurrentHp = instance.MaxHp(species);
        return new BattlePokemon(instance, species);
    }

    private static string FindDataDir()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "data");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "pokemon", "species.json")))
                return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new DirectoryNotFoundException("Cannot find data directory");
    }
}
