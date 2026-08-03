using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace TopTaskBar;

public sealed class AlarmScheduler : IDisposable
{
    private readonly DispatcherTimer _timer;
    private List<AlarmEntry> _alarms = [];
    private List<(AlarmEntry Alarm, DateTime ScheduledAt)> _nextAlarms = [];

    public AlarmScheduler()
    {
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
    }

    public event EventHandler<AlarmTriggeredEventArgs>? AlarmTriggered;

    public void SetAlarms(IEnumerable<AlarmEntry> alarms)
    {
        _alarms = alarms.ToList();
        RefreshSchedule();
    }

    public void RefreshSchedule()
    {
        _nextAlarms = GetNextOccurrences(_alarms, DateTime.Now).ToList();

        if (_nextAlarms.Count == 0)
        {
            _timer.Stop();
            return;
        }

        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    public DateTime? GetNextOccurrence(AlarmEntry alarm, DateTime referenceTime)
    {
        if (!alarm.Enabled)
        {
            return null;
        }

        if (alarm.IsOneTime)
        {
            return GetNextOneTimeOccurrence(alarm, referenceTime);
        }

        return GetNextRecurringOccurrence(alarm, referenceTime);
    }

    public (AlarmEntry Alarm, DateTime ScheduledAt)? GetNextOccurrence(IEnumerable<AlarmEntry> alarms, DateTime referenceTime)
    {
        var nextOccurrences = GetNextOccurrences(alarms, referenceTime);
        return nextOccurrences.Count == 0 ? null : nextOccurrences[0];
    }

    public IReadOnlyList<(AlarmEntry Alarm, DateTime ScheduledAt)> GetNextOccurrences(
        IEnumerable<AlarmEntry> alarms,
        DateTime referenceTime)
    {
        var scheduledAlarms = alarms
            .Where(alarm => alarm.Enabled)
            .Select(alarm => new { Alarm = alarm, ScheduledAt = GetNextOccurrence(alarm, referenceTime) })
            .Where(item => item.ScheduledAt is not null)
            .OrderBy(item => item.ScheduledAt)
            .Select(item => (Alarm: item.Alarm, ScheduledAt: item.ScheduledAt!.Value))
            .ToList();

        if (scheduledAlarms.Count == 0)
        {
            return [];
        }

        var earliestTime = scheduledAlarms[0].ScheduledAt;
        return scheduledAlarms
            .TakeWhile(item => item.ScheduledAt == earliestTime)
            .ToList();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_nextAlarms.Count == 0)
        {
            _timer.Stop();
            return;
        }

        if (DateTime.Now < _nextAlarms[0].ScheduledAt)
        {
            return;
        }

        var triggeredAlarms = _nextAlarms;
        _nextAlarms = [];
        _timer.Stop();

        foreach (var triggered in triggeredAlarms)
        {
            AlarmTriggered?.Invoke(this, new AlarmTriggeredEventArgs(triggered.Alarm, triggered.ScheduledAt));
        }
    }

    private static DateTime GetNextOneTimeOccurrence(AlarmEntry alarm, DateTime referenceTime)
    {
        var scheduled = new DateTime(
            referenceTime.Year,
            referenceTime.Month,
            referenceTime.Day,
            Math.Clamp(alarm.Hour24, 0, 23),
            Math.Clamp(alarm.Minute, 0, 59),
            0);

        if (scheduled <= referenceTime)
        {
            scheduled = scheduled.AddDays(1);
        }

        return scheduled;
    }

    private static DateTime? GetNextRecurringOccurrence(AlarmEntry alarm, DateTime referenceTime)
    {
        var days = Enumerable.Range(0, 8)
            .Select(offset =>
            {
                var date = referenceTime.Date.AddDays(offset);
                var dayMask = ToMask(date.DayOfWeek);
                if ((alarm.DaysOfWeekMask & dayMask) == 0)
                {
                    return (DateTime?)null;
                }

                var scheduled = new DateTime(
                    date.Year,
                    date.Month,
                    date.Day,
                    Math.Clamp(alarm.Hour24, 0, 23),
                    Math.Clamp(alarm.Minute, 0, 59),
                    0);

                return scheduled > referenceTime ? scheduled : null;
            })
            .Where(date => date is not null)
            .OrderBy(date => date)
            .FirstOrDefault();

        return days;
    }

    private static AlarmDayOfWeek ToMask(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => AlarmDayOfWeek.Sunday,
            DayOfWeek.Monday => AlarmDayOfWeek.Monday,
            DayOfWeek.Tuesday => AlarmDayOfWeek.Tuesday,
            DayOfWeek.Wednesday => AlarmDayOfWeek.Wednesday,
            DayOfWeek.Thursday => AlarmDayOfWeek.Thursday,
            DayOfWeek.Friday => AlarmDayOfWeek.Friday,
            DayOfWeek.Saturday => AlarmDayOfWeek.Saturday,
            _ => AlarmDayOfWeek.None
        };
    }
}

public sealed class AlarmTriggeredEventArgs : EventArgs
{
    public AlarmTriggeredEventArgs(AlarmEntry alarm, DateTime scheduledAt)
    {
        Alarm = alarm;
        ScheduledAt = scheduledAt;
    }

    public AlarmEntry Alarm { get; }

    public DateTime ScheduledAt { get; }
}
