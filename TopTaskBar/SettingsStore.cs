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
        if (TryLoad(out var settings))
        {
            return settings;
        }

        return CreateDefaultSettings();
    }

    public static bool TryLoad(out TopTaskBarSettings settings)
    {
        return TryLoadFromPath(SettingsFilePath, out settings);
    }

    internal static bool TryLoadFromPath(string settingsPath, out TopTaskBarSettings settings)
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                settings = CreateDefaultSettings();
                SaveToPath(settings, settingsPath);
                return true;
            }

            var json = File.ReadAllText(settingsPath);
            settings = JsonSerializer.Deserialize<TopTaskBarSettings>(json) ??
                       throw new JsonException("설정 파일의 루트 개체가 비어 있습니다.");
            var shouldSave = Normalize(settings);

            if (shouldSave)
            {
                SaveToPath(settings, settingsPath);
            }

            return true;
        }
        catch (Exception ex)
        {
            InteractionLogger.Log(
                $"SettingsLoadFailed path=\"{settingsPath}\" type=\"{ex.GetType().FullName}\" message=\"{ex.Message}\"");
            BackupInvalidSettings(settingsPath);
            settings = CreateDefaultSettings();
            return false;
        }
    }

    public static void Save(TopTaskBarSettings settings)
    {
        SaveToPath(settings, SettingsFilePath);
    }

    internal static void SaveToPath(TopTaskBarSettings settings, string settingsPath)
    {
        var directoryPath = Path.GetDirectoryName(settingsPath) ?? ".";
        Directory.CreateDirectory(directoryPath);

        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        var temporaryPath = Path.Combine(
            directoryPath,
            $"{Path.GetFileName(settingsPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, settingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static bool Normalize(TopTaskBarSettings settings)
    {
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

        if (settings.AlarmEntries is null || settings.AlarmEntries.Count == 0)
        {
            settings.AlarmEntries = CreateDefaultAlarmEntries();
            shouldSave = true;
        }

        return shouldSave;
    }

    private static void BackupInvalidSettings(string settingsPath)
    {
        if (!File.Exists(settingsPath))
        {
            return;
        }

        try
        {
            var directoryPath = Path.GetDirectoryName(settingsPath) ?? ".";
            var fileName = Path.GetFileNameWithoutExtension(settingsPath);
            var extension = Path.GetExtension(settingsPath);
            var backupPath = Path.Combine(
                directoryPath,
                $"{fileName}.invalid-{DateTime.Now:yyyyMMdd-HHmmssfff}{extension}");
            File.Copy(settingsPath, backupPath, overwrite: false);
            InteractionLogger.Log($"InvalidSettingsBackedUp source=\"{settingsPath}\" backup=\"{backupPath}\"");
        }
        catch (Exception ex)
        {
            InteractionLogger.Log(
                $"InvalidSettingsBackupFailed path=\"{settingsPath}\" type=\"{ex.GetType().FullName}\" message=\"{ex.Message}\"");
        }
    }

    private static TopTaskBarSettings CreateDefaultSettings()
    {
        return new TopTaskBarSettings
        {
            WindowSlotWidth = 42,
            PinnedApps = CreateDefaultPinnedApps(),
            RecentLauncherPaths = [],
            AlarmEntries = CreateDefaultAlarmEntries()
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
                Name = "명령 프롬프트",
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

    private static List<AlarmEntry> CreateDefaultAlarmEntries()
    {
        return
        [
            new AlarmEntry
            {
                Label = "기상",
                Hour24 = 7,
                Minute = 30,
                Enabled = false,
                DaysOfWeekMask = AlarmDayOfWeek.None
            },
            new AlarmEntry
            {
                Label = "출발",
                Hour24 = 8,
                Minute = 40,
                Enabled = false,
                DaysOfWeekMask = AlarmDayOfWeek.None
            },
            new AlarmEntry
            {
                Label = "복습",
                Hour24 = 21,
                Minute = 0,
                Enabled = false,
                DaysOfWeekMask = AlarmDayOfWeek.None
            }
        ];
    }
}
