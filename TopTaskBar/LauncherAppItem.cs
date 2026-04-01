namespace TopTaskBar;

public sealed class LauncherAppItem
{
    public required string Name { get; init; }

    public required string Path { get; init; }

    public string Arguments { get; init; } = string.Empty;

    public string WorkingDirectory { get; init; } = string.Empty;

    public System.Windows.Media.ImageSource? Icon { get; init; }

    public string FallbackGlyph { get; init; } = "A";
}
