public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();
        // Load settings, e.g.:
        // DarkModeSwitch.IsOn = ...;
    }

    private void DarkModeSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        // Save/apply dark mode setting
    }

    private void ClearMostVisited_Click(object sender, RoutedEventArgs e)
    {
        // Clear visit counts, e.g.:
        // ((MainWindow)App.Window).ClearVisitCounts();
    }
}