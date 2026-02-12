using System.Text.Json;

namespace PokemonGen1.Core.Types;

public class TypeChart
{
    private readonly Dictionary<(PokemonType Attacking, PokemonType Defending), float> _chart = new();

    public float GetEffectiveness(PokemonType attacking, PokemonType defending)
    {
        return _chart.TryGetValue((attacking, defending), out var eff) ? eff : 1.0f;
    }

    public float GetTotalEffectiveness(PokemonType attacking, PokemonType defending1, PokemonType? defending2)
    {
        float eff = GetEffectiveness(attacking, defending1);
        if (defending2.HasValue)
            eff *= GetEffectiveness(attacking, defending2.Value);
        return eff;
    }

    public static TypeChart LoadFromJson(string json)
    {
        var chart = new TypeChart();
        var doc = JsonDocument.Parse(json);
        var entries = doc.RootElement.GetProperty("entries");

        foreach (var entry in entries.EnumerateArray())
        {
            var attacking = Enum.Parse<PokemonType>(entry.GetProperty("attacking").GetString()!);
            var defending = Enum.Parse<PokemonType>(entry.GetProperty("defending").GetString()!);
            var multiplier = entry.GetProperty("multiplier").GetSingle();
            chart._chart[(attacking, defending)] = multiplier;
        }

        return chart;
    }
}
