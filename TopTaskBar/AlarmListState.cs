using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TopTaskBar;

public sealed class AlarmListState : INotifyPropertyChanged
{
    private AlarmEntry? _selectedAlarm;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AlarmEntry> Alarms { get; } = [];

    public AlarmEntry? SelectedAlarm
    {
        get => _selectedAlarm;
        set
        {
            if (ReferenceEquals(_selectedAlarm, value))
            {
                return;
            }

            _selectedAlarm = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
