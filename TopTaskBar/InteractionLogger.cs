using System.Diagnostics;
using System;
using System.IO;

namespace TopTaskBar;

internal static class InteractionLogger
{
    private static readonly object SyncRoot = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TopTaskBar",
        "logs");
    private static readonly string LogPath = Path.Combine(LogDirectory, "interaction.log");

    public static string CurrentLogPath => LogPath;

    public static void Log(string message)
    {
        if (!Debugger.IsAttached)
        {
            return;
        }

        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";

        lock (SyncRoot)
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
    }
}
