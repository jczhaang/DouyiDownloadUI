using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class YtDlpEngineTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dyui-engine-" + Guid.NewGuid().ToString("N"));
    private readonly FakeProcessRunner _runner = new();

    public YtDlpEngineTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private YtDlpEngine NewEngine() => new("yt-dlp.exe", Path.Combine(_dir, "ffmpeg"), _runner);

    private DownloadRequest Request(string name = "001 中三 舞") =>
        new("https://v.douyin.com/abc/", _dir, name, DownloadMode.Video);

    [Fact]
    public async Task GetMetadataAsync_Returns_Title()
    {
        var engine = NewEngine();
        var meta = await engine.GetMetadataAsync("https://v.douyin.com/abc/", CancellationToken.None);
        Assert.Equal("测试视频标题", meta!.Title);
    }

    [Fact]
    public async Task GetMetadataAsync_NonZero_Exit_Returns_Null()
    {
        _runner.ExitCode = 1;
        var engine = NewEngine();
        Assert.Null(await engine.GetMetadataAsync("https://v.douyin.com/abc/", CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_Reports_Progress_And_Succeeds()
    {
        _runner.StdoutLines.Add("download:45.6% 1.2MiB/s 00:05");
        var reported = new List<DownloadProgress>();
        var engine = NewEngine();
        var result = await engine.DownloadAsync(
            Request(), new Progress<DownloadProgress>(reported.Add), CancellationToken.None);
        Assert.True(result.Success);
        Assert.NotNull(result.FilePath);
        Assert.Equal("001 中三 舞.mp4", Path.GetFileName(result.FilePath!));
        Assert.Single(reported);
        Assert.Equal(45.6, reported[0].Percent);
    }

    [Fact]
    public async Task DownloadAsync_Missing_Directory_Returns_SavePathInvalid()
    {
        var engine = NewEngine();
        var result = await engine.DownloadAsync(
            new DownloadRequest("u", Path.Combine(_dir, "no-such"), "001 舞", DownloadMode.Video),
            null, CancellationToken.None);
        Assert.False(result.Success);
        Assert.Equal(DownloadErrorKind.SavePathInvalid, result.ErrorKind);
    }

    [Fact]
    public async Task DownloadAsync_Stderr_Unavailable_Maps_To_VideoUnavailable()
    {
        _runner.ExitCode = 1;
        _runner.StderrLine = "ERROR: Video unavailable";
        var result = await NewEngine().DownloadAsync(Request(), null, CancellationToken.None);
        Assert.Equal(DownloadErrorKind.VideoUnavailable, result.ErrorKind);
    }

    [Fact]
    public async Task DownloadAsync_Existing_File_Gets_Unique_Name()
    {
        File.WriteAllText(Path.Combine(_dir, "001 中三 舞.mp4"), "old");
        var engine = NewEngine();
        var result = await engine.DownloadAsync(Request(), null, CancellationToken.None);
        Assert.True(result.Success);
        Assert.Equal("001 中三 舞（2）.mp4", Path.GetFileName(result.FilePath!));
    }

    [Fact]
    public async Task DownloadAsync_Cancel_Cleans_Partial_And_Returns_Canceled()
    {
        _runner.CancelOnRun = true;
        File.WriteAllText(Path.Combine(_dir, "001 中三 舞.mp4.part"), "partial");
        var result = await NewEngine().DownloadAsync(Request(), null, CancellationToken.None);
        Assert.Equal(DownloadErrorKind.Canceled, result.ErrorKind);
        Assert.False(File.Exists(Path.Combine(_dir, "001 中三 舞.mp4.part")));
    }
}
