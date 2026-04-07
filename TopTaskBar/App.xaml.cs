using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TopTaskBar;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        InteractionLogger.Log("AppStartup");
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        InteractionLogger.Log($"AppExit code={e.ApplicationExitCode}");

        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

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

