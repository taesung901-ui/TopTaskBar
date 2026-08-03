using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace TopTaskBar;

internal static class InteractionLogger
{
    private const long MaxLogBytes = 5 * 1024 * 1024;
    private const int MaxArchiveCount = 3;
    private static readonly object SyncRoot = new();
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TopTaskBar",
        "logs");
    private static readonly string LogPath = Path.Combine(LogDirectory, "interaction.log");

    public static string CurrentLogPath => LogPath;

    public static void Log(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}";

        lock (SyncRoot)
        {
            try
            {
                AppendLine(LogPath, line, MaxLogBytes, MaxArchiveCount);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TopTaskBar log write failed: {ex.Message}");
            }
        }
    }

    internal static void AppendLine(string logPath, string line, long maxLogBytes, int maxArchiveCount)
    {
        var directoryPath = Path.GetDirectoryName(logPath) ?? ".";
        Directory.CreateDirectory(directoryPath);

        var encodedLength = Encoding.UTF8.GetByteCount(line + Environment.NewLine);
        if (File.Exists(logPath) && new FileInfo(logPath).Length + encodedLength > maxLogBytes)
        {
            Rotate(logPath, maxArchiveCount);
        }

        File.AppendAllText(logPath, line + Environment.NewLine);
    }

    private static void Rotate(string logPath, int maxArchiveCount)
    {
        if (maxArchiveCount <= 0)
        {
            File.Delete(logPath);
            return;
        }

        var oldestArchive = GetArchivePath(logPath, maxArchiveCount);
        if (File.Exists(oldestArchive))
        {
            File.Delete(oldestArchive);
        }

        for (var index = maxArchiveCount - 1; index >= 1; index--)
        {
            var sourcePath = GetArchivePath(logPath, index);
            if (File.Exists(sourcePath))
            {
                File.Move(sourcePath, GetArchivePath(logPath, index + 1));
            }
        }

        File.Move(logPath, GetArchivePath(logPath, 1));
    }

    private static string GetArchivePath(string logPath, int index)
    {
        var directoryPath = Path.GetDirectoryName(logPath) ?? ".";
        var fileName = Path.GetFileNameWithoutExtension(logPath);
        var extension = Path.GetExtension(logPath);
        return Path.Combine(directoryPath, $"{fileName}.{index}{extension}");
    }
}
