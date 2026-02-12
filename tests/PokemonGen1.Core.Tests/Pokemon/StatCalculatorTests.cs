using PokemonGen1.Core.Pokemon;

namespace PokemonGen1.Core.Tests.Pokemon;

public class StatCalculatorTests
{
    [Fact]
    public void CalculateHp_Pikachu_Level25_CorrectValue()
    {
        // Pikachu: Base HP 35, DV 15, no stat exp, level 25
        int hp = StatCalculator.CalculateHp(35, 15, 0, 25);
        // ((35+15)*2 + 0) * 25/100 + 25 + 10 = 100*25/100 + 35 = 25 + 35 = 60
        Assert.Equal(60, hp);
    }

    [Fact]
    public void CalculateHp_Chansey_Level100_HighValue()
    {
        // Chansey: Base HP 250, DV 15, no stat exp, level 100
        int hp = StatCalculator.CalculateHp(250, 15, 0, 100);
        // ((250+15)*2 + 0) * 100/100 + 100 + 10 = 530 + 110 = 640
        Assert.Equal(640, hp);
    }

    [Fact]
    public void CalculateStat_Pikachu_Speed_Level25()
    {
        // Pikachu: Base Speed 90, DV 15, no stat exp, level 25
        int speed = StatCalculator.CalculateStat(90, 15, 0, 25);
        // ((90+15)*2 + 0) * 25/100 + 5 = 210*25/100 + 5 = 52 + 5 = 57
        Assert.Equal(57, speed);
    }

    [Fact]
    public void CalculateStat_WithStatExp_IncreasesStat()
    {
        int withoutExp = StatCalculator.CalculateStat(80, 10, 0, 50);
        int withExp = StatCalculator.CalculateStat(80, 10, 65535, 50);
        Assert.True(withExp > withoutExp);
    }

    [Fact]
    public void ExperienceForLevel_MediumFast_Level100()
    {
        int exp = StatCalculator.ExperienceForLevel(GrowthRate.MediumFast, 100);
        Assert.Equal(1000000, exp);
    }

    [Fact]
    public void ExperienceForLevel_Slow_Level100()
    {
        int exp = StatCalculator.ExperienceForLevel(GrowthRate.Slow, 100);
        Assert.Equal(1250000, exp);
    }
}
