using System;
using System.IO;
using System.Text.Json;

namespace TopTaskBar;

internal static class SettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string SettingsDirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TopTaskBar");

    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectoryPath, "settings.json");

    public static TopTaskBarSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return new TopTaskBarSettings();
            }

            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<TopTaskBarSettings>(json) ?? new TopTaskBarSettings();
        }
        catch
        {
            return new TopTaskBarSettings();
        }
    }

    public static void Save(TopTaskBarSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectoryPath);
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(SettingsFilePath, json);
    }
}
