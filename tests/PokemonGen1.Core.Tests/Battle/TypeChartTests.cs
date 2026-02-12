using PokemonGen1.Core.Types;

namespace PokemonGen1.Core.Tests.Battle;

public class TypeChartTests
{
    private readonly TypeChart _chart;

    public TypeChartTests()
    {
        var json = File.ReadAllText(FindDataPath("types/type_chart.json"));
        _chart = TypeChart.LoadFromJson(json);
    }

    [Fact]
    public void Fire_SuperEffective_Against_Grass()
    {
        Assert.Equal(2.0f, _chart.GetEffectiveness(PokemonType.Fire, PokemonType.Grass));
    }

    [Fact]
    public void Water_SuperEffective_Against_Fire()
    {
        Assert.Equal(2.0f, _chart.GetEffectiveness(PokemonType.Water, PokemonType.Fire));
    }

    [Fact]
    public void Electric_NoEffect_Against_Ground()
    {
        Assert.Equal(0.0f, _chart.GetEffectiveness(PokemonType.Electric, PokemonType.Ground));
    }

    [Fact]
    public void Normal_NoEffect_Against_Ghost()
    {
        Assert.Equal(0.0f, _chart.GetEffectiveness(PokemonType.Normal, PokemonType.Ghost));
    }

    [Fact]
    public void Ghost_NoEffect_Against_Psychic_Gen1Bug()
    {
        // Gen 1 specific: Ghost has no effect on Psychic (bug)
        Assert.Equal(0.0f, _chart.GetEffectiveness(PokemonType.Ghost, PokemonType.Psychic));
    }

    [Fact]
    public void Poison_SuperEffective_Against_Bug_Gen1()
    {
        // Gen 1 specific: Poison is super effective against Bug
        Assert.Equal(2.0f, _chart.GetEffectiveness(PokemonType.Poison, PokemonType.Bug));
    }

    [Fact]
    public void Bug_SuperEffective_Against_Poison_Gen1()
    {
        // Gen 1 specific: Bug is super effective against Poison
        Assert.Equal(2.0f, _chart.GetEffectiveness(PokemonType.Bug, PokemonType.Poison));
    }

    [Fact]
    public void Normal_Vs_Normal_IsNeutral()
    {
        Assert.Equal(1.0f, _chart.GetEffectiveness(PokemonType.Normal, PokemonType.Normal));
    }

    [Fact]
    public void Dragon_SuperEffective_Against_Dragon()
    {
        Assert.Equal(2.0f, _chart.GetEffectiveness(PokemonType.Dragon, PokemonType.Dragon));
    }

    [Fact]
    public void GetTotalEffectiveness_DualType()
    {
        // Electric vs Water/Flying (Gyarados): 2.0 * 2.0 = 4.0
        float eff = _chart.GetTotalEffectiveness(PokemonType.Electric, PokemonType.Water, PokemonType.Flying);
        Assert.Equal(4.0f, eff);
    }

    [Fact]
    public void GetTotalEffectiveness_Ground_Vs_Electric_Flying()
    {
        // Ground vs Electric/Flying: 2.0 * 0.0 = 0.0
        float eff = _chart.GetTotalEffectiveness(PokemonType.Ground, PokemonType.Electric, PokemonType.Flying);
        Assert.Equal(0.0f, eff);
    }

    private static string FindDataPath(string relativePath)
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "data", relativePath);
            if (File.Exists(candidate)) return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new FileNotFoundException($"Cannot find data/{relativePath}");
    }
}
