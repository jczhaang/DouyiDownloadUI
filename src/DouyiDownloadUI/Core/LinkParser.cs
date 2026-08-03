using System.Text.RegularExpressions;

namespace DouyiDownloadUI.Core;

public static partial class LinkParser
{
    public static string? ExtractUrl(string? shareText)
    {
        if (string.IsNullOrWhiteSpace(shareText)) return null;
        var match = DouyinUrlPattern().Match(shareText);
        if (!match.Success) return null;
        return match.Value.TrimEnd('。', '，', ',', '.', '；', ';', '）', ')', '】', ']', '》', '>');
    }

    [GeneratedRegex(
        @"https?://(?:v\.douyin\.com|www\.douyin\.com)/[^\s\u4e00-\u9fff]+",
        RegexOptions.IgnoreCase)]
    private static partial Regex DouyinUrlPattern();
}
