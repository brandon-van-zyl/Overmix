using System.Text.Json;
using System.Text.Json.Serialization;

namespace Overmix.Core.Settings;

/// <summary>Keeps settings in a JSON file, normally in the user's app data folder.</summary>
public sealed class JsonSettingsStore(string path) : ISettingsStore
{
    public static string DefaultPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Overmix", "settings.json");

    public OvermixSettings Load()
    {
        if (!File.Exists(path)) return new OvermixSettings();
        try
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize(stream, SettingsJson.Default.OvermixSettings) ?? new OvermixSettings();
        }
        catch (JsonException)
        {
            // Keep the unreadable file for a look later, and start fresh rather than refusing to run.
            File.Move(path, path + ".broken", overwrite: true);
            return new OvermixSettings();
        }
    }

    public void Save(OvermixSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".saving";
        using (var stream = File.Create(temporary))
        {
            JsonSerializer.Serialize(stream, settings, SettingsJson.Default.OvermixSettings);
        }

        File.Move(temporary, path, overwrite: true);
    }
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(OvermixSettings))]
internal sealed partial class SettingsJson : JsonSerializerContext;
