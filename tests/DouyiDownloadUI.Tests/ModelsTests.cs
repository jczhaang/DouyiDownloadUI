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

    [Fact]
    public void VideoMetadata_Default_IsImagePost_Is_False()
    {
        var meta = new VideoMetadata("测试标题");
        Assert.False(meta.IsImagePost);
    }

    [Fact]
    public void VideoMetadata_With_IsImagePost_True()
    {
        var meta = new VideoMetadata("图文标题", IsImagePost: true);
        Assert.True(meta.IsImagePost);
        Assert.Equal("图文标题", meta.Title);
    }
}
