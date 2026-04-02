using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace TopTaskBar;

internal sealed class OutlookClassicUnreadMonitor : IDisposable
{
    private const int OlFolderInbox = 6;
    private readonly DispatcherTimer _timer;
    private bool _hasUnreadMail;

    public OutlookClassicUnreadMonitor()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _timer.Tick += OnTick;
    }

    public event EventHandler? StateChanged;

    public bool HasUnreadMail => _hasUnreadMail;

    public void Start()
    {
        RefreshNow();
        _timer.Start();
    }

    public void RefreshNow()
    {
        var hasUnread = TryReadUnreadCount(out var unreadCount) && unreadCount > 0;
        if (_hasUnreadMail == hasUnread)
        {
            return;
        }

        _hasUnreadMail = hasUnread;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        RefreshNow();
    }

    private static bool TryReadUnreadCount(out int unreadCount)
    {
        unreadCount = 0;

        object? outlookApplication = null;
        object? outlookNamespace = null;
        object? inboxFolder = null;

        try
        {
            outlookApplication = TryGetActiveComObject("Outlook.Application");
            if (outlookApplication is null)
            {
                return false;
            }

            dynamic app = outlookApplication;
            outlookNamespace = app.Session;
            dynamic session = outlookNamespace;
            inboxFolder = session.GetDefaultFolder(OlFolderInbox);
            dynamic inbox = inboxFolder;
            unreadCount = (int)inbox.UnReadItemCount;
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            ReleaseComObject(inboxFolder);
            ReleaseComObject(outlookNamespace);
            ReleaseComObject(outlookApplication);
        }
    }

    private static object? TryGetActiveComObject(string progId)
    {
        if (CLSIDFromProgID(progId, out var clsid) != 0)
        {
            return null;
        }

        return GetActiveObject(ref clsid, IntPtr.Zero, out var obj) == 0 ? obj : null;
    }

    private static void ReleaseComObject(object? comObject)
    {
        if (comObject is null || !Marshal.IsComObject(comObject))
        {
            return;
        }

        try
        {
            Marshal.FinalReleaseComObject(comObject);
        }
        catch
        {
            // Ignore COM release failures during shutdown or Outlook restart.
        }
    }

    [DllImport("ole32.dll")]
    private static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string progId, out Guid clsid);

    [DllImport("oleaut32.dll")]
    private static extern int GetActiveObject(ref Guid rclsid, IntPtr reserved, [MarshalAs(UnmanagedType.IUnknown)] out object? ppunk);
}

