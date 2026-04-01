using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace TopTaskBar;

public sealed class AlarmEntry : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _label = "Alarm";
    private int _hour24;
    private int _minute;
    private bool _enabled;
    private AlarmDayOfWeek _daysOfWeekMask = AlarmDayOfWeek.None;
    private DateTime? _nextOccurrence;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set
        {
            if (_id == value)
            {
                return;
            }

            _id = value;
            OnPropertyChanged();
        }
    }

    public string Label
    {
        get => _label;
        set
        {
            if (_label == value)
            {
                return;
            }

            _label = value;
            OnPropertyChanged();
        }
    }

    public int Hour24
    {
        get => _hour24;
        set
        {
            var normalized = Math.Clamp(value, 0, 23);
            if (_hour24 == normalized)
            {
                return;
            }

            _hour24 = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MeridiemText));
            OnPropertyChanged(nameof(DisplayTimeText));
        }
    }

    public int Minute
    {
        get => _minute;
        set
        {
            var normalized = Math.Clamp(value, 0, 59);
            if (_minute == normalized)
            {
                return;
            }

            _minute = normalized;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayTimeText));
        }
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            OnPropertyChanged();
        }
    }

    public AlarmDayOfWeek DaysOfWeekMask
    {
        get => _daysOfWeekMask;
        set
        {
            if (_daysOfWeekMask == value)
            {
                return;
            }

            _daysOfWeekMask = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsOneTime));
            OnPropertyChanged(nameof(RepeatSummary));
        }
    }

    public DateTime? NextOccurrence
    {
        get => _nextOccurrence;
        set
        {
            if (_nextOccurrence == value)
            {
                return;
            }

            _nextOccurrence = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusSummary));
        }
    }

    public bool IsOneTime => DaysOfWeekMask == AlarmDayOfWeek.None;

    public string MeridiemText => Hour24 < 12 ? "오전" : "오후";

    public string DisplayTimeText
    {
        get
        {
            var hour12 = Hour24 % 12;
            if (hour12 == 0)
            {
                hour12 = 12;
            }

            return $"{hour12}:{Minute:00}";
        }
    }

    public string RepeatSummary
    {
        get
        {
            if (IsOneTime)
            {
                return "1회성";
            }

            var parts = new[]
            {
                (AlarmDayOfWeek.Sunday, "일"),
                (AlarmDayOfWeek.Monday, "월"),
                (AlarmDayOfWeek.Tuesday, "화"),
                (AlarmDayOfWeek.Wednesday, "수"),
                (AlarmDayOfWeek.Thursday, "목"),
                (AlarmDayOfWeek.Friday, "금"),
                (AlarmDayOfWeek.Saturday, "토")
            }
            .Where(item => (DaysOfWeekMask & item.Item1) != 0)
            .Select(item => item.Item2);

            return string.Join(" ", parts);
        }
    }

    public string StatusSummary
    {
        get
        {
            if (!Enabled)
            {
                return IsOneTime ? "꺼짐 · 1회성" : $"꺼짐 · {RepeatSummary}";
            }

            if (NextOccurrence is null)
            {
                return IsOneTime ? "1회성 대기 중" : $"{RepeatSummary} 대기 중";
            }

            var next = NextOccurrence.Value;
            var today = DateTime.Today;
            var nextText = next.Date == today
                ? $"오늘 {next:HH:mm}"
                : next.Date == today.AddDays(1)
                    ? $"내일 {next:HH:mm}"
                    : next.ToString("MM-dd HH:mm");

            return IsOneTime ? $"{nextText} · 1회성" : $"{nextText} · {RepeatSummary}";
        }
    }

    public AlarmEntry CreateEditableCopy()
    {
        return new AlarmEntry
        {
            Id = Id,
            Label = Label,
            Hour24 = Hour24,
            Minute = Minute,
            Enabled = Enabled,
            DaysOfWeekMask = DaysOfWeekMask
        };
    }

    public void ApplyFrom(AlarmEntry source)
    {
        Label = source.Label;
        Hour24 = source.Hour24;
        Minute = source.Minute;
        Enabled = source.Enabled;
        DaysOfWeekMask = source.DaysOfWeekMask;
        NextOccurrence = source.NextOccurrence;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
