using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using Brush = System.Windows.Media.Brush;
using Button = System.Windows.Controls.Button;
using Point = System.Windows.Point;

namespace TopTaskBar;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const double MinWindowSlotWidth = 28;
    private const double MaxWindowSlotWidth = 160;
    private const double WindowSlotWidthStep = 6;
    private const double DefaultWindowSlotWidth = 42;
    private const int MaxRecentLauncherApps = 5;

    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _popupDismissTimer;
    private readonly DispatcherTimer _settingsReloadTimer;
    private readonly FileSystemWatcher _settingsWatcher;
    private TopTaskBarSettings _settings;
    private readonly List<IntPtr> _windowOrder = [];
    private readonly List<LauncherAppItem> _allLauncherApps = [];
    private AppBarHelper? _appBarHelper;
    private DateTime _displayedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private string _currentDateTimeText = DateTime.Now.ToString("MM-dd HH:mm");
    private string _searchText = string.Empty;
    private WindowsThemePalette _themePalette = WindowsThemeHelper.GetPalette();
    private string _windowCountLabel = "0 apps";
    private double _windowSlotWidth = DefaultWindowSlotWidth;
    private bool _wasLeftButtonDown;
    private bool _wasRightButtonDown;
    private DateTime _ignoreOutsideClickUntilUtc = DateTime.MinValue;

    public MainWindow()
    {
        InteractionLogger.Log($"Application started. LogPath={InteractionLogger.CurrentLogPath}");
        _settings = SettingsStore.Load();
        _windowSlotWidth = ClampWindowSlotWidth(_settings.WindowSlotWidth);

        InitializeComponent();
        DataContext = this;

        OpenWindows = new ObservableCollection<AppWindowInfo>();
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _popupDismissTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _settingsReloadTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _settingsWatcher = new FileSystemWatcher
        {
            Path = Path.GetDirectoryName(SettingsStore.SettingsPath) ?? ".",
            Filter = Path.GetFileName(SettingsStore.SettingsPath),
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime
        };

        _refreshTimer.Tick += OnRefreshTimerTick;
        _popupDismissTimer.Tick += OnPopupDismissTimerTick;
        _settingsReloadTimer.Tick += OnSettingsReloadTimerTick;
        _settingsWatcher.Changed += OnSettingsFileChanged;
        _settingsWatcher.Created += OnSettingsFileChanged;
        _settingsWatcher.Renamed += OnSettingsFileRenamed;
        _settingsWatcher.EnableRaisingEvents = true;

        Loaded += OnLoaded;
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
        Activated += OnActivated;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AppWindowInfo> OpenWindows { get; }

    public ObservableCollection<CalendarDayItem> CalendarDays { get; } = [];

    public ObservableCollection<LauncherAppItem> LauncherApps { get; } = [];

    public ObservableCollection<LauncherAppItem> RecentLauncherApps { get; } = [];

    public bool HasLauncherSearchResults => LauncherApps.Count > 0;

    public bool IsLauncherSearchActive => !string.IsNullOrWhiteSpace(SearchText);

    public bool HasRecentLauncherApps => RecentLauncherApps.Count > 0;

    public string SearchText
    {
        get => _searchText;
        set
        {
            var normalized = value ?? string.Empty;
            if (_searchText == normalized)
            {
                return;
            }

            _searchText = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsLauncherSearchActive));
            RefreshLauncherAppsView();
        }
    }

    public string WindowCountLabel
    {
        get => _windowCountLabel;
        private set
        {
            if (_windowCountLabel == value)
            {
                return;
            }

            _windowCountLabel = value;
            OnPropertyChanged();
        }
    }

    public double WindowSlotWidth
    {
        get => _windowSlotWidth;
        private set
        {
            if (Math.Abs(_windowSlotWidth - value) < 0.1)
            {
                return;
            }

            _windowSlotWidth = value;
            _settings.WindowSlotWidth = value;
            SettingsStore.Save(_settings);
            OnPropertyChanged();
            OnPropertyChanged(nameof(WindowSlotWidthLabel));
        }
    }

    public string WindowSlotWidthLabel => $"{WindowSlotWidth:0}px";

    public string CurrentDateTimeText
    {
        get => _currentDateTimeText;
        private set
        {
            if (_currentDateTimeText == value)
            {
                return;
            }

            _currentDateTimeText = value;
            OnPropertyChanged();
        }
    }

    public string DisplayMonthLabel => $"{_displayedMonth.Year}년 {_displayedMonth.Month}월";

    public Brush BarBackgroundBrush => _themePalette.BarBackgroundBrush;

    public Brush BarBorderBrush => _themePalette.BarBorderBrush;

    public Brush PrimaryForegroundBrush => _themePalette.PrimaryForegroundBrush;

    public Brush MutedForegroundBrush => _themePalette.MutedForegroundBrush;

    public Brush AccentBackgroundBrush => _themePalette.AccentBackgroundBrush;

    public Brush AccentBorderBrush => _themePalette.AccentBorderBrush;

    public Brush AccentForegroundBrush => _themePalette.AccentForegroundBrush;

    public Brush AppStripBackgroundBrush => _themePalette.AppStripBackgroundBrush;

    public Brush AppStripBorderBrush => _themePalette.AppStripBorderBrush;

    public Brush ChipBackgroundBrush => _themePalette.ChipBackgroundBrush;

    public Brush ChipBorderBrush => _themePalette.ChipBorderBrush;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyTheme();
        LoadLauncherApps();
        UpdateCurrentDateTime();
        RefreshOpenWindows();
        _refreshTimer.Start();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        _appBarHelper = new AppBarHelper(this);
        _appBarHelper.Attach(new WindowInteropHelper(this).Handle);
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        ApplyTheme();
        RefreshOpenWindows();
    }

    private void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        UpdateCurrentDateTime();
        RefreshOpenWindows();
    }

    private void RefreshOpenWindows()
    {
        var currentHandle = new WindowInteropHelper(this).Handle;
        var latestWindows = WindowCatalog.GetOpenWindows(currentHandle);
        var latestByHandle = latestWindows.ToDictionary(window => window.Hwnd);

        _windowOrder.RemoveAll(hwnd => !latestByHandle.ContainsKey(hwnd));

        foreach (var window in latestWindows)
        {
            if (!_windowOrder.Contains(window.Hwnd))
            {
                _windowOrder.Add(window.Hwnd);
            }
        }

        var windows = _windowOrder
            .Where(latestByHandle.ContainsKey)
            .Select(hwnd => latestByHandle[hwnd])
            .ToList();

        OpenWindows.Clear();
        foreach (var window in windows)
        {
            OpenWindows.Add(window);
        }

        WindowCountLabel = $"{OpenWindows.Count} apps";
    }

    private void OnWindowButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: AppWindowInfo windowInfo })
        {
            return;
        }

        var debugInfo = WindowCatalog.GetWindowDebugInfo(windowInfo.Hwnd);
        InteractionLogger.Log(
            $"ButtonClick title=\"{windowInfo.Title}\" original=0x{debugInfo.OriginalHandle.ToInt64():X} " +
            $"comparable=0x{debugInfo.ComparableHandle.ToInt64():X} action=0x{debugInfo.ActionHandle.ToInt64():X} windowInfo.IsActive={windowInfo.IsActive} " +
            $"foreground=0x{debugInfo.ForegroundHandle.ToInt64():X} foregroundComparable=0x{debugInfo.ForegroundComparableHandle.ToInt64():X} " +
            $"isMinimized={debugInfo.IsMinimized}");

        if (windowInfo.IsActive)
        {
            WindowCatalog.MinimizeWindow(windowInfo.Hwnd);
        }
        else
        {
            WindowCatalog.ActivateWindow(windowInfo.Hwnd);
        }

        RefreshOpenWindows();
    }

    private void OnWindowButtonPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button { DataContext: AppWindowInfo windowInfo })
        {
            InteractionLogger.Log("RunningAppRightDown without AppWindowInfo context");
            return;
        }

        InteractionLogger.Log(
            $"RunningAppRightDown title=\"{windowInfo.Title}\" hwnd=0x{windowInfo.Hwnd.ToInt64():X} " +
            $"launcherOpen={LauncherPopup.IsOpen} calendarOpen={CalendarPopup.IsOpen} handled={e.Handled}");
    }

    private void OnCloseButtonClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.ContextMenu is null)
        {
            return;
        }

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.IsOpen = true;
    }

    private void OnDateTimeButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CloseLauncherPopup();

        if (CalendarPopup.IsOpen)
        {
            CalendarPopup.IsOpen = false;
            e.Handled = true;
            return;
        }

        SyncCalendarToToday();
        _ignoreOutsideClickUntilUtc = DateTime.UtcNow.AddMilliseconds(200);
        CalendarPopup.IsOpen = true;
        e.Handled = true;
    }

    private void OnLauncherButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CloseCalendarPopup();

        if (LauncherPopup.IsOpen)
        {
            LauncherPopup.IsOpen = false;
            e.Handled = true;
            return;
        }

        _ignoreOutsideClickUntilUtc = DateTime.UtcNow.AddMilliseconds(200);
        LauncherPopup.IsOpen = true;
        e.Handled = true;
    }

    private void OnCalendarPopupOpened(object sender, EventArgs e)
    {
        _wasLeftButtonDown = IsMouseButtonDown(VkLbutton);
        _wasRightButtonDown = IsMouseButtonDown(VkRbutton);
        _popupDismissTimer.Start();
    }

    private void OnCalendarPopupClosed(object sender, EventArgs e)
    {
        _popupDismissTimer.Stop();
    }

    private void OnLauncherPopupOpened(object sender, EventArgs e)
    {
        _appBarHelper?.SetInteractiveMode(true);
        _wasLeftButtonDown = IsMouseButtonDown(VkLbutton);
        _wasRightButtonDown = IsMouseButtonDown(VkRbutton);
        _popupDismissTimer.Start();
        Dispatcher.BeginInvoke(new Action(() =>
        {
            Activate();
            LauncherSearchBox.Focus();
            Keyboard.Focus(LauncherSearchBox);
        }), DispatcherPriority.Input);
    }

    private void OnLauncherPopupClosed(object sender, EventArgs e)
    {
        _appBarHelper?.SetInteractiveMode(false);
        SearchText = string.Empty;

        if (!CalendarPopup.IsOpen)
        {
            _popupDismissTimer.Stop();
        }
    }

    private void OnPopupDismissTimerTick(object? sender, EventArgs e)
    {
        if (!CalendarPopup.IsOpen && !LauncherPopup.IsOpen)
        {
            _popupDismissTimer.Stop();
            return;
        }

        var isLeftButtonDown = IsMouseButtonDown(VkLbutton);
        var isRightButtonDown = IsMouseButtonDown(VkRbutton);
        var clickedThisTick = (!_wasLeftButtonDown && isLeftButtonDown) || (!_wasRightButtonDown && isRightButtonDown);

        _wasLeftButtonDown = isLeftButtonDown;
        _wasRightButtonDown = isRightButtonDown;

        if (DateTime.UtcNow < _ignoreOutsideClickUntilUtc)
        {
            return;
        }

        if (!clickedThisTick || !GetCursorPos(out var cursorPosition))
        {
            return;
        }

        var isOverDateButton = IsScreenPointWithinElement(DateTimeButton, cursorPosition);
        var isOverCalendar = IsScreenPointWithinElement(CalendarPopupRoot, cursorPosition);
        var isOverLauncherButton = IsScreenPointWithinElement(LauncherButton, cursorPosition);
        var isOverLauncher = IsScreenPointWithinElement(LauncherPopupRoot, cursorPosition);

        if (CalendarPopup.IsOpen && !isOverDateButton && !isOverCalendar)
        {
            CalendarPopup.IsOpen = false;
        }

        if (LauncherPopup.IsOpen && !isOverLauncherButton && !isOverLauncher)
        {
            LauncherPopup.IsOpen = false;
        }
    }

    private void OnRunningAppMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu { PlacementTarget: Button { DataContext: AppWindowInfo windowInfo } } contextMenu)
        {
            InteractionLogger.Log("RunningAppMenuOpened without AppWindowInfo placement target");
            return;
        }

        contextMenu.DataContext = windowInfo;

        InteractionLogger.Log(
            $"RunningAppMenuOpened title=\"{windowInfo.Title}\" hwnd=0x{windowInfo.Hwnd.ToInt64():X}");
    }

    private void OnRunningAppMenuClosed(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu contextMenu)
        {
            InteractionLogger.Log("RunningAppMenuClosed without AppWindowInfo placement target");
            return;
        }

        var windowInfo = contextMenu.DataContext as AppWindowInfo;
        contextMenu.DataContext = null;

        if (windowInfo is null)
        {
            InteractionLogger.Log("RunningAppMenuClosed without preserved AppWindowInfo context");
            return;
        }

        InteractionLogger.Log(
            $"RunningAppMenuClosed title=\"{windowInfo.Title}\" hwnd=0x{windowInfo.Hwnd.ToInt64():X}");
    }

    private void OnDecreaseWidthClick(object sender, RoutedEventArgs e)
    {
        WindowSlotWidth = ClampWindowSlotWidth(WindowSlotWidth - WindowSlotWidthStep);
    }

    private void OnIncreaseWidthClick(object sender, RoutedEventArgs e)
    {
        WindowSlotWidth = ClampWindowSlotWidth(WindowSlotWidth + WindowSlotWidthStep);
    }

    private void OnResetWidthClick(object sender, RoutedEventArgs e)
    {
        WindowSlotWidth = DefaultWindowSlotWidth;
    }

    private void OnRefreshWindowsClick(object sender, RoutedEventArgs e)
    {
        RefreshOpenWindows();
    }

    private void OnAddRunningAppToLauncherClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: AppWindowInfo windowInfo })
        {
            InteractionLogger.Log("AddRunningAppToLauncherClick missing AppWindowInfo context");
            return;
        }

        InteractionLogger.Log(
            $"AddRunningAppToLauncherClick title=\"{windowInfo.Title}\" hwnd=0x{windowInfo.Hwnd.ToInt64():X}");

        if (!WindowCatalog.TryGetExecutablePath(windowInfo.Hwnd, out var executablePath))
        {
            InteractionLogger.Log(
                $"AddRunningAppToLauncherPathFailed title=\"{windowInfo.Title}\" hwnd=0x{windowInfo.Hwnd.ToInt64():X}");
            ShowOwnedMessageBox(
                "이 앱의 실행 경로를 가져오지 못했습니다.\n\n일반 데스크톱 앱이 아닌 경우 지원되지 않을 수 있습니다.",
                "TopTaskBar",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        InteractionLogger.Log(
            $"AddRunningAppToLauncherPathResolved title=\"{windowInfo.Title}\" hwnd=0x{windowInfo.Hwnd.ToInt64():X} path=\"{executablePath}\"");

        var appName = GetLauncherDisplayName(windowInfo.Title, executablePath);
        if (!TryAddPinnedApp(appName, executablePath, out var message))
        {
            InteractionLogger.Log(
                $"AddRunningAppToLauncherSaveFailed title=\"{windowInfo.Title}\" hwnd=0x{windowInfo.Hwnd.ToInt64():X} " +
                $"path=\"{executablePath}\" appName=\"{appName}\" message=\"{message}\"");
            ShowOwnedMessageBox(message, "TopTaskBar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        InteractionLogger.Log(
            $"AddRunningAppToLauncherSaved title=\"{windowInfo.Title}\" hwnd=0x{windowInfo.Hwnd.ToInt64():X} " +
            $"path=\"{executablePath}\" appName=\"{appName}\" message=\"{message}\"");

        ShowOwnedMessageBox(
            message,
            "TopTaskBar",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnLauncherAppClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: LauncherAppItem app })
        {
            return;
        }

        var targetType = GetLauncherTargetType(app.Path);
        if (!TryValidateLauncherTarget(app.Path, targetType, out var validationMessage))
        {
            InteractionLogger.Log(
                $"LauncherAppExecuteRejected name=\"{app.Name}\" type={targetType} path=\"{app.Path}\" message=\"{validationMessage}\"");
            ShowOwnedMessageBox(validationMessage, "TopTaskBar", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = app.Path,
                UseShellExecute = true
            };

            if (targetType == LauncherTargetType.Executable)
            {
                startInfo.Arguments = app.Arguments;

                if (!string.IsNullOrWhiteSpace(app.WorkingDirectory))
                {
                    startInfo.WorkingDirectory = app.WorkingDirectory;
                }
            }
            else if (targetType == LauncherTargetType.Directory && !string.IsNullOrWhiteSpace(app.WorkingDirectory))
            {
                startInfo.WorkingDirectory = app.WorkingDirectory;
            }

            InteractionLogger.Log(
                $"LauncherAppExecute name=\"{app.Name}\" type={targetType} path=\"{app.Path}\"");

            _ = Process.Start(startInfo);
            RegisterRecentLauncherApp(app);
            CloseLauncherPopup();
        }
        catch (Exception)
        {
            ShowOwnedMessageBox(GetLaunchFailureMessage(targetType), "TopTaskBar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnAddLauncherAppClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "앱 또는 바로가기 선택",
            Filter = "앱 및 바로가기 (*.exe;*.lnk)|*.exe;*.lnk|실행 파일 (*.exe)|*.exe|바로가기 (*.lnk)|*.lnk",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var selectedPath = dialog.FileName;
        if (!TryAddPinnedApp(Path.GetFileNameWithoutExtension(selectedPath), selectedPath, out var message))
        {
            ShowOwnedMessageBox(message, "TopTaskBar", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ShowOwnedMessageBox(message, "TopTaskBar", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnEditLauncherSettingsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!File.Exists(SettingsStore.SettingsPath))
            {
                SettingsStore.Save(_settings);
            }

            _ = Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = $"\"{SettingsStore.SettingsPath}\"",
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
            ShowOwnedMessageBox("settings.json 파일을 메모장으로 열지 못했습니다.", "TopTaskBar", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnPreviousMonthClick(object sender, RoutedEventArgs e)
    {
        _displayedMonth = _displayedMonth.AddMonths(-1);
        RebuildCalendarDays();
    }

    private void OnNextMonthClick(object sender, RoutedEventArgs e)
    {
        _displayedMonth = _displayedMonth.AddMonths(1);
        RebuildCalendarDays();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimerTick;
        _popupDismissTimer.Stop();
        _popupDismissTimer.Tick -= OnPopupDismissTimerTick;
        _settingsReloadTimer.Stop();
        _settingsReloadTimer.Tick -= OnSettingsReloadTimerTick;
        _settingsWatcher.EnableRaisingEvents = false;
        _settingsWatcher.Changed -= OnSettingsFileChanged;
        _settingsWatcher.Created -= OnSettingsFileChanged;
        _settingsWatcher.Renamed -= OnSettingsFileRenamed;
        _settingsWatcher.Dispose();
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _appBarHelper?.Dispose();
        _appBarHelper = null;
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        Dispatcher.Invoke(ApplyTheme);
    }

    private void ApplyTheme()
    {
        _themePalette = WindowsThemeHelper.GetPalette();
        OnPropertyChanged(nameof(BarBackgroundBrush));
        OnPropertyChanged(nameof(BarBorderBrush));
        OnPropertyChanged(nameof(PrimaryForegroundBrush));
        OnPropertyChanged(nameof(MutedForegroundBrush));
        OnPropertyChanged(nameof(AccentBackgroundBrush));
        OnPropertyChanged(nameof(AccentBorderBrush));
        OnPropertyChanged(nameof(AccentForegroundBrush));
        OnPropertyChanged(nameof(AppStripBackgroundBrush));
        OnPropertyChanged(nameof(AppStripBorderBrush));
        OnPropertyChanged(nameof(ChipBackgroundBrush));
        OnPropertyChanged(nameof(ChipBorderBrush));
    }

    private void LoadLauncherApps()
    {
        _allLauncherApps.Clear();

        foreach (var app in _settings.PinnedApps)
        {
            var targetType = GetLauncherTargetType(app.Path);
            _allLauncherApps.Add(new LauncherAppItem
            {
                Name = app.Name,
                Path = app.Path,
                Arguments = app.Arguments,
                WorkingDirectory = app.WorkingDirectory,
                Icon = AppIconHelper.LoadIcon(app.Path),
                FallbackGlyph = GetLauncherFallbackGlyph(targetType)
            });
        }

        RefreshLauncherAppsView();
        SyncRecentLauncherApps();
    }

    private void RefreshLauncherAppsView()
    {
        LauncherApps.Clear();

        var query = SearchText.Trim();
        var filteredApps = string.IsNullOrWhiteSpace(query)
            ? _allLauncherApps
            : _allLauncherApps
                .Where(app => app.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

        foreach (var app in filteredApps)
        {
            LauncherApps.Add(app);
        }

        OnPropertyChanged(nameof(HasLauncherSearchResults));
    }

    private void RegisterRecentLauncherApp(LauncherAppItem app)
    {
        _settings.RecentLauncherPaths.RemoveAll(path =>
            string.Equals(path, app.Path, StringComparison.OrdinalIgnoreCase));
        _settings.RecentLauncherPaths.Insert(0, app.Path);

        while (_settings.RecentLauncherPaths.Count > MaxRecentLauncherApps)
        {
            _settings.RecentLauncherPaths.RemoveAt(_settings.RecentLauncherPaths.Count - 1);
        }

        SettingsStore.Save(_settings);
        SyncRecentLauncherApps();
    }

    private void SyncRecentLauncherApps()
    {
        RecentLauncherApps.Clear();

        foreach (var path in _settings.RecentLauncherPaths)
        {
            var app = _allLauncherApps.FirstOrDefault(item =>
                string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase));

            if (app is not null)
            {
                RecentLauncherApps.Add(app);
            }
        }

        while (RecentLauncherApps.Count > MaxRecentLauncherApps)
        {
            RecentLauncherApps.RemoveAt(RecentLauncherApps.Count - 1);
        }
        OnPropertyChanged(nameof(HasRecentLauncherApps));
    }

    private void SyncCalendarToToday()
    {
        _displayedMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        RebuildCalendarDays();
    }

    private void RebuildCalendarDays()
    {
        CalendarDays.Clear();

        var firstOfMonth = new DateTime(_displayedMonth.Year, _displayedMonth.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(_displayedMonth.Year, _displayedMonth.Month);
        var leadingDays = (int)firstOfMonth.DayOfWeek;
        var startDate = firstOfMonth.AddDays(-leadingDays);

        for (var index = 0; index < 42; index++)
        {
            var date = startDate.AddDays(index);
            CalendarDays.Add(new CalendarDayItem
            {
                Date = date,
                DayText = date.Day.ToString(),
                IsCurrentMonth = date.Month == _displayedMonth.Month && date.Year == _displayedMonth.Year,
                IsToday = date.Date == DateTime.Today
            });
        }

        OnPropertyChanged(nameof(DisplayMonthLabel));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static double ClampWindowSlotWidth(double value)
    {
        if (value < MinWindowSlotWidth)
        {
            return MinWindowSlotWidth;
        }

        if (value > MaxWindowSlotWidth)
        {
            return MaxWindowSlotWidth;
        }

        return value;
    }

    private void UpdateCurrentDateTime()
    {
        CurrentDateTimeText = DateTime.Now.ToString("MM-dd HH:mm");
    }

    private void OnSettingsFileChanged(object sender, FileSystemEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(RestartSettingsReloadTimer));
    }

    private void OnSettingsFileRenamed(object sender, RenamedEventArgs e)
    {
        Dispatcher.BeginInvoke(new Action(RestartSettingsReloadTimer));
    }

    private void RestartSettingsReloadTimer()
    {
        _settingsReloadTimer.Stop();
        _settingsReloadTimer.Start();
    }

    private void OnSettingsReloadTimerTick(object? sender, EventArgs e)
    {
        _settingsReloadTimer.Stop();
        ReloadSettingsFromDisk();
    }

    private void ReloadSettingsFromDisk()
    {
        var reloadedSettings = SettingsStore.Load();
        _settings = reloadedSettings;

        var reloadedWindowSlotWidth = ClampWindowSlotWidth(_settings.WindowSlotWidth);
        if (Math.Abs(_windowSlotWidth - reloadedWindowSlotWidth) >= 0.1)
        {
            _windowSlotWidth = reloadedWindowSlotWidth;
            OnPropertyChanged(nameof(WindowSlotWidth));
            OnPropertyChanged(nameof(WindowSlotWidthLabel));
        }

        LoadLauncherApps();
    }

    private void CloseCalendarPopup()
    {
        if (CalendarPopup.IsOpen)
        {
            CalendarPopup.IsOpen = false;
        }
    }

    private void CloseLauncherPopup()
    {
        if (LauncherPopup.IsOpen)
        {
            LauncherPopup.IsOpen = false;
        }
    }

    private static bool IsScreenPointWithinElement(FrameworkElement? element, NativePoint screenPoint)
    {
        if (element is null || !element.IsLoaded || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }

        var topLeft = element.PointToScreen(new Point(0, 0));
        var bounds = new Rect(topLeft.X, topLeft.Y, element.ActualWidth, element.ActualHeight);
        return bounds.Contains(new Point(screenPoint.X, screenPoint.Y));
    }

    private static bool IsMouseButtonDown(int virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    private static string GetLauncherDisplayName(string windowTitle, string executablePath)
    {
        if (!string.IsNullOrWhiteSpace(windowTitle))
        {
            var separators = new[] { " - ", " — ", " | " };
            foreach (var separator in separators)
            {
                var separatorIndex = windowTitle.LastIndexOf(separator, StringComparison.CurrentCulture);
                if (separatorIndex > 0)
                {
                    var candidate = windowTitle[(separatorIndex + separator.Length)..].Trim();
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return Path.GetFileNameWithoutExtension(executablePath);
    }

    private static LauncherTargetType GetLauncherTargetType(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return LauncherTargetType.Unknown;
        }

        if (Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return LauncherTargetType.Url;
        }

        if (Directory.Exists(path))
        {
            return LauncherTargetType.Directory;
        }

        var extension = Path.GetExtension(path);
        if (string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
        {
            return LauncherTargetType.Executable;
        }

        if (string.Equals(extension, ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return LauncherTargetType.Shortcut;
        }

        return LauncherTargetType.Unknown;
    }

    private static string GetLauncherFallbackGlyph(LauncherTargetType targetType)
    {
        return targetType switch
        {
            LauncherTargetType.Directory => "F",
            LauncherTargetType.Url => "W",
            LauncherTargetType.Shortcut => "L",
            _ => "A"
        };
    }

    private static string GetDefaultLauncherName(string path, LauncherTargetType targetType)
    {
        return targetType switch
        {
            LauncherTargetType.Directory => new DirectoryInfo(path).Name,
            LauncherTargetType.Url when Uri.TryCreate(path, UriKind.Absolute, out var uri) => uri.Host,
            _ => Path.GetFileNameWithoutExtension(path)
        };
    }

    private static bool TryValidateLauncherTarget(string path, LauncherTargetType targetType, out string message)
    {
        message = string.Empty;

        switch (targetType)
        {
            case LauncherTargetType.Executable:
                if (!File.Exists(path))
                {
                    message = "실행 파일을 찾을 수 없습니다.";
                    return false;
                }
                break;

            case LauncherTargetType.Shortcut:
                if (!File.Exists(path))
                {
                    message = "바로가기 파일을 찾을 수 없습니다.";
                    return false;
                }
                break;

            case LauncherTargetType.Directory:
                if (!Directory.Exists(path))
                {
                    message = "폴더를 찾을 수 없습니다.";
                    return false;
                }
                break;

            case LauncherTargetType.Url:
                if (!Uri.TryCreate(path, UriKind.Absolute, out var uri) ||
                    (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    message = "http 또는 https 형식의 올바른 URL이 아닙니다.";
                    return false;
                }
                break;

            default:
                message = "지원하지 않는 런처 항목입니다.";
                return false;
        }

        return true;
    }

    private static string GetLaunchFailureMessage(LauncherTargetType targetType)
    {
        return targetType switch
        {
            LauncherTargetType.Directory => "폴더를 열지 못했습니다.",
            LauncherTargetType.Url => "URL을 열지 못했습니다.",
            LauncherTargetType.Shortcut => "바로가기를 실행하지 못했습니다.",
            _ => "앱 실행에 실패했습니다."
        };
    }

    private MessageBoxResult ShowOwnedMessageBox(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
    {
        return MessageBox.Show(this, messageBoxText, caption, button, icon);
    }

    private bool TryAddPinnedApp(string appNameCandidate, string executablePath, out string message)
    {
        InteractionLogger.Log(
            $"TryAddPinnedAppStart candidate=\"{appNameCandidate}\" path=\"{executablePath}\"");
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            message = "실행 경로가 비어 있습니다.";
            InteractionLogger.Log($"TryAddPinnedAppReject reason=\"empty_path\" message=\"{message}\"");
            return false;
        }

        var targetType = GetLauncherTargetType(executablePath);
        if (targetType == LauncherTargetType.Unknown)
        {
            message = "현재는 .exe, .lnk, 폴더, http/https URL만 런처에 추가할 수 있습니다.";
            InteractionLogger.Log($"TryAddPinnedAppReject reason=\"unsupported_target\" path=\"{executablePath}\" message=\"{message}\"");
            return false;
        }

        if ((targetType == LauncherTargetType.Executable || targetType == LauncherTargetType.Shortcut) &&
            !File.Exists(executablePath))
        {
            message = "대상 파일을 찾을 수 없습니다.";
            InteractionLogger.Log($"TryAddPinnedAppReject reason=\"missing_file\" path=\"{executablePath}\" message=\"{message}\"");
            return false;
        }

        if (targetType == LauncherTargetType.Directory && !Directory.Exists(executablePath))
        {
            message = "대상 폴더를 찾을 수 없습니다.";
            InteractionLogger.Log($"TryAddPinnedAppReject reason=\"missing_directory\" path=\"{executablePath}\" message=\"{message}\"");
            return false;
        }

        if (_settings.PinnedApps.Any(app =>
                string.Equals(app.Path, executablePath, StringComparison.OrdinalIgnoreCase)))
        {
            message = "이미 런처에 추가된 앱입니다.";
            InteractionLogger.Log($"TryAddPinnedAppReject reason=\"duplicate\" path=\"{executablePath}\" message=\"{message}\"");
            return false;
        }

        var appName = string.IsNullOrWhiteSpace(appNameCandidate)
            ? GetDefaultLauncherName(executablePath, targetType)
            : appNameCandidate.Trim();
        var workingDirectory = targetType switch
        {
            LauncherTargetType.Executable => Path.GetDirectoryName(executablePath) ?? string.Empty,
            LauncherTargetType.Directory => executablePath,
            _ => string.Empty
        };

        _settings.PinnedApps.Add(new LauncherAppSetting
        {
            Name = appName,
            Path = executablePath,
            Arguments = string.Empty,
            WorkingDirectory = workingDirectory
        });

        _settings.PinnedApps = _settings.PinnedApps
            .OrderBy(app => app.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(app => app.Path, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        SettingsStore.Save(_settings);
        LoadLauncherApps();

        message = $"'{appName}' 앱을 런처에 추가했습니다.";
        InteractionLogger.Log($"TryAddPinnedAppSuccess appName=\"{appName}\" path=\"{executablePath}\"");
        return true;
    }

    private enum LauncherTargetType
    {
        Unknown,
        Executable,
        Shortcut,
        Directory,
        Url
    }

    private const int VkLbutton = 0x01;
    private const int VkRbutton = 0x02;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint lpPoint);
}
