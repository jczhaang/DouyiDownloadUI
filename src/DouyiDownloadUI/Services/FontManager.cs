using System.Windows;
using System.Windows.Controls;

namespace DouyiDownloadUI.Services;

public static class FontManager
{
    public static void Apply(string fontSize, Control root)
    {
        root.FontSize = fontSize switch
        {
            "Standard" => 14,
            "ExtraLarge" => 22,
            _ => 18
        };
    }
}
