using System.Diagnostics;
using System.IO;
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Services;

public interface IDownloadEngine
{
    Task<VideoMetadata?> GetMetadataAsync(string shareUrl, CancellationToken ct);
    Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct);
}

public sealed class YtDlpEngine : IDownloadEngine
{
    private readonly string _ytDlpPath;
    private readonly string _ffmpegLocation;
    private readonly IProcessRunner _processRunner;

    public YtDlpEngine(string ytDlpPath, string ffmpegLocation, IProcessRunner? processRunner = null)
    {
        _ytDlpPath = ytDlpPath;
        _ffmpegLocation = ffmpegLocation;
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public async Task<VideoMetadata?> GetMetadataAsync(string shareUrl, CancellationToken ct)
    {
        var start = CreateStartInfo(
            "--no-playlist", "--skip-download", "--print", "title", shareUrl);
        string? title = null;
        var exitCode = await _processRunner.RunAsync(
            start,
            line => title ??= string.IsNullOrWhiteSpace(line) ? null : line,
            null,
            ct);
        return exitCode == 0 && !string.IsNullOrWhiteSpace(title)
            ? new VideoMetadata(title)
            : null;
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

        var ext = request.Mode == DownloadMode.Audio ? "mp3" : "mp4";
        var unique = FilenameBuilder.MakeUnique(
            request.OutputDirectory, request.FileNameWithoutExtension, ext);
        var safeRequest = request with
        {
            FileNameWithoutExtension = Path.GetFileNameWithoutExtension(unique)
        };

        var start = CreateStartInfo(
            YtDlpCommandBuilder.BuildArguments(safeRequest, _ffmpegLocation));
        string? stderrTail = null;
        int exitCode;
        try
        {
            exitCode = await _processRunner.RunAsync(
                start,
                line =>
                {
                    var p = ProgressParser.ParseLine(line);
                    if (p is not null) progress?.Report(p);
                },
                line => stderrTail = line,
                ct);
        }
        catch (OperationCanceledException)
        {
            CleanupPartial(safeRequest);
            return new DownloadResult(false, null, DownloadErrorKind.Canceled, "已取消");
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return new DownloadResult(
                false, null, DownloadErrorKind.EngineError, $"yt-dlp 启动失败：{ex.Message}");
        }
        catch (Exception ex)
        {
            return new DownloadResult(
                false, null, DownloadErrorKind.EngineError, $"yt-dlp 异常：{ex.Message}");
        }

        if (exitCode != 0)
        {
            return MapError(stderrTail ?? $"退出码 {exitCode}");
        }

        var file = FindOutputFile(safeRequest);
        return file is null
            ? new DownloadResult(false, null, DownloadErrorKind.EngineError, "未找到输出文件")
            : new DownloadResult(true, file, DownloadErrorKind.None, null);
    }

    private ProcessStartInfo CreateStartInfo(params string[] arguments)
    {
        var start = new ProcessStartInfo { FileName = _ytDlpPath };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return start;
    }

    private static DownloadResult MapError(string stderr)
    {
        if (stderr.Contains("Video unavailable", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("Unsupported URL", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("Private video", StringComparison.OrdinalIgnoreCase))
        {
            return new DownloadResult(false, null, DownloadErrorKind.VideoUnavailable, stderr);
        }
        if (stderr.Contains("Unable to download webpage", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("timed out", StringComparison.OrdinalIgnoreCase)
            || stderr.Contains("Connection", StringComparison.OrdinalIgnoreCase))
        {
            return new DownloadResult(false, null, DownloadErrorKind.Network, stderr);
        }
        return new DownloadResult(false, null, DownloadErrorKind.EngineError, stderr);
    }

    private static void CleanupPartial(DownloadRequest request)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(
                         request.OutputDirectory, request.FileNameWithoutExtension + ".*"))
            {
                if (file.EndsWith(".part", StringComparison.OrdinalIgnoreCase)
                    || file.EndsWith(".ytdl", StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception)
        {
            // 清理失败不阻塞主流程
        }
    }

    private static string? FindOutputFile(DownloadRequest request)
    {
        foreach (var file in Directory.EnumerateFiles(request.OutputDirectory))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (string.Equals(name, request.FileNameWithoutExtension, StringComparison.Ordinal))
            {
                return file;
            }
        }
        return null;
    }
}
