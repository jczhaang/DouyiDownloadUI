using System.Diagnostics;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class ProcessRunnerTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "dyui-proc-" + Guid.NewGuid().ToString("N"));

    public ProcessRunnerTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public async Task RunAsync_Cancel_Kills_Child_Process()
    {
        var pidFile = Path.Combine(_dir, "child.pid");
        var start = new ProcessStartInfo("powershell");
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-Command");
        start.ArgumentList.Add(
            $"$PID | Set-Content -LiteralPath '{pidFile}'; Start-Sleep -Seconds 300");

        var runner = new ProcessRunner();
        using var cts = new CancellationTokenSource();
        var runTask = runner.RunAsync(start, null, null, cts.Token);

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!File.Exists(pidFile) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(50);
        }
        Assert.True(File.Exists(pidFile), "子进程未能启动并写出 PID 文件");

        var pid = int.Parse(File.ReadAllText(pidFile));
        try
        {
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

            var exited = false;
            var exitDeadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < exitDeadline)
            {
                try
                {
                    using var proc = Process.GetProcessById(pid);
                    if (proc.HasExited)
                    {
                        exited = true;
                        break;
                    }
                }
                catch (ArgumentException)
                {
                    exited = true;
                    break;
                }
                await Task.Delay(100);
            }
            Assert.True(exited, $"子进程 {pid} 在取消后仍存活");
        }
        finally
        {
            try
            {
                using var proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill(entireProcessTree: true);
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
