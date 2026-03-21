using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TopTaskBar;

internal sealed class AppBarHelper : IDisposable
{
    public const double BarHeightDip = 50;
    private const int WmUser = 0x0400;
    private const int CallbackMessageId = WmUser + 1;
    private const int WmMouseActivate = 0x0021;
    private const int WmDpiChanged = 0x02E0;
    private const int MaNoActivate = 3;
    private const int GwlExstyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private static readonly IntPtr MonitorDefaultToNearest = new(2);

    private readonly Window _window;
    private HwndSource? _source;
    private bool _isRegistered;

    public AppBarHelper(Window window)
    {
        _window = window;
    }

    public void Attach(IntPtr hwnd)
    {
        _source = HwndSource.FromHwnd(hwnd);
        if (_source is null)
        {
            throw new InvalidOperationException("Failed to access the window source for AppBar registration.");
        }

        _source.AddHook(WndProc);
        ApplyNoActivateStyle(hwnd);
        RegisterAppBar(hwnd);
        UpdateAppBarBounds(hwnd);
    }

    public void Dispose()
    {
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

    private void RegisterAppBar(IntPtr hwnd)
    {
        var data = CreateAppBarData(hwnd);
        data.uCallbackMessage = CallbackMessageId;
        SHAppBarMessage(AppBarMessage.New, ref data);
        _isRegistered = true;
    }

    private void UpdateAppBarBounds(IntPtr hwnd)
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

        var transformToDevice = _source.CompositionTarget.TransformToDevice;
        var transformFromDevice = _source.CompositionTarget.TransformFromDevice;

        var monitorLeftPx = monitorInfo.rcMonitor.left;
        var monitorTopPx = monitorInfo.rcMonitor.top;
        var monitorRightPx = monitorInfo.rcMonitor.right;
        var barHeightPx = Math.Max(1, (int)Math.Round(BarHeightDip * transformToDevice.M22));

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

        var topLeftDip = transformFromDevice.Transform(new Point(data.rc.left, data.rc.top));
        var bottomRightDip = transformFromDevice.Transform(new Point(data.rc.right, data.rc.bottom));

        _window.Left = topLeftDip.X;
        _window.Top = topLeftDip.Y;
        _window.Width = bottomRightDip.X - topLeftDip.X;
        _window.Height = bottomRightDip.Y - topLeftDip.Y;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmMouseActivate)
        {
            handled = true;
            return new IntPtr(MaNoActivate);
        }

        if (msg == WmDpiChanged)
        {
            UpdateAppBarBounds(hwnd);
        }

        if (msg == CallbackMessageId && wParam.ToInt32() == (int)AppBarNotification.PosChanged)
        {
            UpdateAppBarBounds(hwnd);
        }

        return IntPtr.Zero;
    }

    private static void ApplyNoActivateStyle(IntPtr hwnd)
    {
        var exStyle = GetWindowLong(hwnd, GwlExstyle);
        SetWindowLong(hwnd, GwlExstyle, exStyle | WsExNoActivate);
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
        PosChanged = 0x00000001
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
}
