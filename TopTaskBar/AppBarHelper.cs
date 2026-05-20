using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Win32;

namespace TopTaskBar;

internal sealed class AppBarHelper : IDisposable
{
    public const double BarHeightDip = 50;
    private const double DefaultDpi = 96;
    private const int WmUser = 0x0400;
    private const int CallbackMessageId = WmUser + 1;
    private const int WmMouseActivate = 0x0021;
    private const int WmSettingChange = 0x001A;
    private const int WmDisplayChange = 0x007E;
    private const int WmPowerBroadcast = 0x0218;
    private const int WmDpiChanged = 0x02E0;
    private const int PbtApmResumeSuspend = 0x0007;
    private const int PbtApmResumeAutomatic = 0x0012;
    private const int MaNoActivate = 3;
    private const int GwlExstyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private static readonly IntPtr HwndBottom = new(1);
    private static readonly IntPtr HwndTopmost = new(-1);
    private const uint SwpNosize = 0x0001;
    private const uint SwpNomove = 0x0002;
    private const uint SwpNoactivate = 0x0010;
    private static readonly IntPtr MonitorDefaultToNearest = new(2);

    private readonly Window _window;
    private readonly DispatcherTimer _appBarRefreshTimer;
    private HwndSource? _source;
    private bool _isRegistered;
    private bool _noActivateEnabled = true;
    private bool _isFullscreenAppActive;
    private int _pendingRefreshPasses;
    private int _pendingRefreshTotalPasses;
    private string _pendingRefreshReason = string.Empty;

    public AppBarHelper(Window window)
    {
        _window = window;
        _appBarRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _appBarRefreshTimer.Tick += OnAppBarRefreshTimerTick;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        SystemEvents.SessionSwitch += OnSessionSwitch;
    }

    public void Attach(IntPtr hwnd)
    {
        _source = HwndSource.FromHwnd(hwnd);
        if (_source is null)
        {
            throw new InvalidOperationException("Failed to access the window source for AppBar registration.");
        }

        _source.AddHook(WndProc);
        ApplyNoActivateStyle(hwnd, enabled: true);
        RegisterAppBar(hwnd);
        UpdateAppBarBounds(hwnd, "Attach", logMetrics: true);
        ScheduleAppBarRefresh("AttachDelayed");
    }

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        _appBarRefreshTimer.Stop();
        _appBarRefreshTimer.Tick -= OnAppBarRefreshTimerTick;

        if (_source is not null)
        {
            _source.RemoveHook(WndProc);
        }

        if (_isRegistered && _source is not null)
        {
            var data = CreateAppBarData(_source.Handle);
            SHAppBarMessage(AppBarMessage.Remove, ref data);
            _isRegistered = false;
        }
    }

    public void SetInteractiveMode(bool isInteractive)
    {
        if (_source is null)
        {
            return;
        }

        _noActivateEnabled = !isInteractive;
        ApplyNoActivateStyle(_source.Handle, _noActivateEnabled);
    }

    public void ScheduleRefresh(string reason, int passes = 3)
    {
        ScheduleAppBarRefresh(reason, passes);
    }

    private void RegisterAppBar(IntPtr hwnd)
    {
        var data = CreateAppBarData(hwnd);
        data.uCallbackMessage = CallbackMessageId;
        SHAppBarMessage(AppBarMessage.New, ref data);
        _isRegistered = true;
    }

    private void UpdateAppBarBounds(IntPtr hwnd, string reason = "Update", bool logMetrics = false)
    {
        if (_source?.CompositionTarget is null)
        {
            return;
        }

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo
        {
            cbSize = Marshal.SizeOf<MonitorInfo>()
        };

        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var fallbackTransformToDevice = _source.CompositionTarget.TransformToDevice;
        var scale = GetWindowDpiScale(hwnd, fallbackTransformToDevice.M11, fallbackTransformToDevice.M22);

        var monitorLeftPx = monitorInfo.rcMonitor.left;
        var monitorTopPx = monitorInfo.rcMonitor.top;
        var monitorRightPx = monitorInfo.rcMonitor.right;
        var monitorBottomPx = monitorInfo.rcMonitor.bottom;
        var barHeightPx = Math.Max(1, (int)Math.Round(BarHeightDip * scale.Y));

        var data = CreateAppBarData(hwnd);
        data.uEdge = AppBarEdge.Top;
        data.rc.left = monitorLeftPx;
        data.rc.top = monitorTopPx;
        data.rc.right = monitorRightPx;
        data.rc.bottom = monitorTopPx + barHeightPx;

        SHAppBarMessage(AppBarMessage.QueryPos, ref data);

        data.rc.left = monitorLeftPx;
        data.rc.top = monitorTopPx;
        data.rc.right = monitorRightPx;
        data.rc.bottom = monitorTopPx + barHeightPx;

        SHAppBarMessage(AppBarMessage.SetPos, ref data);

        _window.Left = data.rc.left / scale.X;
        _window.Top = data.rc.top / scale.Y;
        _window.Width = (data.rc.right - data.rc.left) / scale.X;
        _window.Height = (data.rc.bottom - data.rc.top) / scale.Y;

        if (logMetrics)
        {
            InteractionLogger.Log(
                $"AppBarBounds reason=\"{reason}\" monitorPx=({monitorLeftPx},{monitorTopPx},{monitorRightPx},{monitorBottomPx}) " +
                $"workPx=({monitorInfo.rcWork.left},{monitorInfo.rcWork.top},{monitorInfo.rcWork.right},{monitorInfo.rcWork.bottom}) " +
                $"scale=({scale.X:0.###},{scale.Y:0.###}) fallbackScale=({fallbackTransformToDevice.M11:0.###},{fallbackTransformToDevice.M22:0.###}) barHeightPx={barHeightPx} " +
                $"reservedPx=({data.rc.left},{data.rc.top},{data.rc.right},{data.rc.bottom}) " +
                $"windowDip=({_window.Left:0.##},{_window.Top:0.##},{_window.Width:0.##},{_window.Height:0.##})");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmMouseActivate)
        {
            if (_noActivateEnabled)
            {
                handled = true;
                return new IntPtr(MaNoActivate);
            }
        }

        if (msg == WmDpiChanged)
        {
            UpdateAppBarBounds(hwnd, "WmDpiChanged", logMetrics: true);
            ScheduleAppBarRefresh("WmDpiChangedDelayed", passes: 4);
        }

        if (msg == WmDisplayChange)
        {
            ScheduleAppBarRefresh("WmDisplayChange");
        }

        if (msg == WmSettingChange)
        {
            ScheduleAppBarRefresh("WmSettingChange");
        }

        if (msg == WmPowerBroadcast &&
            (wParam.ToInt32() == PbtApmResumeAutomatic || wParam.ToInt32() == PbtApmResumeSuspend))
        {
            ScheduleAppBarRefresh($"WmPowerBroadcast:{wParam.ToInt32()}");
        }

        if (msg == CallbackMessageId && wParam.ToInt32() == (int)AppBarNotification.PosChanged)
        {
            UpdateAppBarBounds(hwnd);
        }
        else if (msg == CallbackMessageId && wParam.ToInt32() == (int)AppBarNotification.FullscreenApp)
        {
            var fullscreenActive = lParam != IntPtr.Zero;
            if (_isFullscreenAppActive != fullscreenActive)
            {
                _isFullscreenAppActive = fullscreenActive;
                ApplyAppBarZOrder(hwnd, fullscreenActive);
            }
        }

        return IntPtr.Zero;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        ScheduleAppBarRefresh("SystemEvents.DisplaySettingsChanged", passes: 4);
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            ScheduleAppBarRefresh("SystemEvents.PowerModeChanged.Resume", passes: 4);
        }
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        ScheduleAppBarRefresh($"SystemEvents.SessionSwitch.{e.Reason}", passes: 6);
    }

    private void ScheduleAppBarRefresh(string reason, int passes = 2)
    {
        if (!_window.Dispatcher.CheckAccess())
        {
            _window.Dispatcher.BeginInvoke(new Action(() => ScheduleAppBarRefresh(reason, passes)));
            return;
        }

        if (_source is null)
        {
            return;
        }

        _pendingRefreshReason = reason;
        var requestedPasses = Math.Max(1, passes);
        if (requestedPasses >= _pendingRefreshPasses)
        {
            _pendingRefreshTotalPasses = requestedPasses;
        }

        _pendingRefreshPasses = Math.Max(_pendingRefreshPasses, requestedPasses);

        InteractionLogger.Log(
            $"AppBarRefreshScheduled reason=\"{reason}\" passes={_pendingRefreshPasses}");

        _appBarRefreshTimer.Stop();
        _appBarRefreshTimer.Start();
    }

    private void OnAppBarRefreshTimerTick(object? sender, EventArgs e)
    {
        if (_source is null)
        {
            _appBarRefreshTimer.Stop();
            return;
        }

        var hwnd = _source.Handle;
        var passNumber = Math.Max(1, _pendingRefreshTotalPasses - _pendingRefreshPasses + 1);

        UpdateAppBarBounds(
            hwnd,
            $"{_pendingRefreshReason}:DelayedPass{passNumber}",
            logMetrics: true);

        _pendingRefreshPasses--;
        if (_pendingRefreshPasses <= 0)
        {
            _appBarRefreshTimer.Stop();
            _pendingRefreshTotalPasses = 0;
            _pendingRefreshReason = string.Empty;
            return;
        }

        _appBarRefreshTimer.Start();
    }

    private static void ApplyAppBarZOrder(IntPtr hwnd, bool fullscreenAppActive)
    {
        var zOrder = fullscreenAppActive ? HwndBottom : HwndTopmost;
        SetWindowPos(hwnd, zOrder, 0, 0, 0, 0, SwpNomove | SwpNosize | SwpNoactivate);
    }

    private static void ApplyNoActivateStyle(IntPtr hwnd, bool enabled)
    {
        var exStyle = GetWindowLong(hwnd, GwlExstyle);
        var updatedStyle = enabled
            ? exStyle | WsExNoActivate
            : exStyle & ~WsExNoActivate;
        SetWindowLong(hwnd, GwlExstyle, updatedStyle);
    }

    private static (double X, double Y) GetWindowDpiScale(IntPtr hwnd, double fallbackX, double fallbackY)
    {
        try
        {
            var dpi = GetDpiForWindow(hwnd);
            if (dpi > 0)
            {
                var scale = dpi / DefaultDpi;
                return (scale, scale);
            }
        }
        catch (EntryPointNotFoundException)
        {
        }

        return (
            fallbackX > 0 ? fallbackX : 1,
            fallbackY > 0 ? fallbackY : 1);
    }

    private static AppBarData CreateAppBarData(IntPtr hwnd)
    {
        return new AppBarData
        {
            cbSize = Marshal.SizeOf<AppBarData>(),
            hWnd = hwnd
        };
    }

    private enum AppBarMessage
    {
        New = 0x00000000,
        Remove = 0x00000001,
        QueryPos = 0x00000002,
        SetPos = 0x00000003
    }

    private enum AppBarNotification
    {
        PosChanged = 0x00000001,
        FullscreenApp = 0x00000002
    }

    private enum AppBarEdge : uint
    {
        Left = 0,
        Top = 1,
        Right = 2,
        Bottom = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct AppBarData
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public AppBarEdge uEdge;
        public Rect rc;
        public IntPtr lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int cbSize;
        public Rect rcMonitor;
        public Rect rcWork;
        public int dwFlags;
    }

    [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern uint SHAppBarMessage(AppBarMessage dwMessage, ref AppBarData pData);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, IntPtr dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
}
