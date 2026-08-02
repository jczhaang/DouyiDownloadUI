using System.IO;

namespace DouyiDownloadUI;

public static class AppInfo
{
    public const string AppName = "抖音下载";
    public const string GitHubRepo = "jczhaang/DouyiDownloadUI";
    public static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DouyiDownloadUI",
        "settings.json");

    public static string EnginePath(string fileName)
    {
        var tools = Path.Combine(AppContext.BaseDirectory, "tools", fileName);
        return File.Exists(tools) ? tools : Path.Combine(AppContext.BaseDirectory, fileName);
    }
}
