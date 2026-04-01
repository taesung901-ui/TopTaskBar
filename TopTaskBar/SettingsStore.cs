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

    public static string SettingsPath => SettingsFilePath;

    public static TopTaskBarSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                var defaultSettings = CreateDefaultSettings();
                Save(defaultSettings);
                return defaultSettings;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<TopTaskBarSettings>(json) ?? CreateDefaultSettings();
            var shouldSave = false;

            if (settings.PinnedApps is null || settings.PinnedApps.Count == 0)
            {
                settings.PinnedApps = CreateDefaultPinnedApps();
                shouldSave = true;
            }

            if (settings.RecentLauncherPaths is null)
            {
                settings.RecentLauncherPaths = [];
                shouldSave = true;
            }

            if (shouldSave)
            {
                Save(settings);
            }

            return settings;
        }
        catch
        {
            return CreateDefaultSettings();
        }
    }

    public static void Save(TopTaskBarSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectoryPath);
        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    private static TopTaskBarSettings CreateDefaultSettings()
    {
        return new TopTaskBarSettings
        {
            WindowSlotWidth = 42,
            PinnedApps = CreateDefaultPinnedApps(),
            RecentLauncherPaths = []
        };
    }

    private static List<LauncherAppSetting> CreateDefaultPinnedApps()
    {
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var systemDirectory = Environment.SystemDirectory;
        var documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        return
        [
            new LauncherAppSetting
            {
                Name = "Explorer",
                Path = Path.Combine(windowsDirectory, "explorer.exe"),
                WorkingDirectory = windowsDirectory
            },
            new LauncherAppSetting
            {
                Name = "Notepad",
                Path = Path.Combine(systemDirectory, "notepad.exe"),
                WorkingDirectory = systemDirectory
            },
            new LauncherAppSetting
            {
                Name = "Command Prompt",
                Path = Path.Combine(systemDirectory, "cmd.exe"),
                WorkingDirectory = systemDirectory
            },
            new LauncherAppSetting
            {
                Name = "! Documents",
                Path = documentsDirectory,
                WorkingDirectory = documentsDirectory
            },
            new LauncherAppSetting
            {
                Name = "! Google",
                Path = "https://www.google.com",
                WorkingDirectory = string.Empty
            }
        ];
    }
}
