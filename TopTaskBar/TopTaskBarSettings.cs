namespace TopTaskBar;

public sealed class TopTaskBarSettings
{
    public double WindowSlotWidth { get; set; } = 42;

    public List<LauncherAppSetting> PinnedApps { get; set; } = [];

    public List<string> RecentLauncherPaths { get; set; } = [];

    public List<AlarmEntry> AlarmEntries { get; set; } = [];
}
