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
            TypeOptions = new List<string> { "中三", "平四" },
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
        Assert.Equal(2, loaded.TypeOptions.Count);
        Assert.Single(loaded.RecentDownloads);
    }

    [Fact]
    public void Load_Corrupt_File_Returns_Defaults()
    {
        File.WriteAllText(FilePath(), "{ 这不是合法 JSON");
        var settings = new SettingsService(FilePath()).Load();
        Assert.Equal("Large", settings.FontSize);
    }

    [Fact]
    public void Load_Missing_File_Returns_Default_TypeOptions()
    {
        var service = new SettingsService(FilePath());
        var settings = service.Load();
        Assert.Equal(5, settings.TypeOptions.Count);
        Assert.Contains("中三", settings.TypeOptions);
        Assert.Contains("其他", settings.TypeOptions);
    }

    [Fact]
    public void Load_Old_Config_Without_TypeOptions_Initializes_Defaults()
    {
        File.WriteAllText(FilePath(),
            "{\"SaveFolder\":\"D:\\\\v\",\"FontSize\":\"Large\",\"RecentTypes\":[\"中三\"]}");
        var settings = new SettingsService(FilePath()).Load();
        Assert.Equal(5, settings.TypeOptions.Count);
        Assert.Equal("中三", settings.TypeOptions[0]);
    }

    [Fact]
    public void Save_And_Load_TypeOptions_RoundTrip()
    {
        var service = new SettingsService(FilePath());
        var settings = new AppSettings
        {
            SaveFolder = @"D:\videos",
            TypeOptions = new List<string> { "华尔兹", "探戈" }
        };
        service.Save(settings);
        var loaded = service.Load();
        Assert.Equal(2, loaded.TypeOptions.Count);
        Assert.Equal("华尔兹", loaded.TypeOptions[0]);
        Assert.Equal("探戈", loaded.TypeOptions[1]);
    }
}
