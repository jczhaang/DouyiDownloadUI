using System.Globalization;
using System.Windows.Data;

namespace DouyiDownloadUI.Converters;

public sealed class RelativeTimeConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DateTime time) return "";
        var delta = DateTime.Now - time;
        if (delta.TotalMinutes < 1) return "刚刚";
        if (delta.TotalHours < 1) return $"{(int)delta.TotalMinutes} 分钟前";
        if (delta.TotalDays < 1) return $"{(int)delta.TotalHours} 小时前";
        return time.ToString("M月d日");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
