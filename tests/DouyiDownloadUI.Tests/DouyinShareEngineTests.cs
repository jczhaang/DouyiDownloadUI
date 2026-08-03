using System.Diagnostics;
using System.Net;
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class DouyinShareEngineTests : IDisposable
{
    private const string ShareHtml = """
        <html><head><script>
        window._ROUTER_DATA = {"loaderData":{"video_(id)\u002Fpage":{"ua":"Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X)","videoInfoRes":{"item_list":[{"desc":"什么是Node.js 这期视频","video":{"play_addr":{"uri":"v0200fg10000d8uuhinog65i77g243bg","url_list":["https:\u002F\u002Faweme.snssdk.com\u002Faweme\u002Fv1\u002Fplaywm\u002F?line=0&logo_name=aweme_diversion_search&ratio=720p&video_id=v0200fg10000d8uuhinog65i77g243bg"]}}}}]}}};
        </script></head></html>
        """;

    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "dyui-share-" + Guid.NewGuid().ToString("N"));
    private readonly FakeHttpHandler _handler = new();
    private readonly HttpClient _client;

    public DouyinShareEngineTests()
    {
        Directory.CreateDirectory(_dir);
        _client = new HttpClient(_handler);
    }

    public void Dispose()
    {
        _client.Dispose();
        Directory.Delete(_dir, recursive: true);
    }

    private DouyinShareEngine NewEngine(IProcessRunner? runner = null)
        => new(_client, Path.Combine(_dir, "ffmpeg.exe"), runner);

    private DownloadRequest Request(
        string name = "001 中三 舞", DownloadMode mode = DownloadMode.Video)
        => new("https://v.douyin.com/h94R-IulXc8/", _dir, name, mode);

    private static HttpResponseMessage Html() =>
        new(HttpStatusCode.OK) { Content = new StringContent(ShareHtml) };

    private static HttpResponseMessage Bytes(byte[] bytes) =>
        new(HttpStatusCode.OK) { Content = new ByteArrayContent(bytes) };

    [Fact]
    public async Task GetMetadataAsync_Uses_Iesdouyin_Share_Page_And_Returns_Title()
    {
        var requested = new List<string>();
        _handler.Responder = url =>
        {
            requested.Add(url);
            return Html();
        };

        var meta = await NewEngine().GetMetadataAsync(
            "https://www.douyin.com/video/7655535289644928298", CancellationToken.None);

        Assert.Equal("什么是Node.js 这期视频", meta!.Title);
        Assert.Contains(requested, u => u.Contains("iesdouyin.com/share/video/7655535289644928298"));
    }

    [Fact]
    public async Task GetMetadataAsync_Short_Link_Fetches_Link_Directly()
    {
        var requested = new List<string>();
        _handler.Responder = url =>
        {
            requested.Add(url);
            return Html();
        };

        var meta = await NewEngine().GetMetadataAsync(
            "https://v.douyin.com/h94R-IulXc8/", CancellationToken.None);

        Assert.Equal("什么是Node.js 这期视频", meta!.Title);
        Assert.Contains(requested, u => u == "https://v.douyin.com/h94R-IulXc8/");
    }

    [Fact]
    public async Task GetMetadataAsync_Page_Not_Found_Returns_Null()
    {
        _handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.NotFound);
        Assert.Null(await NewEngine().GetMetadataAsync(
            "https://www.douyin.com/video/7655535289644928298", CancellationToken.None));
    }

    [Fact]
    public async Task DownloadAsync_Downloads_Mp4_And_Reports_Progress()
    {
        var videoBytes = new byte[200 * 1024];
        for (var i = 0; i < videoBytes.Length; i++) videoBytes[i] = (byte)(i % 251);
        _handler.Responder = url =>
            url.Contains("playwm") ? Bytes(videoBytes) : Html();

        var reported = new List<DownloadProgress>();
        var result = await NewEngine().DownloadAsync(
            Request(), new Progress<DownloadProgress>(reported.Add), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("001 中三 舞.mp4", Path.GetFileName(result.FilePath!));
        Assert.Equal(videoBytes.Length, new FileInfo(result.FilePath!).Length);
        Assert.NotEmpty(reported);
        Assert.Equal(100, reported[^1].Percent, 1);
    }

    [Fact]
    public async Task DownloadAsync_Existing_File_Gets_Unique_Name()
    {
        File.WriteAllText(Path.Combine(_dir, "001 中三 舞.mp4"), "old");
        _handler.Responder = url =>
            url.Contains("playwm") ? Bytes(new byte[1024]) : Html();

        var result = await NewEngine().DownloadAsync(
            Request(), null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("001 中三 舞（2）.mp4", Path.GetFileName(result.FilePath!));
    }

    [Fact]
    public async Task DownloadAsync_Cancel_Cleans_Partial_File()
    {
        _handler.Responder = url => url.Contains("playwm")
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new CancelAfterFirstReadStream())
            }
            : Html();
        using var cts = new CancellationTokenSource();

        var engine = NewEngine();
        var task = engine.DownloadAsync(Request(), null, cts.Token);
        cts.CancelAfter(200);
        var result = await task;

        Assert.Equal(DownloadErrorKind.Canceled, result.ErrorKind);
        Assert.Empty(Directory.GetFiles(_dir, "*.part"));
    }

    [Fact]
    public async Task DownloadAsync_Audio_Converts_To_Mp3_And_Removes_Temp_Mp4()
    {
        _handler.Responder = url =>
            url.Contains("playwm") ? Bytes(new byte[64 * 1024]) : Html();
        var ffmpeg = new FakeFfmpegRunner();

        var result = await NewEngine(ffmpeg).DownloadAsync(
            Request(mode: DownloadMode.Audio), null, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("001 中三 舞.mp3", Path.GetFileName(result.FilePath!));
        Assert.True(File.Exists(result.FilePath!));
        Assert.NotNull(ffmpeg.LastStartInfo);
        Assert.Equal(Path.Combine(_dir, "ffmpeg.exe"), ffmpeg.LastStartInfo!.FileName);
        Assert.DoesNotContain(Directory.GetFiles(_dir), f => f.Contains(".douyi-tmp"));
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        public Func<string, HttpResponseMessage> Responder { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.NotFound);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            return Task.FromResult(Responder(request.RequestUri!.ToString()));
        }
    }

    private sealed class CancelAfterFirstReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get => 0; set { } }
        public override void Flush() { }

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken ct)
        {
            await Task.Delay(Timeout.Infinite, ct);
            return 0;
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }

    private sealed class FakeFfmpegRunner : IProcessRunner
    {
        public ProcessStartInfo? LastStartInfo { get; private set; }

        public Task<int> RunAsync(
            ProcessStartInfo startInfo,
            Action<string>? onStdoutLine,
            Action<string>? onStderrLine,
            CancellationToken ct)
        {
            LastStartInfo = startInfo;
            var args = startInfo.ArgumentList.ToList();
            var inputIndex = args.IndexOf("-i");
            var input = inputIndex >= 0 && inputIndex + 1 < args.Count ? args[inputIndex + 1] : null;
            var output = args.Count > 0 ? args[^1] : null;
            if (input is not null && output is not null && File.Exists(input))
            {
                File.Copy(input, output, overwrite: true);
            }
            return Task.FromResult(0);
        }
    }
}
