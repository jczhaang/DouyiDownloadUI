namespace DouyiDownloadUI.Core;

public enum DownloadMode { Video, Audio }

public enum DownloadErrorKind
{
    None,
    Network,
    VideoUnavailable,
    SavePathInvalid,
    EngineError,
    Canceled
}

public sealed record VideoMetadata(string Title, bool IsImagePost = false);

public sealed record DownloadRequest(
    string ShareUrl,
    string OutputDirectory,
    string FileNameWithoutExtension,
    DownloadMode Mode);

public sealed record DownloadResult(
    bool Success,
    string? FilePath,
    DownloadErrorKind ErrorKind,
    string? ErrorDetail);

public sealed record DownloadProgress(double Percent, string? Speed, string? Eta);

public sealed record RecentDownloadEntry(
    string FileName,
    string FilePath,
    DateTime DownloadedAt,
    bool IsAudio);

public sealed class AppSettings
{
    public static readonly List<string> DefaultTypeOptions =
        new() { "中三", "中四", "平四", "三步", "其他" };

    public string SaveFolder { get; set; } = "";
    public string FontSize { get; set; } = "Large";
    public int? LastNumber { get; set; }
    public string? LastType { get; set; }
    public List<string> TypeOptions { get; set; } = new(DefaultTypeOptions);
    public List<RecentDownloadEntry> RecentDownloads { get; set; } = new();
}
