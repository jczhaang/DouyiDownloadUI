using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class ProgressParserTests
{
    [Fact]
    public void ParseLine_Full_Line()
    {
        var p = ProgressParser.ParseLine("download:45.6% 1.2MiB/s 00:05");
        Assert.NotNull(p);
        Assert.Equal(45.6, p!.Percent);
        Assert.Equal("1.2MiB/s", p.Speed);
        Assert.Equal("00:05", p.Eta);
    }

    [Fact]
    public void ParseLine_No_Speed_Or_Eta()
    {
        var p = ProgressParser.ParseLine("download:100.0%");
        Assert.NotNull(p);
        Assert.Equal(100.0, p!.Percent);
        Assert.Null(p.Speed);
        Assert.Null(p.Eta);
    }

    [Theory]
    [InlineData("[download] Destination: C:\\x\\y.mp4")]
    [InlineData("")]
    [InlineData(null)]
    public void ParseLine_NonProgress_Returns_Null(string? line)
    {
        Assert.Null(ProgressParser.ParseLine(line));
    }
}
