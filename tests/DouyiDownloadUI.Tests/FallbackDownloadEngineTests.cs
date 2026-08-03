using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class FallbackDownloadEngineTests
{
    private sealed class RecordingEngine : IDownloadEngine
    {
        public VideoMetadata? Metadata { get; set; }
        public DownloadResult Result { get; set; } =
            new(true, null, DownloadErrorKind.None, null);
        public int MetadataCalls { get; private set; }
        public int DownloadCalls { get; private set; }

        public Task<VideoMetadata?> GetMetadataAsync(string shareUrl, CancellationToken ct)
        {
            MetadataCalls++;
            return Task.FromResult(Metadata);
        }

        public Task<DownloadResult> DownloadAsync(
            DownloadRequest request,
            IProgress<DownloadProgress>? progress,
            CancellationToken ct)
        {
            DownloadCalls++;
            return Task.FromResult(Result);
        }
    }

    private static DownloadRequest Request() =>
        new("u", "d", "n", DownloadMode.Video);

    [Fact]
    public async Task GetMetadataAsync_Falls_Back_When_Primary_Returns_Null()
    {
        var primary = new RecordingEngine();
        var fallback = new RecordingEngine { Metadata = new VideoMetadata("兜底标题") };
        var engine = new FallbackDownloadEngine(primary, fallback);

        var meta = await engine.GetMetadataAsync("u", CancellationToken.None);

        Assert.Equal("兜底标题", meta!.Title);
        Assert.Equal(1, fallback.MetadataCalls);
    }

    [Fact]
    public async Task GetMetadataAsync_Uses_Primary_When_Success()
    {
        var primary = new RecordingEngine { Metadata = new VideoMetadata("主标题") };
        var fallback = new RecordingEngine();
        var engine = new FallbackDownloadEngine(primary, fallback);

        var meta = await engine.GetMetadataAsync("u", CancellationToken.None);

        Assert.Equal("主标题", meta!.Title);
        Assert.Equal(0, fallback.MetadataCalls);
    }

    [Fact]
    public async Task DownloadAsync_Falls_Back_On_Failure()
    {
        var primary = new RecordingEngine
        {
            Result = new DownloadResult(false, null, DownloadErrorKind.EngineError, "x")
        };
        var fallback = new RecordingEngine
        {
            Result = new DownloadResult(true, "ok.mp4", DownloadErrorKind.None, null)
        };
        var engine = new FallbackDownloadEngine(primary, fallback);

        var result = await engine.DownloadAsync(Request(), null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1, fallback.DownloadCalls);
    }

    [Fact]
    public async Task DownloadAsync_Does_Not_Fall_Back_On_Cancel()
    {
        var primary = new RecordingEngine
        {
            Result = new DownloadResult(false, null, DownloadErrorKind.Canceled, "已取消")
        };
        var fallback = new RecordingEngine();
        var engine = new FallbackDownloadEngine(primary, fallback);

        var result = await engine.DownloadAsync(Request(), null, CancellationToken.None);

        Assert.Equal(DownloadErrorKind.Canceled, result.ErrorKind);
        Assert.Equal(0, fallback.DownloadCalls);
    }
}
