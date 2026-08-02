using System.IO;

namespace DouyiDownloadUI.Core;

public static class NumberingService
{
    public static int GetMaxNumberInFolder(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return 0;
        var max = 0;
        foreach (var file in Directory.EnumerateFiles(folderPath))
        {
            var name = Path.GetFileName(file);
            var i = 0;
            while (i < name.Length && char.IsDigit(name[i])) i++;
            if (i is 0 or > 5) continue;
            if (int.TryParse(name[..i], out var n) && n > max) max = n;
        }
        return max;
    }

    public static int GetDefaultNumber(string folderPath, int? lastUsedNumber)
    {
        var folderMax = GetMaxNumberInFolder(folderPath);
        if (folderMax > 0) return folderMax + 1;
        if (lastUsedNumber is int last && last > 0) return last + 1;
        return 1;
    }
}
