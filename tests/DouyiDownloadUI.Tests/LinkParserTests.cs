using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Tests;

public class LinkParserTests
{
    private const string ShareText =
        "2.82 02/29 O@x.Sy :7pm aaN:/ 什么是Node.js https://v.douyin.com/h94R-IulXc8/ 复制此链接，打开Dou音搜索，直接观看视频！";

    [Fact]
    public void ExtractUrl_From_ShareText_Returns_ShortUrl()
    {
        var url = LinkParser.ExtractUrl(ShareText);
        Assert.Equal("https://v.douyin.com/h94R-IulXc8/", url);
    }

    [Fact]
    public void ExtractUrl_From_LongUrl_Returns_LongUrl()
    {
        const string text = "看看这个 https://www.douyin.com/video/6914948781100338440 怎么样";
        Assert.Equal("https://www.douyin.com/video/6914948781100338440", LinkParser.ExtractUrl(text));
    }

    [Theory]
    [InlineData("今天天气不错")]
    [InlineData("")]
    [InlineData(null)]
    public void ExtractUrl_Without_Url_Returns_Null(string? text)
    {
        Assert.Null(LinkParser.ExtractUrl(text));
    }
}
