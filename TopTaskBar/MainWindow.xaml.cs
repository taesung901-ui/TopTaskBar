using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace TopTaskBar;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    private const double MinWindowSlotWidth = 28;
    private const double MaxWindowSlotWidth = 160;
    private const double WindowSlotWidthStep = 6;
    private const double DefaultWindowSlotWidth = 42;

    private readonly DispatcherTimer _refreshTimer;
    private readonly TopTaskBarSettings _settings;
    private readonly List<IntPtr> _windowOrder = [];
    private AppBarHelper? _appBarHelper;
    private string _currentDateTimeText = DateTime.Now.ToString("MM-dd HH:mm");
    private WindowsThemePalette _themePalette = WindowsThemeHelper.GetPalette();
    private string _windowCountLabel = "0 apps";
    private double _windowSlotWidth = DefaultWindowSlotWidth;

    public MainWindow()
    {
        _settings = SettingsStore.Load();
        _windowSlotWidth = ClampWindowSlotWidth(_settings.WindowSlotWidth);

        InitializeComponent();
        DataContext = this;

        OpenWindows = new ObservableCollection<AppWindowInfo>();
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };

        _refreshTimer.Tick += OnRefreshTimerTick;

        Loaded += OnLoaded;
        SourceInitialized += OnSourceInitialized;
        Closed += OnClosed;
        Activated += OnActivated;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AppWindowInfo> OpenWindows { get; }

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

    private void OnClosed(object? sender, EventArgs e)
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimerTick;
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
}
