using System.Windows;
using DouyiDownloadUI.ViewModels;

namespace DouyiDownloadUI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void Window_Activated(object sender, EventArgs e) => _viewModel.OnWindowActivated();

    private void Title_SelectionChanged(object sender, RoutedEventArgs e)
    {
        if (TitleTextBox is { SelectionLength: > 0 })
        {
            _viewModel.SetFileNameFromSelection(TitleTextBox.SelectedText);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var window = new SettingsWindow(_viewModel.Settings);
        window.Owner = this;
        window.ShowDialog();
        _viewModel.RefreshFromSettings();
    }
}
