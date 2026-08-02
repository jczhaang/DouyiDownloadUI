using System.IO;
using System.Text.Json;

namespace DouyiDownloadUI;

public static class AppInfo
{
    public const string AppName = "抖音下载";
    public const string GitHubRepo = "jczhaang/DouyiDownloadUI";
    public static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DouyiDownloadUI",
        "settings.json");

    public static string EngineVersion
    {
        get
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "tools", "engine-version.json");
                if (!File.Exists(path)) return "未知";
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                return doc.RootElement.TryGetProperty("version", out var version)
                       && version.GetString() is { Length: > 0 } value
                    ? value
                    : "未知";
            }
            catch (Exception)
            {
                return "未知";
            }
        }
    }

    public static string EnginePath(string fileName)
    {
        var tools = Path.Combine(AppContext.BaseDirectory, "tools", fileName);
        return File.Exists(tools) ? tools : Path.Combine(AppContext.BaseDirectory, fileName);
    }
}
