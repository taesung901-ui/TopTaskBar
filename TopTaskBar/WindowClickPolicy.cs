using System;

namespace TopTaskBar;

internal static class WindowClickPolicy
{
    private static readonly TimeSpan AutoRevealProtectionDuration = TimeSpan.FromSeconds(3);

    public static bool ShouldProtectAutoRevealedWindow(
        bool isActive,
        bool isMinimized,
        IntPtr clickedWindow,
        IntPtr lastMinimizedWindow,
        DateTime lastMinimizedAtUtc,
        DateTime nowUtc)
    {
        if (!isActive || isMinimized ||
            clickedWindow == IntPtr.Zero ||
            lastMinimizedWindow == IntPtr.Zero ||
            clickedWindow == lastMinimizedWindow)
        {
            return false;
        }

        var elapsed = nowUtc - lastMinimizedAtUtc;
        return elapsed >= TimeSpan.Zero && elapsed <= AutoRevealProtectionDuration;
    }
}
