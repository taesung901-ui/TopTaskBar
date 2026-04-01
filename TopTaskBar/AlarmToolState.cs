using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TopTaskBar;

public sealed class AlarmToolState : INotifyPropertyChanged
{
    private int _selectedHour;
    private int _selectedMinute;
    private DateTime? _scheduledAt;
    private bool _isArmed;
    private bool _isCompleted;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int SelectedHour
    {
        get => _selectedHour;
        set
        {
            var normalized = Math.Clamp(value, 0, 23);
            if (_selectedHour == normalized)
            {
                return;
            }

            _selectedHour = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedTimeText));
        }
    }

    public int SelectedMinute
    {
        get => _selectedMinute;
        set
        {
            var normalized = Math.Clamp(value, 0, 59);
            if (_selectedMinute == normalized)
            {
                return;
            }

            _selectedMinute = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedTimeText));
        }
    }

    public DateTime? ScheduledAt
    {
        get => _scheduledAt;
        set
        {
            if (_scheduledAt == value)
            {
                return;
            }

            _scheduledAt = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ScheduledAtText));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public bool IsArmed
    {
        get => _isArmed;
        set
        {
            if (_isArmed == value)
            {
                return;
            }

            _isArmed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanArm));
            OnPropertyChanged(nameof(CanDisarm));
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
            OnPropertyChanged(nameof(CanArm));
            OnPropertyChanged(nameof(CanDisarm));
            OnPropertyChanged(nameof(StatusText));
        }
    }

    public string SelectedTimeText => $"{SelectedHour:00}:{SelectedMinute:00}";

    public string ScheduledAtText
    {
        get
        {
            if (ScheduledAt is null)
            {
                return "미설정";
            }

            var scheduled = ScheduledAt.Value;
            var today = DateTime.Today;

            if (scheduled.Date == today)
            {
                return $"오늘 {scheduled:HH:mm}";
            }

            if (scheduled.Date == today.AddDays(1))
            {
                return $"내일 {scheduled:HH:mm}";
            }

            return scheduled.ToString("MM-dd HH:mm");
        }
    }

    public bool CanArm => !IsArmed;

    public bool CanDisarm => IsArmed || IsCompleted;

    public string StatusText
    {
        get
        {
            if (IsCompleted)
            {
                return "알람 완료";
            }

            if (IsArmed && ScheduledAt is not null)
            {
                return $"{ScheduledAtText} 대기 중";
            }

            return "미설정";
        }
    }

    public void RefreshComputedState()
    {
        OnPropertyChanged(nameof(SelectedTimeText));
        OnPropertyChanged(nameof(ScheduledAtText));
        OnPropertyChanged(nameof(CanArm));
        OnPropertyChanged(nameof(CanDisarm));
        OnPropertyChanged(nameof(StatusText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
