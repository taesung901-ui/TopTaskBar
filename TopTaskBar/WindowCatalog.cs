using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TopTaskBar;

internal static class WindowCatalog
{
    private const int PreferredIconSize = 32;
    private const int DwmaCloaked = 14;
    private const int GwlExstyle = -20;
    private const uint GaRoot = 2;
    private const uint GaRootOwner = 3;
    private const long WsExToolwindow = 0x00000080L;
    private const long WsExAppwindow = 0x00040000L;
    private const int GwOwner = 4;
    private const int IconSmall = 0;
    private const int IconBig = 1;
    private const int IconSmall2 = 2;
    private const int WmGeticon = 0x007F;
    private const int SwRestore = 9;
    private const int SwMinimize = 6;
    private const int SwShow = 5;
    private const int WmSyscommand = 0x0112;
    private static readonly IntPtr ScMinimize = new(0xF020);
    private static readonly IntPtr ScRestore = new(0xF120);
    private const uint ProcessQueryLimitedInformation = 0x1000;
    private const uint ShgfiIcon = 0x000000100;
    private const uint ShgfiLargeIcon = 0x000000000;
    private static readonly IntPtr GclpHicon = new(-14);
    private static readonly IntPtr GclpHiconsm = new(-34);

    public static IReadOnlyList<AppWindowInfo> GetOpenWindows(IntPtr excludedHwnd)
    {
        var windows = new List<AppWindowInfo>();
        var shellWindow = GetShellWindow();
        var foregroundWindow = GetComparableWindow(GetForegroundWindow());

        EnumWindows((hwnd, _) =>
        {
            if (!ShouldIncludeWindow(hwnd, excludedHwnd, shellWindow))
            {
                return true;
            }

            var title = GetWindowTitle(hwnd);
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            windows.Add(new AppWindowInfo
            {
                Hwnd = hwnd,
                Title = title,
                Icon = GetWindowIcon(hwnd),
                IsActive = GetComparableWindow(hwnd) == foregroundWindow
            });

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    public static void ActivateWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var originalHwnd = hwnd;
        hwnd = GetActionableWindow(hwnd);

        var foregroundWindow = GetForegroundWindow();
        var currentThreadId = GetCurrentThreadId();
        var foregroundThreadId = foregroundWindow == IntPtr.Zero
            ? 0
            : GetWindowThreadProcessId(foregroundWindow, out _);
        var targetThreadId = GetWindowThreadProcessId(hwnd, out _);

        if (IsWindowMinimized(hwnd))
        {
            SendMessage(hwnd, WmSyscommand, ScRestore, IntPtr.Zero);
            ShowWindowAsync(hwnd, SwRestore);
        }
        else
        {
            ShowWindowAsync(hwnd, SwShow);
        }

        if (foregroundThreadId != 0)
        {
            AttachThreadInput(currentThreadId, foregroundThreadId, true);
        }

        if (targetThreadId != 0 && targetThreadId != foregroundThreadId)
        {
            AttachThreadInput(currentThreadId, targetThreadId, true);
        }

        try
        {
            InteractionLogger.Log(
                $"ActivateWindow original=0x{originalHwnd.ToInt64():X} target=0x{hwnd.ToInt64():X} " +
                $"foreground=0x{foregroundWindow.ToInt64():X} minimized={IsWindowMinimized(hwnd)}");
            BringWindowToTop(hwnd);
            SetForegroundWindow(hwnd);
            SetFocus(hwnd);
            SetActiveWindow(hwnd);
            var finalForeground = GetForegroundWindow();
            InteractionLogger.Log(
                $"ActivateWindow result target=0x{hwnd.ToInt64():X} finalForeground=0x{finalForeground.ToInt64():X} " +
                $"matched={GetComparableWindow(finalForeground) == hwnd}");
        }
        finally
        {
            if (targetThreadId != 0 && targetThreadId != foregroundThreadId)
            {
                AttachThreadInput(currentThreadId, targetThreadId, false);
            }

            if (foregroundThreadId != 0)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }

    public static void MinimizeWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var originalHwnd = hwnd;
        hwnd = GetActionableWindow(hwnd);
        InteractionLogger.Log(
            $"MinimizeWindow original=0x{originalHwnd.ToInt64():X} target=0x{hwnd.ToInt64():X} minimizedBefore={IsWindowMinimized(hwnd)}");
        SendMessage(hwnd, WmSyscommand, ScMinimize, IntPtr.Zero);
        ShowWindowAsync(hwnd, SwMinimize);
        InteractionLogger.Log(
            $"MinimizeWindow result target=0x{hwnd.ToInt64():X} minimizedAfter={IsWindowMinimized(hwnd)}");
    }

    public static bool IsForegroundWindow(IntPtr hwnd)
    {
        return hwnd != IntPtr.Zero && GetComparableWindow(hwnd) == GetComparableWindow(GetForegroundWindow());
    }

    public static WindowDebugInfo GetWindowDebugInfo(IntPtr hwnd)
    {
        var comparableHandle = GetComparableWindow(hwnd);
        var actionHandle = GetActionableWindow(hwnd);
        var foregroundHandle = GetForegroundWindow();
        var foregroundComparableHandle = GetComparableWindow(foregroundHandle);

        return new WindowDebugInfo(
            hwnd,
            comparableHandle,
            actionHandle,
            foregroundHandle,
            foregroundComparableHandle,
            hwnd != IntPtr.Zero && IsWindowMinimized(actionHandle),
            hwnd == IntPtr.Zero ? string.Empty : GetWindowTitle(hwnd));
    }

    private static IntPtr GetComparableWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var rootOwner = GetAncestor(hwnd, GaRootOwner);
        if (rootOwner != IntPtr.Zero)
        {
            return rootOwner;
        }

        var root = GetAncestor(hwnd, GaRoot);
        return root != IntPtr.Zero ? root : hwnd;
    }

    private static IntPtr GetActionableWindow(IntPtr hwnd)
    {
        var comparable = GetComparableWindow(hwnd);
        if (comparable == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var popup = GetLastActivePopup(comparable);
        if (popup != IntPtr.Zero && IsWindowVisible(popup))
        {
            return popup;
        }

        return comparable;
    }

    private static bool IsWindowMinimized(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        var placement = new WindowPlacement
        {
            length = Marshal.SizeOf<WindowPlacement>()
        };

        return GetWindowPlacement(hwnd, ref placement) && placement.showCmd == SwShowminimized;
    }

    private static bool ShouldIncludeWindow(IntPtr hwnd, IntPtr excludedHwnd, IntPtr shellWindow)
    {
        if (hwnd == IntPtr.Zero || hwnd == excludedHwnd || hwnd == shellWindow)
        {
            return false;
        }

        if (!IsWindowVisible(hwnd) || IsWindowCloaked(hwnd))
        {
            return false;
        }

        var exStyle = GetWindowLongPtr(hwnd, GwlExstyle).ToInt64();
        if ((exStyle & WsExToolwindow) != 0)
        {
            return false;
        }

        if (GetWindow(hwnd, GwOwner) != IntPtr.Zero && (exStyle & WsExAppwindow) == 0)
        {
            return false;
        }

        return GetWindowTextLength(hwnd) > 0;
    }

    private static bool IsWindowCloaked(IntPtr hwnd)
    {
        if (DwmGetWindowAttribute(hwnd, DwmaCloaked, out int cloaked, Marshal.SizeOf<int>()) != 0)
        {
            return false;
        }

        return cloaked != 0;
    }

    private static string GetWindowTitle(IntPtr hwnd)
    {
        var length = GetWindowTextLength(hwnd);
        var builder = new StringBuilder(length + 1);
        _ = GetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static ImageSource? GetWindowIcon(IntPtr hwnd)
    {
        var iconHandle = SendMessage(hwnd, WmGeticon, new IntPtr(IconBig), IntPtr.Zero);
        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = SendMessage(hwnd, WmGeticon, new IntPtr(IconSmall2), IntPtr.Zero);
        }

        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = SendMessage(hwnd, WmGeticon, new IntPtr(IconSmall), IntPtr.Zero);
        }

        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = GetClassLongPtr(hwnd, GclpHiconsm);
        }

        if (iconHandle == IntPtr.Zero)
        {
            iconHandle = GetClassLongPtr(hwnd, GclpHicon);
        }

        if (iconHandle == IntPtr.Zero)
        {
            return GetProcessIcon(hwnd);
        }

        return CreateImageSourceFromIcon(iconHandle);
    }

    private static ImageSource? GetProcessIcon(IntPtr hwnd)
    {
        _ = GetWindowThreadProcessId(hwnd, out var processId);
        if (processId == 0)
        {
            return null;
        }

        var processHandle = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (processHandle == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var capacity = 1024;
            var builder = new StringBuilder(capacity);
            if (!QueryFullProcessImageName(processHandle, 0, builder, ref capacity))
            {
                return null;
            }

            var filePath = builder.ToString();
            var fileInfo = new ShFileInfo();
            var result = SHGetFileInfo(
                filePath,
                0,
                out fileInfo,
                (uint)Marshal.SizeOf<ShFileInfo>(),
                ShgfiIcon | ShgfiLargeIcon);

            if (result == IntPtr.Zero || fileInfo.hIcon == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return CreateImageSourceFromIcon(fileInfo.hIcon);
            }
            finally
            {
                DestroyIcon(fileInfo.hIcon);
            }
        }
        finally
        {
            CloseHandle(processHandle);
        }
    }

    private static ImageSource? CreateImageSourceFromIcon(IntPtr iconHandle)
    {
        var copiedIcon = CopyIcon(iconHandle);
        if (copiedIcon == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(
                copiedIcon,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(PreferredIconSize, PreferredIconSize));

            source.Freeze();
            return source;
        }
        finally
        {
            DestroyIcon(copiedIcon);
        }
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPlacement
    {
        public int length;
        public int flags;
        public int showCmd;
        public Point ptMinPosition;
        public Point ptMaxPosition;
        public Rect rcNormalPosition;
    }

    private const int SwShowminimized = 2;

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetLastActivePopup(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, int uCmd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetFocus(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SetActiveWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowPlacement(IntPtr hWnd, ref WindowPlacement lpwndpl);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtr", SetLastError = true)]
    private static extern IntPtr GetClassLongPtr(IntPtr hWnd, IntPtr nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CopyIcon(IntPtr hIcon);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(
        IntPtr hProcess,
        int dwFlags,
        StringBuilder lpExeName,
        ref int lpdwSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        out ShFileInfo psfi,
        uint cbFileInfo,
        uint uFlags);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out int pvAttribute, int cbAttribute);
}
