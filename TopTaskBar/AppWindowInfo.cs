using System;
using System.Windows.Media;

namespace TopTaskBar;

public sealed class AppWindowInfo
{
    public required IntPtr Hwnd { get; init; }

    public required string Title { get; init; }

    public ImageSource? Icon { get; init; }

    public bool IsActive { get; init; }
}
