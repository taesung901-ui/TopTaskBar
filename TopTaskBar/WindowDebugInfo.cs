using System;

namespace TopTaskBar;

internal readonly record struct WindowDebugInfo(
    IntPtr OriginalHandle,
    IntPtr ComparableHandle,
    IntPtr ActionHandle,
    IntPtr ForegroundHandle,
    IntPtr ForegroundComparableHandle,
    bool IsMinimized,
    string Title);
