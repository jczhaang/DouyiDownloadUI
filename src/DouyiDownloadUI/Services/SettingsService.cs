using System.IO;
using System.Text.Json;
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Services;

public sealed class SettingsService
{
    private readonly string _filePath;

    public SettingsService(string settingsFilePath) => _filePath = settingsFilePath;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return CreateDefault();
            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            if (settings is null) return CreateDefault();
            settings.RecentTypes ??= new List<string>();
            settings.RecentDownloads ??= new List<RecentDownloadEntry>();
            if (string.IsNullOrWhiteSpace(settings.SaveFolder)) settings.SaveFolder = DefaultSaveFolder();
            return settings;
        }
        catch (Exception)
        {
            return CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    private static AppSettings CreateDefault() => new()
    {
        SaveFolder = DefaultSaveFolder(),
        FontSize = "Large",
        RecentTypes = new List<string>(),
        RecentDownloads = new List<RecentDownloadEntry>()
    };

    private static string DefaultSaveFolder() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "抖音下载");
}
