using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TopTaskBar;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\TopTaskBar_47FD43FA_1BB8_4D54_B8BC_1457379E90C2";
    private Mutex? _singleInstanceMutex;

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        InteractionLogger.Log("AppStartup");
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            InteractionLogger.Log("DuplicateInstanceDetected shutdown=true");
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown(0);
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        InteractionLogger.Log($"AppExit code={e.ApplicationExitCode}");

        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        _singleInstanceMutex = null;

        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException("DispatcherUnhandledException", e.Exception);
        // Keep default crash behavior so we don't hide fatal errors.
        e.Handled = false;
    }

    private static void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            LogException($"AppDomainUnhandledException terminating={e.IsTerminating}", exception);
            return;
        }

        InteractionLogger.Log(
            $"AppDomainUnhandledException terminating={e.IsTerminating} nonExceptionObject=\"{e.ExceptionObject}\"");
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogException("UnobservedTaskException", e.Exception);
    }

    private static void LogException(string source, Exception exception)
    {
        InteractionLogger.Log(
            $"{source} type=\"{exception.GetType().FullName}\" message=\"{exception.Message}\" stack=\"{exception.StackTrace}\"");

        if (exception.InnerException is not null)
        {
            InteractionLogger.Log(
                $"{source}.Inner type=\"{exception.InnerException.GetType().FullName}\" message=\"{exception.InnerException.Message}\" stack=\"{exception.InnerException.StackTrace}\"");
        }
    }
}
