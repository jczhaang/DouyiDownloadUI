using System.Windows;
using DouyiDownloadUI.Services;
using DouyiDownloadUI.ViewModels;

namespace DouyiDownloadUI;

public partial class App : Application
{
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = new SettingsService(AppInfo.SettingsPath);
        var config = settings.Load();
        var engine = new YtDlpEngine(AppInfo.EnginePath("yt-dlp.exe"), AppInfo.EnginePath("ffmpeg.exe"));
        var viewModel = new MainViewModel(engine, settings, new ClipboardService());
        _window = new MainWindow(viewModel);
        // Task 12 解除注释：FontManager.Apply(config.FontSize, _window);
        _window.Show();
    }
}
