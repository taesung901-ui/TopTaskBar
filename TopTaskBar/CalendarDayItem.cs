using System;

namespace TopTaskBar;

public sealed class CalendarDayItem
{
    public required DateTime Date { get; init; }

    public required string DayText { get; init; }

    public bool IsCurrentMonth { get; init; }

    public bool IsToday { get; init; }

    public bool IsSelected { get; init; }

    public bool IsSunday => Date.DayOfWeek == DayOfWeek.Sunday;

    public bool IsSaturday => Date.DayOfWeek == DayOfWeek.Saturday;
}
