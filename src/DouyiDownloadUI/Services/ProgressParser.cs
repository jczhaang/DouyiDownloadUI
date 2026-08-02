using System.Globalization;
using System.Text.RegularExpressions;
using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Services;

public static partial class ProgressParser
{
    public static DownloadProgress? ParseLine(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        var match = ProgressLinePattern().Match(line);
        if (!match.Success) return null;
        var percent = double.Parse(match.Groups["percent"].Value, CultureInfo.InvariantCulture);
        var speed = match.Groups["speed"].Success ? match.Groups["speed"].Value : null;
        var eta = match.Groups["eta"].Success ? match.Groups["eta"].Value : null;
        return new DownloadProgress(percent, speed, eta);
    }

    [GeneratedRegex(@"^download:(?<percent>\d+(?:\.\d+)?)%(?: (?<speed>[^ ]+))?(?: (?<eta>\S+))?$")]
    private static partial Regex ProgressLinePattern();
}
