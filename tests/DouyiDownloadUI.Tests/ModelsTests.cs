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
    }

    [Fact]
    public void AppSettings_Default_TypeOptions_Has_Five_Presets()
    {
        var settings = new AppSettings();
        Assert.Equal(5, settings.TypeOptions.Count);
        Assert.Equal("中三", settings.TypeOptions[0]);
        Assert.Equal("中四", settings.TypeOptions[1]);
        Assert.Equal("平四", settings.TypeOptions[2]);
        Assert.Equal("三步", settings.TypeOptions[3]);
        Assert.Equal("其他", settings.TypeOptions[4]);
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
