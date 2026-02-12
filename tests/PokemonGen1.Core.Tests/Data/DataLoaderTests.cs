using PokemonGen1.Core.Data;
using PokemonGen1.Core.Types;

namespace PokemonGen1.Core.Tests.Data;

public class DataLoaderTests
{
    private readonly GameData _data;

    public DataLoaderTests()
    {
        _data = GameData.LoadFromDirectory(FindDataDir());
    }

    [Fact]
    public void LoadsAll151Species()
    {
        Assert.Equal(151, _data.Species.Count);
    }

    [Fact]
    public void LoadsAll165Moves()
    {
        Assert.Equal(165, _data.Moves.Count);
    }

    [Fact]
    public void Bulbasaur_HasCorrectStats()
    {
        var bulbasaur = _data.GetSpecies(1);
        Assert.Equal("Bulbasaur", bulbasaur.Name);
        Assert.Equal(PokemonType.Grass, bulbasaur.Type1);
        Assert.Equal(PokemonType.Poison, bulbasaur.Type2);
        Assert.Equal(45, bulbasaur.BaseHp);
        Assert.Equal(49, bulbasaur.BaseAttack);
        Assert.Equal(49, bulbasaur.BaseDefense);
        Assert.Equal(65, bulbasaur.BaseSpecial);
        Assert.Equal(45, bulbasaur.BaseSpeed);
    }

    [Fact]
    public void Mewtwo_HasCorrectStats()
    {
        var mewtwo = _data.GetSpecies(150);
        Assert.Equal("Mewtwo", mewtwo.Name);
        Assert.Equal(PokemonType.Psychic, mewtwo.Type1);
        Assert.Null(mewtwo.Type2);
        Assert.Equal(106, mewtwo.BaseHp);
        Assert.Equal(110, mewtwo.BaseAttack);
        Assert.Equal(90, mewtwo.BaseDefense);
        Assert.Equal(154, mewtwo.BaseSpecial);
        Assert.Equal(130, mewtwo.BaseSpeed);
    }

    [Fact]
    public void Thunderbolt_HasCorrectData()
    {
        var thunderbolt = _data.GetMove(85);
        Assert.Equal("Thunderbolt", thunderbolt.Name);
        Assert.Equal(PokemonType.Electric, thunderbolt.Type);
        Assert.Equal(95, thunderbolt.Power);
        Assert.Equal(100, thunderbolt.Accuracy);
        Assert.Equal(15, thunderbolt.MaxPP);
    }

    [Fact]
    public void Evolutions_ContainBulbasaurLine()
    {
        var evos = _data.GetEvolutions(1);
        Assert.Single(evos);
        Assert.Equal(2, evos[0].ToSpeciesId);
        Assert.Equal(16, evos[0].Level);
    }

    [Fact]
    public void Items_Loaded()
    {
        Assert.True(_data.Items.Count > 0);
    }

    [Fact]
    public void TypeChart_Loaded()
    {
        Assert.NotNull(_data.TypeChart);
        // Fire > Grass
        Assert.Equal(2.0f, _data.TypeChart.GetEffectiveness(PokemonType.Fire, PokemonType.Grass));
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
