using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TopTaskBar;

internal sealed class AppBarHelper : IDisposable
{
    private const int BarHeight = 50;
    private const int WmUser = 0x0400;
    private const int CallbackMessageId = WmUser + 1;
    private const int WmMouseActivate = 0x0021;
    private const int MaNoActivate = 3;
    private const int GwlExstyle = -20;
    private const int WsExNoActivate = 0x08000000;

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
        var screenWidth = (int)SystemParameters.PrimaryScreenWidth;

        var data = CreateAppBarData(hwnd);
        data.uEdge = AppBarEdge.Top;
        data.rc.left = 0;
        data.rc.top = 0;
        data.rc.right = screenWidth;
        data.rc.bottom = BarHeight;

        SHAppBarMessage(AppBarMessage.QueryPos, ref data);

        data.rc.left = 0;
        data.rc.top = 0;
        data.rc.right = screenWidth;
        data.rc.bottom = BarHeight;

        SHAppBarMessage(AppBarMessage.SetPos, ref data);

        _window.Left = data.rc.left;
        _window.Top = data.rc.top;
        _window.Width = data.rc.right - data.rc.left;
        _window.Height = data.rc.bottom - data.rc.top;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmMouseActivate)
        {
            handled = true;
            return new IntPtr(MaNoActivate);
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

    [DllImport("shell32.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern uint SHAppBarMessage(AppBarMessage dwMessage, ref AppBarData pData);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
