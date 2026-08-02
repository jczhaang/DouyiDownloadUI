using System.Diagnostics;
using DouyiDownloadUI.Core;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

internal sealed class FakeProcessRunner : IProcessRunner
{
    public int ExitCode { get; set; }
    public string? StderrLine { get; set; }
    public bool CancelOnRun { get; set; }
    public List<string> StdoutLines { get; } = new();
    public ProcessStartInfo? LastStartInfo { get; private set; }

    public async Task<int> RunAsync(
        ProcessStartInfo startInfo,
        Action<string>? onStdoutLine,
        Action<string>? onStderrLine,
        CancellationToken ct)
    {
        LastStartInfo = startInfo;
        if (CancelOnRun)
        {
            await Task.Delay(50, ct);
            throw new OperationCanceledException(ct);
        }
        if (startInfo.ArgumentList.Contains("--print"))
        {
            onStdoutLine?.Invoke("测试视频标题");
        }
        else
        {
            var args = startInfo.ArgumentList.ToList();
            var outputIndex = args.IndexOf("--output");
            if (outputIndex >= 0 && outputIndex + 1 < args.Count)
            {
                var template = args[outputIndex + 1].Replace("%(ext)s", "mp4");
                Directory.CreateDirectory(Path.GetDirectoryName(template)!);
                File.WriteAllText(template, "fake");
            }
            foreach (var line in StdoutLines) onStdoutLine?.Invoke(line);
        }
        onStderrLine?.Invoke(StderrLine!);
        return ExitCode;
    }
}

internal sealed class FakeEngine : IDownloadEngine
{
    public VideoMetadata? Metadata { get; set; }
    public DownloadResult DownloadResult { get; set; } =
        new(true, null, DownloadErrorKind.None, null);
    public DownloadRequest? LastRequest { get; private set; }

    public Task<VideoMetadata?> GetMetadataAsync(string shareUrl, CancellationToken ct)
        => Task.FromResult(Metadata);

    public Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        LastRequest = request;
        return Task.FromResult(DownloadResult);
    }
}
