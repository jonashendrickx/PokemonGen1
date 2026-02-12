using System.Text.Json;
using System.Text.Json.Serialization;

namespace PokemonGen1.Core.Save;

public class SaveManager
{
    private const string SaveFileName = "pokemon_save.json";
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string GetSavePath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PokemonGen1");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, SaveFileName);
    }

    public static void Save(SaveData data)
    {
        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(GetSavePath(), json);
    }

    public static SaveData? Load()
    {
        var path = GetSavePath();
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SaveData>(json, Options);
    }
}
