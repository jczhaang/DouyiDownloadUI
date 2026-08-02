using System.Windows;
using Microsoft.Win32;
using DouyiDownloadUI.Services;
using DouyiDownloadUI.ViewModels;

namespace DouyiDownloadUI;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsService settings)
    {
        InitializeComponent();
        FontManager.Apply(settings.Load().FontSize, this);
        _viewModel = new SettingsViewModel(settings);
        DataContext = _viewModel;
    }

    private void ChangeFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择保存位置",
            InitialDirectory = _viewModel.SaveFolder
        };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.ApplySaveFolder(dialog.FolderName);
        }
    }
}
