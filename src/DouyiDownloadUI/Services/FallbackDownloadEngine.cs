using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Services;

public sealed class FallbackDownloadEngine : IDownloadEngine
{
    private readonly IDownloadEngine _primary;
    private readonly IDownloadEngine _fallback;

    public FallbackDownloadEngine(IDownloadEngine primary, IDownloadEngine fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public async Task<VideoMetadata?> GetMetadataAsync(string shareUrl, CancellationToken ct)
    {
        try
        {
            var meta = await _primary.GetMetadataAsync(shareUrl, ct);
            if (meta is not null) return meta;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // 主引擎异常时交给兜底引擎
        }
        return await _fallback.GetMetadataAsync(shareUrl, ct);
    }

    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        DownloadResult result;
        try
        {
            result = await _primary.DownloadAsync(request, progress, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            result = new DownloadResult(false, null, DownloadErrorKind.EngineError, "主引擎异常");
        }
        if (result.Success || result.ErrorKind == DownloadErrorKind.Canceled) return result;
        try
        {
            return await _fallback.DownloadAsync(request, progress, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DownloadResult(
                false, null, DownloadErrorKind.EngineError, $"兜底引擎异常：{ex.Message}");
        }
    }
}
