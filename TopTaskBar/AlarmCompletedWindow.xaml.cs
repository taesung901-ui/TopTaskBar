using System.Windows;

namespace TopTaskBar;

public partial class AlarmCompletedWindow : Window
{
    public AlarmCompletedWindow()
    {
        InitializeComponent();
    }

    public void SetAlarmLabel(string label)
    {
        AlarmLabelText.Text = label;
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
