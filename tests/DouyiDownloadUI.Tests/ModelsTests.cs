using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Tests;

public class ModelsTests
{
    [Fact]
    public void DownloadResult_Default_Is_Failure()
    {
        var result = new DownloadResult(false, null, DownloadErrorKind.None, null);
        Assert.False(result.Success);
        Assert.Null(result.FilePath);
    }

    [Fact]
    public void AppSettings_Has_Defaults()
    {
        var settings = new AppSettings();
        Assert.Equal("Large", settings.FontSize);
        Assert.Null(settings.LastNumber);
        Assert.Empty(settings.RecentTypes);
    }
}
