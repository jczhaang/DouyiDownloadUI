using System.Text.RegularExpressions;

namespace DouyiDownloadUI.Core;

public sealed record DouyinShareInfo(string? Title, string? PlayUrl, bool IsImagePost);

public static partial class DouyinShareParser
{
    public static string? ExtractVideoId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var match = VideoIdPattern().Match(url);
        return match.Success ? match.Groups["id"].Value : null;
    }

    public static DouyinShareInfo? Parse(string html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var playMatch = PlayUrlPattern().Match(html);
        if (!playMatch.Success) return null;
        var playUrl = Unescape(playMatch.Groups["url"].Value);
        var descMatch = DescPattern().Match(html);
        var title = descMatch.Success ? Unescape(descMatch.Groups["desc"].Value).Trim() : null;

        var typeMatch = AwemeTypePattern().Match(html);
        var isImagePost = typeMatch.Success && typeMatch.Groups["type"].Value == "2";

        if (isImagePost)
        {
            var uriMatch = PlayUriPattern().Match(html);
            if (uriMatch.Success)
            {
                playUrl = Unescape(uriMatch.Groups["uri"].Value);
            }
        }

        return new DouyinShareInfo(title, playUrl, isImagePost);
    }

    private static string Unescape(string value)
    {
        if (!value.Contains('\\')) return value;
        return UnicodeEscapePattern().Replace(
            value,
            m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
    }

    [GeneratedRegex(@"(?:modal_id=|/video/|/share/video/)(?<id>\d+)")]
    private static partial Regex VideoIdPattern();

    [GeneratedRegex(@"""play_addr"":\{[^}]*?""url_list"":\[""(?<url>https?:[^""]+)""")]
    private static partial Regex PlayUrlPattern();

    [GeneratedRegex(@"""play_addr"":\{[^}]*?""uri"":""(?<uri>[^""]+)""")]
    private static partial Regex PlayUriPattern();

    [GeneratedRegex(@"""aweme_type"":(?<type>\d+)")]
    private static partial Regex AwemeTypePattern();

    [GeneratedRegex(@"""desc"":""(?<desc>[^""]*)""")]
    private static partial Regex DescPattern();

    [GeneratedRegex(@"\\u([0-9a-fA-F]{4})")]
    private static partial Regex UnicodeEscapePattern();
}
