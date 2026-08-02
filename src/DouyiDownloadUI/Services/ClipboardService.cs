namespace DouyiDownloadUI.Services;

public interface IClipboardService
{
    string? GetText();
}

public sealed class ClipboardService : IClipboardService
{
    public string? GetText()
    {
        try
        {
            return System.Windows.Clipboard.ContainsText()
                ? System.Windows.Clipboard.GetText()
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
