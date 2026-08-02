using System.Windows;
using DouyiDownloadUI.Services;
using DouyiDownloadUI.ViewModels;
using System.IO;

namespace DouyiDownloadUI;

public partial class App : Application
{
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            LogService.Error("未处理异常", args.Exception);
            new CrashWindow().ShowDialog();
            args.Handled = true;
        };
        CleanupOldLogs();
        var settings = new SettingsService(AppInfo.SettingsPath);
        var config = settings.Load();
        var engine = new YtDlpEngine(AppInfo.EnginePath("yt-dlp.exe"), AppInfo.EnginePath("ffmpeg.exe"));
        var viewModel = new MainViewModel(engine, settings, new ClipboardService());
        _window = new MainWindow(viewModel);
        FontManager.Apply(config.FontSize, _window);
        _window.Show();
    }

    private static void CleanupOldLogs()
    {
        try
        {
            Directory.CreateDirectory(LogService.LogDirectory);
            foreach (var file in Directory.EnumerateFiles(LogService.LogDirectory, "douyi-*.log"))
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-30)) File.Delete(file);
            }
        }
        catch (Exception)
        {
        }
    }
}
