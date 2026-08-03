using System.Diagnostics;
using System.IO;
using System.Net.Http;
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Services;

public sealed class DouyinShareEngine : IDownloadEngine
{
    private const string MobileUserAgent =
        "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) " +
        "AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1";
    private const int BufferSize = 64 * 1024;

    private readonly HttpClient _httpClient;
    private readonly string _ffmpegLocation;
    private readonly IProcessRunner _processRunner;

    public DouyinShareEngine(
        HttpClient httpClient, string ffmpegLocation, IProcessRunner? processRunner = null)
    {
        _httpClient = httpClient;
        _ffmpegLocation = ffmpegLocation;
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public async Task<VideoMetadata?> GetMetadataAsync(string shareUrl, CancellationToken ct)
    {
        var html = await FetchSharePageAsync(shareUrl, ct);
        if (html is null)
        {
            LogService.Error($"抖音分享页获取失败：{shareUrl}");
            return null;
        }
        var info = DouyinShareParser.Parse(html);
        if (info is null)
        {
            LogService.Error($"抖音分享页未找到播放数据：{shareUrl}");
            return null;
        }
        if (info.Title is not { Length: > 0 } title)
        {
            LogService.Error($"抖音分享页缺少标题：{shareUrl}");
            return null;
        }
        return new VideoMetadata(title, info.IsImagePost);
    }

    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        if (!Directory.Exists(request.OutputDirectory))
        {
            return new DownloadResult(
                false, null, DownloadErrorKind.SavePathInvalid, request.OutputDirectory);
        }

        var html = await FetchSharePageAsync(request.ShareUrl, ct);
        if (html is null)
        {
            return new DownloadResult(false, null, DownloadErrorKind.Network, "分享页获取失败");
        }
        var info = DouyinShareParser.Parse(html);
        if (info?.PlayUrl is null)
        {
            return new DownloadResult(
                false, null, DownloadErrorKind.VideoUnavailable, "页面中未找到播放地址");
        }

        if (info.IsImagePost && request.Mode == DownloadMode.Video)
        {
            return new DownloadResult(
                false, null, DownloadErrorKind.VideoUnavailable, "这是图文作品，没有视频可下载");
        }

        var ext = request.Mode == DownloadMode.Audio ? "mp3" : "mp4";
        var unique = FilenameBuilder.MakeUnique(
            request.OutputDirectory, request.FileNameWithoutExtension, ext);
        var finalPath = Path.Combine(request.OutputDirectory, unique);

        try
        {
            if (request.Mode == DownloadMode.Audio)
            {
                if (info.IsImagePost)
                {
                    // 图文作品：音乐 URL 已是 MP3，直接流式下载，无需 ffmpeg
                    await DownloadVideoAsync(info.PlayUrl, finalPath, progress, ct);
                }
                else
                {
                    await DownloadAudioAsync(info.PlayUrl, finalPath, progress, ct);
                }
            }
            else
            {
                await DownloadVideoAsync(info.PlayUrl, finalPath, progress, ct);
            }
            return new DownloadResult(true, finalPath, DownloadErrorKind.None, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            CleanupPartial(finalPath);
            return new DownloadResult(false, null, DownloadErrorKind.Canceled, "已取消");
        }
        catch (OperationCanceledException ex)
        {
            CleanupPartial(finalPath);
            return new DownloadResult(false, null, DownloadErrorKind.Network, ex.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            CleanupPartial(finalPath);
            return new DownloadResult(false, null, DownloadErrorKind.Network, ex.Message);
        }
        catch (Exception ex)
        {
            CleanupPartial(finalPath);
            return new DownloadResult(false, null, DownloadErrorKind.EngineError, ex.Message);
        }
    }

    private async Task<string?> FetchSharePageAsync(string shareUrl, CancellationToken ct)
    {
        var videoId = DouyinShareParser.ExtractVideoId(shareUrl);
        var pageUrl = videoId is not null
            ? $"https://www.iesdouyin.com/share/video/{videoId}"
            : shareUrl;
        try
        {
            using var request = CreateRequest(HttpMethod.Get, pageUrl);
            using var response = await _httpClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private async Task DownloadVideoAsync(
        string playUrl, string finalPath, IProgress<DownloadProgress>? progress, CancellationToken ct)
    {
        var partPath = finalPath + ".part";
        using var request = CreateRequest(HttpMethod.Get, playUrl);
        using var response = await _httpClient.SendAsync(
            request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"播放地址返回 {(int)response.StatusCode}");
        }

        await using var source = await response.Content.ReadAsStreamAsync(ct);
        using (var target = new FileStream(
                   partPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var total = response.Content.Headers.ContentLength;
            var buffer = new byte[BufferSize];
            long downloaded = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, ct)) > 0)
            {
                await target.WriteAsync(buffer.AsMemory(0, read), ct);
                downloaded += read;
                if (total > 0)
                {
                    progress?.Report(new DownloadProgress(
                        downloaded * 100.0 / total.Value, null, null));
                }
            }
            await target.FlushAsync(ct);
        }
        File.Move(partPath, finalPath);
    }

    private async Task DownloadAudioAsync(
        string playUrl, string finalPath, IProgress<DownloadProgress>? progress, CancellationToken ct)
    {
        var dir = Path.GetDirectoryName(finalPath)!;
        var tempMp4 = Path.Combine(
            dir, ".douyi-tmp-" + Path.GetFileNameWithoutExtension(finalPath) + ".mp4");
        if (File.Exists(tempMp4)) File.Delete(tempMp4);
        try
        {
            await DownloadVideoAsync(playUrl, tempMp4, progress, ct);

            var start = new ProcessStartInfo { FileName = _ffmpegLocation };
            start.ArgumentList.Add("-y");
            start.ArgumentList.Add("-i");
            start.ArgumentList.Add(tempMp4);
            start.ArgumentList.Add("-vn");
            start.ArgumentList.Add("-c:a");
            start.ArgumentList.Add("libmp3lame");
            start.ArgumentList.Add("-q:a");
            start.ArgumentList.Add("2");
            start.ArgumentList.Add(finalPath);

            var exitCode = await _processRunner.RunAsync(start, null, null, ct);
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"ffmpeg 退出码 {exitCode}");
            }
            if (!File.Exists(finalPath))
            {
                throw new InvalidOperationException("ffmpeg 未生成输出文件");
            }
        }
        finally
        {
            TryDelete(tempMp4);
            TryDelete(tempMp4 + ".part");
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.UserAgent.ParseAdd(MobileUserAgent);
        request.Headers.Referrer = new Uri("https://www.douyin.com/");
        return request;
    }

    private static void CleanupPartial(string finalPath)
    {
        TryDelete(finalPath + ".part");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception)
        {
        }
    }
}
