using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class YtDlpCommandBuilderTests
{
    private static DownloadRequest Request(DownloadMode mode) => new(
        "https://v.douyin.com/h94R-IulXc8/",
        @"D:\videos",
        "007 中三 舞",
        mode);

    [Fact]
    public void Video_Mode_Contains_Common_Args()
    {
        var args = YtDlpCommandBuilder.BuildArguments(Request(DownloadMode.Video), @"D:\tools");
        Assert.Contains("--no-playlist", args);
        Assert.Contains("--no-overwrites", args);
        Assert.Contains("--newline", args);
        Assert.Contains("--progress-template", args);
        Assert.Contains(@"D:\tools", args);
        Assert.Contains(@"D:\videos\007 中三 舞.%(ext)s", args);
        Assert.Equal("https://v.douyin.com/h94R-IulXc8/", args[^1]);
        Assert.DoesNotContain("--extract-audio", args);
    }

    [Fact]
    public void Audio_Mode_Contains_Extract_Audio()
    {
        var args = YtDlpCommandBuilder.BuildArguments(Request(DownloadMode.Audio), @"D:\tools");
        Assert.Contains("--extract-audio", args);
        Assert.Contains("--audio-format", args);
        Assert.Contains("mp3", args);
    }
}
