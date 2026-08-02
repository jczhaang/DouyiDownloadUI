using System.Diagnostics;
using System.IO;

namespace DouyiDownloadUI.Services;

public interface IProcessRunner
{
    Task<int> RunAsync(
        ProcessStartInfo startInfo,
        Action<string>? onStdoutLine,
        Action<string>? onStderrLine,
        CancellationToken ct);
}

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<int> RunAsync(
        ProcessStartInfo startInfo,
        Action<string>? onStdoutLine,
        Action<string>? onStderrLine,
        CancellationToken ct)
    {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("进程启动失败");
        var stdoutTask = ReadLinesAsync(process.StandardOutput, onStdoutLine, ct);
        var stderrTask = ReadLinesAsync(process.StandardError, onStderrLine, ct);
        await Task.WhenAll(stdoutTask, stderrTask);
        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }

    private static async Task ReadLinesAsync(
        StreamReader reader,
        Action<string>? onLine,
        CancellationToken ct)
    {
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            onLine?.Invoke(line);
        }
    }
}
