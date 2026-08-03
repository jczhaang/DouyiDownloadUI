using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Tests;

public class NumberingServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "dyui-num-" + Guid.NewGuid().ToString("N"));

    public NumberingServiceTests() => Directory.CreateDirectory(_dir);

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void GetDefaultNumber_FolderMax_Plus_One()
    {
        File.WriteAllText(Path.Combine(_dir, "007 中三 舞.mp4"), "x");
        File.WriteAllText(Path.Combine(_dir, "003 平四 舞.mp4"), "x");
        File.WriteAllText(Path.Combine(_dir, "随意文件.txt"), "x");
        Assert.Equal(8, NumberingService.GetDefaultNumber(_dir, null));
    }

    [Fact]
    public void GetDefaultNumber_EmptyFolder_Uses_LastUsed_Plus_One()
    {
        Assert.Equal(11, NumberingService.GetDefaultNumber(_dir, 10));
    }

    [Fact]
    public void GetDefaultNumber_EmptyFolder_NoMemory_Returns_One()
    {
        Assert.Equal(1, NumberingService.GetDefaultNumber(_dir, null));
    }

    [Fact]
    public void GetDefaultNumber_Ignores_Long_Digit_Runs()
    {
        File.WriteAllText(Path.Combine(_dir, "123456 标题.mp4"), "x");
        Assert.Equal(1, NumberingService.GetDefaultNumber(_dir, null));
    }
}
