using System.Diagnostics;
using System.IO;
using System.Windows;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI;

public partial class CrashWindow : Window
{
    public CrashWindow()
    {
        InitializeComponent();
        LogPathText.Text = LogService.LogDirectory;
    }

    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(LogService.LogDirectory);
            Process.Start(new ProcessStartInfo("explorer.exe", LogService.LogDirectory)
            {
                UseShellExecute = true
            });
        }
        catch (Exception)
        {
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
