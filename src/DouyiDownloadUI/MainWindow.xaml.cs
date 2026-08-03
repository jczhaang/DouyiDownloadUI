using System.Windows;
using System.Windows.Input;
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;
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
        FontManager.Apply(_viewModel.Settings.Load().FontSize, this);
    }

    private void RecentList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RecentList.SelectedItem is RecentDownloadEntry entry)
        {
            _viewModel.OpenRecent(entry);
        }
    }
}
