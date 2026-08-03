using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TopTaskBar.Tests;

[TestClass]
public sealed class AlarmSchedulerTests
{
    [TestMethod]
    public void WeeklyAlarmAfterTodaysTimeSchedulesSameDayNextWeek()
    {
        using var scheduler = new AlarmScheduler();
        var alarm = CreateAlarm(AlarmDayOfWeek.Monday, hour: 9);
        var mondayAfterAlarm = new DateTime(2026, 8, 3, 10, 0, 0);

        var next = scheduler.GetNextOccurrence(alarm, mondayAfterAlarm);

        Assert.AreEqual(new DateTime(2026, 8, 10, 9, 0, 0), next);
    }

    [TestMethod]
    public void WeeklyAlarmBeforeTodaysTimeSchedulesToday()
    {
        using var scheduler = new AlarmScheduler();
        var alarm = CreateAlarm(AlarmDayOfWeek.Monday, hour: 9);
        var mondayBeforeAlarm = new DateTime(2026, 8, 3, 8, 0, 0);

        var next = scheduler.GetNextOccurrence(alarm, mondayBeforeAlarm);

        Assert.AreEqual(new DateTime(2026, 8, 3, 9, 0, 0), next);
    }

    [TestMethod]
    public void EarliestOccurrencesIncludeEveryAlarmAtTheSameTime()
    {
        using var scheduler = new AlarmScheduler();
        var first = CreateAlarm(AlarmDayOfWeek.Monday, hour: 9, label: "첫 번째");
        var second = CreateAlarm(AlarmDayOfWeek.Monday, hour: 9, label: "두 번째");
        var later = CreateAlarm(AlarmDayOfWeek.Monday, hour: 10, label: "나중");
        var reference = new DateTime(2026, 8, 3, 8, 0, 0);

        var next = scheduler.GetNextOccurrences([first, second, later], reference);

        Assert.AreEqual(2, next.Count);
        CollectionAssert.AreEquivalent(
            new[] { "첫 번째", "두 번째" },
            next.Select(item => item.Alarm.Label).ToArray());
        Assert.IsTrue(next.All(item => item.ScheduledAt == new DateTime(2026, 8, 3, 9, 0, 0)));
    }

    [TestMethod]
    public void OneTimeAlarmAfterTodaysTimeSchedulesTomorrow()
    {
        using var scheduler = new AlarmScheduler();
        var alarm = CreateAlarm(AlarmDayOfWeek.None, hour: 9);
        var reference = new DateTime(2026, 8, 3, 10, 0, 0);

        var next = scheduler.GetNextOccurrence(alarm, reference);

        Assert.AreEqual(new DateTime(2026, 8, 4, 9, 0, 0), next);
    }

    private static AlarmEntry CreateAlarm(
        AlarmDayOfWeek days,
        int hour,
        string label = "테스트")
    {
        return new AlarmEntry
        {
            Label = label,
            Enabled = true,
            Hour24 = hour,
            Minute = 0,
            DaysOfWeekMask = days
        };
    }
}
