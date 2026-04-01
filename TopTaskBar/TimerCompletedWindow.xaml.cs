using System.Windows;

namespace TopTaskBar;

public partial class TimerCompletedWindow : Window
{
    public TimerCompletedWindow()
    {
        InitializeComponent();
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
