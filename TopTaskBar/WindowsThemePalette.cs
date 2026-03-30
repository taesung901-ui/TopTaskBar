using System.Windows.Media;
using Brush = System.Windows.Media.Brush;

namespace TopTaskBar;

public sealed class WindowsThemePalette
{
    public required Brush BarBackgroundBrush { get; init; }

    public required Brush BarBorderBrush { get; init; }

    public required Brush PrimaryForegroundBrush { get; init; }

    public required Brush MutedForegroundBrush { get; init; }

    public required Brush AccentBackgroundBrush { get; init; }

    public required Brush AccentBorderBrush { get; init; }

    public required Brush AccentForegroundBrush { get; init; }

    public required Brush AppStripBackgroundBrush { get; init; }

    public required Brush AppStripBorderBrush { get; init; }

    public required Brush ChipBackgroundBrush { get; init; }

    public required Brush ChipBorderBrush { get; init; }
}
