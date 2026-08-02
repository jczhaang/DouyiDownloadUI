using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DouyiDownloadUI.Core;

public static partial class FilenameBuilder
{
    public const int MaxTitleLength = 30;
    private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars();

    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";
        var cleaned = new string(
            input.Select(ch => InvalidChars.Contains(ch) || char.IsControl(ch) ? ' ' : ch).ToArray());
        return WhitespacePattern().Replace(cleaned, " ").Trim();
    }

    public static string Truncate(string text)
    {
        if (text.Length <= MaxTitleLength) return text;
        return text[..MaxTitleLength] + "…";
    }

    public static string BuildFileName(string number, string type, string title, string extension)
    {
        var parts = new[] { Sanitize(number), Sanitize(type), Truncate(Sanitize(title)) }
            .Where(p => p.Length > 0);
        var name = string.Join(" ", parts);
        if (name.Length == 0) name = "未命名";
        var ext = extension.StartsWith('.') ? extension : "." + extension;
        return name + ext.ToLowerInvariant();
    }

    public static string MakeUnique(string directory, string fileNameWithoutExtension, string extension)
    {
        var ext = extension.StartsWith('.') ? extension : "." + extension;
        var candidate = fileNameWithoutExtension + ext;
        if (!File.Exists(Path.Combine(directory, candidate))) return candidate;
        for (var i = 2; ; i++)
        {
            candidate = $"{fileNameWithoutExtension}（{i}）{ext}";
            if (!File.Exists(Path.Combine(directory, candidate))) return candidate;
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
