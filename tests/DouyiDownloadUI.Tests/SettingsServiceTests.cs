using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dyui-set-" + Guid.NewGuid().ToString("N"));

    public SettingsServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private string FilePath() => Path.Combine(_dir, "settings.json");

    [Fact]
    public void Load_Missing_File_Returns_Defaults()
    {
        var service = new SettingsService(FilePath());
        var settings = service.Load();
        Assert.Equal("Large", settings.FontSize);
        Assert.Contains("抖音下载", settings.SaveFolder);
    }

    [Fact]
    public void Save_And_Load_RoundTrip()
    {
        var service = new SettingsService(FilePath());
        var settings = new AppSettings
        {
            SaveFolder = @"D:\videos",
            FontSize = "ExtraLarge",
            LastNumber = 42,
            LastType = "中三",
            RecentTypes = new List<string> { "中三", "平四" },
            RecentDownloads = new List<RecentDownloadEntry>
            {
                new("001 中三 舞.mp4", @"D:\videos\001 中三 舞.mp4", DateTime.Now, false)
            }
        };
        service.Save(settings);
        var loaded = service.Load();
        Assert.Equal(@"D:\videos", loaded.SaveFolder);
        Assert.Equal("ExtraLarge", loaded.FontSize);
        Assert.Equal(42, loaded.LastNumber);
        Assert.Equal("中三", loaded.LastType);
        Assert.Equal(2, loaded.RecentTypes.Count);
        Assert.Single(loaded.RecentDownloads);
    }

    [Fact]
    public void Load_Corrupt_File_Returns_Defaults()
    {
        File.WriteAllText(FilePath(), "{ 这不是合法 JSON");
        var settings = new SettingsService(FilePath()).Load();
        Assert.Equal("Large", settings.FontSize);
    }
}
