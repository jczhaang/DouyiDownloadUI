using System.IO;
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Services;

public static class YtDlpCommandBuilder
{
    public static string[] BuildArguments(DownloadRequest request, string ffmpegLocation)
    {
        var args = new List<string>
        {
            "--no-playlist",
            "--no-overwrites",
            "--newline",
            "--progress-template",
            "download:%(progress._percent_str)s %(progress.speed)s %(progress.eta)s",
            "--output",
            Path.Combine(request.OutputDirectory, request.FileNameWithoutExtension + ".%(ext)s"),
            "--ffmpeg-location",
            ffmpegLocation
        };
        if (request.Mode == DownloadMode.Audio)
        {
            args.Add("--extract-audio");
            args.Add("--audio-format");
            args.Add("mp3");
        }
        args.Add(request.ShareUrl);
        return args.ToArray();
    }
}
