using System.IO;

namespace DouyiDownloadUI.Services;

public static class LogService
{
    public static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DouyiDownloadUI",
        "logs");

    private static readonly object Sync = new();

    public static void Info(string message) => Write("INFO", message);
    public static void Error(string message, Exception? ex = null) =>
        Write("ERROR", ex is null ? message : $"{message}\n{ex}");

    public static string GetLatestLogPath()
    {
        Directory.CreateDirectory(LogDirectory);
        return Path.Combine(LogDirectory, $"douyi-{DateTime.Now:yyyy-MM-dd}.log");
    }

    private static void Write(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var path = GetLatestLogPath();
            lock (Sync)
            {
                File.AppendAllText(
                    path,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}");
            }
        }
        catch (Exception)
        {
            // 日志失败不影响主流程
        }
    }
}
