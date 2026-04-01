using System;
using System.Windows;
using System.Windows.Controls;

namespace TopTaskBar;

public partial class AlarmEditWindow : Window
{
    private readonly AlarmEntry _editingAlarm;

    public AlarmEditWindow(AlarmEntry alarm)
    {
        _editingAlarm = alarm.CreateEditableCopy();
        InitializeComponent();
        LoadFromAlarm();
    }

    public AlarmEntry EditedAlarm => _editingAlarm;

    private void OnHourIncreaseClick(object sender, RoutedEventArgs e)
    {
        AdjustTime(hoursDelta: 1, minutesDelta: 0);
    }

    private void OnHourDecreaseClick(object sender, RoutedEventArgs e)
    {
        AdjustTime(hoursDelta: -1, minutesDelta: 0);
    }

    private void OnMinuteIncreaseClick(object sender, RoutedEventArgs e)
    {
        AdjustTime(hoursDelta: 0, minutesDelta: 1);
    }

    private void OnMinuteDecreaseClick(object sender, RoutedEventArgs e)
    {
        AdjustTime(hoursDelta: 0, minutesDelta: -1);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var hour12 = ParsePart(HourTextBox.Text, GetDisplayHour(), 1, 12);
        var minute = ParsePart(MinuteTextBox.Text, _editingAlarm.Minute, 0, 59);
        var meridiem = (MeridiemComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "오전";
        var label = (AlarmLabelTextBox.Text ?? string.Empty).Trim();

        _editingAlarm.Label = string.IsNullOrWhiteSpace(label) ? "Alarm" : label;
        _editingAlarm.Hour24 = ConvertTo24Hour(hour12, meridiem == "오후");
        _editingAlarm.Minute = minute;
        _editingAlarm.DaysOfWeekMask = BuildDaysMask();

        DialogResult = true;
        Close();
    }

    private void LoadFromAlarm()
    {
        AlarmLabelTextBox.Text = _editingAlarm.Label;

        var hour24 = _editingAlarm.Hour24;
        MeridiemComboBox.SelectedIndex = hour24 < 12 ? 0 : 1;
        HourTextBox.Text = GetDisplayHour().ToString("00");
        MinuteTextBox.Text = _editingAlarm.Minute.ToString("00");

        SundayCheckBox.IsChecked = (_editingAlarm.DaysOfWeekMask & AlarmDayOfWeek.Sunday) != 0;
        MondayCheckBox.IsChecked = (_editingAlarm.DaysOfWeekMask & AlarmDayOfWeek.Monday) != 0;
        TuesdayCheckBox.IsChecked = (_editingAlarm.DaysOfWeekMask & AlarmDayOfWeek.Tuesday) != 0;
        WednesdayCheckBox.IsChecked = (_editingAlarm.DaysOfWeekMask & AlarmDayOfWeek.Wednesday) != 0;
        ThursdayCheckBox.IsChecked = (_editingAlarm.DaysOfWeekMask & AlarmDayOfWeek.Thursday) != 0;
        FridayCheckBox.IsChecked = (_editingAlarm.DaysOfWeekMask & AlarmDayOfWeek.Friday) != 0;
        SaturdayCheckBox.IsChecked = (_editingAlarm.DaysOfWeekMask & AlarmDayOfWeek.Saturday) != 0;
    }

    private void AdjustTime(int hoursDelta, int minutesDelta)
    {
        var hour12 = ParsePart(HourTextBox.Text, GetDisplayHour(), 1, 12);
        var minute = ParsePart(MinuteTextBox.Text, _editingAlarm.Minute, 0, 59);
        var totalMinutes = ((hour12 % 12) * 60) + minute + (hoursDelta * 60) + minutesDelta;
        var minutesPerHalfDay = 12 * 60;

        totalMinutes %= minutesPerHalfDay;
        if (totalMinutes < 0)
        {
            totalMinutes += minutesPerHalfDay;
        }

        var adjustedHour = totalMinutes / 60;
        if (adjustedHour == 0)
        {
            adjustedHour = 12;
        }

        HourTextBox.Text = adjustedHour.ToString("00");
        MinuteTextBox.Text = (totalMinutes % 60).ToString("00");
    }

    private int GetDisplayHour()
    {
        var hour12 = _editingAlarm.Hour24 % 12;
        return hour12 == 0 ? 12 : hour12;
    }

    private AlarmDayOfWeek BuildDaysMask()
    {
        AlarmDayOfWeek mask = AlarmDayOfWeek.None;

        if (SundayCheckBox.IsChecked == true) mask |= AlarmDayOfWeek.Sunday;
        if (MondayCheckBox.IsChecked == true) mask |= AlarmDayOfWeek.Monday;
        if (TuesdayCheckBox.IsChecked == true) mask |= AlarmDayOfWeek.Tuesday;
        if (WednesdayCheckBox.IsChecked == true) mask |= AlarmDayOfWeek.Wednesday;
        if (ThursdayCheckBox.IsChecked == true) mask |= AlarmDayOfWeek.Thursday;
        if (FridayCheckBox.IsChecked == true) mask |= AlarmDayOfWeek.Friday;
        if (SaturdayCheckBox.IsChecked == true) mask |= AlarmDayOfWeek.Saturday;

        return mask;
    }

    private static int ParsePart(string? text, int fallback, int min, int max)
    {
        if (!int.TryParse(text, out var value))
        {
            value = fallback;
        }

        return Math.Clamp(value, min, max);
    }

    private static int ConvertTo24Hour(int hour12, bool isPm)
    {
        hour12 = Math.Clamp(hour12, 1, 12);

        if (isPm)
        {
            return hour12 == 12 ? 12 : hour12 + 12;
        }

        return hour12 == 12 ? 0 : hour12;
    }
}
