using System;
using System.Windows.Threading;

namespace TopTaskBar;

public sealed class TimerToolController : IDisposable
{
    private readonly DispatcherTimer _timer;

    public TimerToolController()
    {
        State = new TimerToolState();
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
    }

    public event EventHandler? Completed;

    public TimerToolState State { get; }

    public void SetPreset(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return;
        }

        _timer.Stop();
        State.IsRunning = false;
        State.SelectedDuration = duration;
        State.RemainingTime = duration;
        State.IsCompleted = false;
    }

    public void Start()
    {
        if (State.RemainingTime <= TimeSpan.Zero)
        {
            State.RemainingTime = State.SelectedDuration;
        }

        State.IsCompleted = false;
        State.IsRunning = true;
        _timer.Start();
    }

    public void Pause()
    {
        _timer.Stop();
        State.IsRunning = false;
    }

    public void Reset()
    {
        _timer.Stop();
        State.IsRunning = false;
        State.IsCompleted = false;
        State.RemainingTime = State.SelectedDuration;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (State.RemainingTime <= TimeSpan.Zero)
        {
            Complete();
            return;
        }

        State.RemainingTime -= TimeSpan.FromSeconds(1);
        if (State.RemainingTime <= TimeSpan.Zero)
        {
            State.RemainingTime = TimeSpan.Zero;
            Complete();
        }
    }

    private void Complete()
    {
        _timer.Stop();
        State.IsRunning = false;
        State.IsCompleted = true;
        Completed?.Invoke(this, EventArgs.Empty);
    }
}
