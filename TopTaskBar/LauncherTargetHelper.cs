using System;
using System.IO;

namespace TopTaskBar;

internal enum LauncherTargetType
{
    Unknown,
    Executable,
    Shortcut,
    Directory,
    Url
}

internal sealed record RunningAppLauncherTarget(
    string Name,
    string Path,
    string Arguments,
    string WorkingDirectory,
    bool ReplaceBareHostEntry);

internal static class LauncherTargetHelper
{
    private const string HyperVManagerTitleKorean = "Hyper-V 관리자";
    private const string HyperVManagerTitleEnglish = "Hyper-V Manager";

    public static LauncherTargetType GetTargetType(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LauncherTargetType.Unknown;
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return LauncherTargetType.Url;
        }

        if (Directory.Exists(path))
        {
            return LauncherTargetType.Directory;
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".exe" => LauncherTargetType.Executable,
            ".lnk" => LauncherTargetType.Shortcut,
            _ => LauncherTargetType.Unknown
        };
    }

    public static string GetFallbackGlyph(LauncherTargetType targetType)
    {
        return targetType switch
        {
            LauncherTargetType.Directory => "F",
            LauncherTargetType.Url => "W",
            LauncherTargetType.Shortcut => "L",
            _ => "A"
        };
    }

    public static string GetDefaultName(string path, LauncherTargetType targetType)
    {
        return targetType switch
        {
            LauncherTargetType.Directory => new DirectoryInfo(path).Name,
            LauncherTargetType.Url when Uri.TryCreate(path, UriKind.Absolute, out var uri) => uri.Host,
            _ => Path.GetFileNameWithoutExtension(path)
        };
    }

    public static string GetDisplayName(string windowTitle, string executablePath)
    {
        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            string[] separators = [" - ", " — ", " | "];
            foreach (var separator in separators)
            {
                var separatorIndex = windowTitle.LastIndexOf(separator, StringComparison.CurrentCulture);
                if (separatorIndex > 0)
                {
                    var candidate = windowTitle[(separatorIndex + separator.Length)..].Trim();
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return Path.GetFileNameWithoutExtension(executablePath);
    }

    public static bool TryResolveRunningApp(
        string windowTitle,
        string executablePath,
        out RunningAppLauncherTarget? target,
        out string message)
    {
        return TryResolveRunningApp(
            windowTitle,
            executablePath,
            Environment.SystemDirectory,
            File.Exists,
            out target,
            out message);
    }

    internal static bool TryResolveRunningApp(
        string windowTitle,
        string executablePath,
        string systemDirectory,
        Func<string, bool> fileExists,
        out RunningAppLauncherTarget? target,
        out string message)
    {
        target = null;
        message = string.Empty;

        if (!string.Equals(Path.GetFileName(executablePath), "mmc.exe", StringComparison.OrdinalIgnoreCase))
        {
            target = new RunningAppLauncherTarget(
                GetDisplayName(windowTitle, executablePath),
                executablePath,
                string.Empty,
                Path.GetDirectoryName(executablePath) ?? string.Empty,
                false);
            return true;
        }

        var normalizedTitle = windowTitle.Trim();
        var isHyperVManager =
            string.Equals(normalizedTitle, HyperVManagerTitleKorean, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedTitle, HyperVManagerTitleEnglish, StringComparison.OrdinalIgnoreCase);
        if (!isHyperVManager)
        {
            message = "MMC 기반 앱은 실행 파일만으로 구분할 수 없습니다. 바로가기(.lnk)를 추가하거나 JSON에서 콘솔 파일 인수를 지정해 주세요.";
            return false;
        }

        var consolePath = Path.Combine(systemDirectory, "virtmgmt.msc");
        if (!fileExists(consolePath))
        {
            message = "Hyper-V 관리자 콘솔 파일(virtmgmt.msc)을 찾을 수 없습니다.";
            return false;
        }

        target = new RunningAppLauncherTarget(
            HyperVManagerTitleKorean,
            executablePath,
            $"\"{consolePath}\"",
            systemDirectory,
            true);
        return true;
    }

    public static bool TryValidate(string path, LauncherTargetType targetType, out string message)
    {
        message = string.Empty;

        switch (targetType)
        {
            case LauncherTargetType.Executable when !File.Exists(path):
                message = "실행 파일을 찾을 수 없습니다.";
                return false;
            case LauncherTargetType.Shortcut when !File.Exists(path):
                message = "바로가기 파일을 찾을 수 없습니다.";
                return false;
            case LauncherTargetType.Directory when !Directory.Exists(path):
                message = "폴더를 찾을 수 없습니다.";
                return false;
            case LauncherTargetType.Url when !IsHttpUrl(path):
                message = "http 또는 https 형식의 올바른 URL이 아닙니다.";
                return false;
            case LauncherTargetType.Unknown:
                message = "지원하지 않는 런처 항목입니다.";
                return false;
            default:
                return true;
        }
    }

    public static string GetLaunchFailureMessage(LauncherTargetType targetType)
    {
        return targetType switch
        {
            LauncherTargetType.Directory => "폴더를 열지 못했습니다.",
            LauncherTargetType.Url => "URL을 열지 못했습니다.",
            LauncherTargetType.Shortcut => "바로가기를 실행하지 못했습니다.",
            _ => "앱 실행에 실패했습니다."
        };
    }

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !string.Equals(Path.GetFileName(path), "whale.exe", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        var versionDirectory = Path.GetDirectoryName(path);
        var applicationDirectory = string.IsNullOrWhiteSpace(versionDirectory)
            ? null
            : Path.GetDirectoryName(versionDirectory);

        if (string.IsNullOrWhiteSpace(applicationDirectory) ||
            !applicationDirectory.Contains(
                Path.Combine("Naver", "Naver Whale", "Application"),
                StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        var stableWhalePath = Path.Combine(applicationDirectory, "whale.exe");
        return File.Exists(stableWhalePath) ? stableWhalePath : path;
    }

    private static bool IsHttpUrl(string path)
    {
        return Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
