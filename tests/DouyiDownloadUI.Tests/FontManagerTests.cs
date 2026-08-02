using System.Threading;
using System.Windows.Controls;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class FontManagerTests
{
    [Fact]
    public void Apply_Maps_Names_To_Sizes()
    {
        var failures = new List<Exception>();
        var thread = new Thread(() =>
        {
            try
            {
                var button = new Button();
                FontManager.Apply("Standard", button);
                Assert.Equal(14, button.FontSize);
                FontManager.Apply("Large", button);
                Assert.Equal(18, button.FontSize);
                FontManager.Apply("ExtraLarge", button);
                Assert.Equal(22, button.FontSize);
                FontManager.Apply("未知值", button);
                Assert.Equal(18, button.FontSize);
            }
            catch (Exception ex)
            {
                lock (failures) failures.Add(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failures.Count > 0)
        {
            throw new Xunit.Sdk.XunitException(
                "字体映射断言失败", failures[0]);
        }
    }
}
