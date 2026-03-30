using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _popupDismissTimer;
    private readonly TopTaskBarSettings _settings;
    private readonly List<IntPtr> _windowOrder = [];
    private AppBarHelper? _appBarHelper;
    private DateTime _displayedMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _selectedCalendarDate = DateTime.Today;
    private string _currentDateTimeText = DateTime.Now.ToString("MM-dd HH:mm");
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

        _refreshTimer.Tick += OnRefreshTimerTick;
        _popupDismissTimer.Tick += OnPopupDismissTimerTick;

        Loaded += OnLoaded;
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
        Activated += OnActivated;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AppWindowInfo> OpenWindows { get; }

    public ObservableCollection<CalendarDayItem> CalendarDays { get; } = [];

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
        if (CalendarPopup.IsOpen)
        {
            CalendarPopup.IsOpen = false;
            e.Handled = true;
            return;
        }

        SyncCalendarToSelectedDate();
        _ignoreOutsideClickUntilUtc = DateTime.UtcNow.AddMilliseconds(200);
        CalendarPopup.IsOpen = true;
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

    private void OnPopupDismissTimerTick(object? sender, EventArgs e)
    {
        if (!CalendarPopup.IsOpen)
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

        if (!isOverDateButton && !isOverCalendar)
        {
            CalendarPopup.IsOpen = false;
        }
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

    private void OnCalendarDayClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: CalendarDayItem dayItem })
        {
            return;
        }

        _selectedCalendarDate = dayItem.Date;
        _displayedMonth = new DateTime(dayItem.Date.Year, dayItem.Date.Month, 1);
        RebuildCalendarDays();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimerTick;
        _popupDismissTimer.Stop();
        _popupDismissTimer.Tick -= OnPopupDismissTimerTick;
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

    private void SyncCalendarToSelectedDate()
    {
        _displayedMonth = new DateTime(_selectedCalendarDate.Year, _selectedCalendarDate.Month, 1);
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
                IsToday = date.Date == DateTime.Today,
                IsSelected = date.Date == _selectedCalendarDate.Date
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
