using System;
using System.Windows.Threading;

namespace TopTaskBar;

public sealed class AlarmToolController : IDisposable
{
    private readonly DispatcherTimer _timer;

    public AlarmToolController()
    {
        State = new AlarmToolState
        {
            SelectedHour = DateTime.Now.Hour,
            SelectedMinute = DateTime.Now.Minute,
            ScheduledAt = null,
            IsArmed = false,
            IsCompleted = false
        };

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
    }

    public AlarmToolState State { get; }

    public event EventHandler? Completed;

    public void SetTime(int hour, int minute)
    {
        State.SelectedHour = hour;
        State.SelectedMinute = minute;
        State.IsCompleted = false;

        if (State.IsArmed)
        {
            State.ScheduledAt = CalculateNextOccurrence();
        }
        else
        {
            State.ScheduledAt = null;
        }

        State.RefreshComputedState();
    }

    public void Arm()
    {
        State.IsCompleted = false;
        State.ScheduledAt = CalculateNextOccurrence();
        State.IsArmed = true;
        _timer.Start();
        State.RefreshComputedState();
    }

    public void Disarm()
    {
        _timer.Stop();
        State.IsArmed = false;
        State.IsCompleted = false;
        State.ScheduledAt = null;
        State.RefreshComputedState();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (!State.IsArmed || State.ScheduledAt is null)
        {
            return;
        }

        if (DateTime.Now < State.ScheduledAt.Value)
        {
            return;
        }

        _timer.Stop();
        State.IsArmed = false;
        State.IsCompleted = true;
        State.ScheduledAt = null;
        State.RefreshComputedState();
        Completed?.Invoke(this, EventArgs.Empty);
    }

    private DateTime CalculateNextOccurrence()
    {
        var now = DateTime.Now;
        var scheduled = new DateTime(now.Year, now.Month, now.Day, State.SelectedHour, State.SelectedMinute, 0);

        if (scheduled <= now)
        {
            scheduled = scheduled.AddDays(1);
        }

        return scheduled;
    }
}
