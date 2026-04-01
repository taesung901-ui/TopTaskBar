using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TopTaskBar;

public sealed class TimerToolState : INotifyPropertyChanged
{
    private TimeSpan _selectedDuration = TimeSpan.FromMinutes(5);
    private TimeSpan _remainingTime = TimeSpan.FromMinutes(5);
    private bool _isRunning;
    private bool _isCompleted;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TimeSpan SelectedDuration
    {
        get => _selectedDuration;
        set
        {
            if (_selectedDuration == value)
            {
                return;
            }

            _selectedDuration = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedDurationText));
            OnPropertyChanged(nameof(CanReset));
        }
    }

    public TimeSpan RemainingTime
    {
        get => _remainingTime;
        set
        {
            if (_remainingTime == value)
            {
                return;
            }

            _remainingTime = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanReset));
        }
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning == value)
            {
                return;
            }

            _isRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanStart));
            OnPropertyChanged(nameof(CanPause));
            OnPropertyChanged(nameof(CanReset));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public bool IsCompleted
    {
        get => _isCompleted;
        set
        {
            if (_isCompleted == value)
            {
                return;
            }

            _isCompleted = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanReset));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string RemainingTimeText => RemainingTime.TotalHours >= 1
        ? RemainingTime.ToString(@"hh\:mm\:ss")
        : RemainingTime.ToString(@"mm\:ss");

    public string SelectedDurationText => SelectedDuration.TotalHours >= 1
        ? SelectedDuration.ToString(@"hh\:mm\:ss")
        : SelectedDuration.ToString(@"mm\:ss");

    public bool CanStart => !IsRunning && RemainingTime > TimeSpan.Zero;

    public bool CanPause => IsRunning;

    public bool CanReset => IsRunning || IsCompleted || RemainingTime != SelectedDuration;

    public string StatusText => IsCompleted
        ? "완료됨"
        : IsRunning
            ? "실행 중"
            : "대기 중";

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
