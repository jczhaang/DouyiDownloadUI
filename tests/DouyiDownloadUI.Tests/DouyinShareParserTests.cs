using DouyiDownloadUI.Core;

namespace DouyiDownloadUI.Tests;

public class DouyinShareParserTests
{
    private const string ShareHtml = """
        <html><head><script>
        window._ROUTER_DATA = {"loaderData":{"video_(id)\u002Fpage":{"ua":"Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X)","videoInfoRes":{"item_list":[{"desc":"什么是Node.js 这期视频","video":{"play_addr":{"uri":"v0200fg10000d8uuhinog65i77g243bg","url_list":["https:\u002F\u002Faweme.snssdk.com\u002Faweme\u002Fv1\u002Fplaywm\u002F?line=0&logo_name=aweme_diversion_search&ratio=720p&video_id=v0200fg10000d8uuhinog65i77g243bg"]}}}}]}}};
        </script></head></html>
        """;

    [Fact]
    public void Parse_Share_Page_Returns_Title_And_Play_Url()
    {
        var info = DouyinShareParser.Parse(ShareHtml);

        Assert.NotNull(info);
        Assert.Equal("什么是Node.js 这期视频", info!.Title);
        Assert.Equal(
            "https://aweme.snssdk.com/aweme/v1/playwm/?line=0&logo_name=aweme_diversion_search&ratio=720p&video_id=v0200fg10000d8uuhinog65i77g243bg",
            info.PlayUrl);
    }

    [Fact]
    public void Parse_Page_Without_Play_Addr_Returns_Null()
    {
        Assert.Null(DouyinShareParser.Parse("<html><body>没有视频数据</body></html>"));
    }

    [Theory]
    [InlineData("https://www.douyin.com/jingxuan/search/%E6%A1%82%E7%8E%B2?modal_id=7244187005528132901&type=general", "7244187005528132901")]
    [InlineData("https://www.douyin.com/video/7655535289644928298?previous_page=web_code_link", "7655535289644928298")]
    [InlineData("https://www.iesdouyin.com/share/video/7655535289644928298", "7655535289644928298")]
    public void ExtractVideoId_From_Url_Returns_Id(string url, string expected)
    {
        Assert.Equal(expected, DouyinShareParser.ExtractVideoId(url));
    }

    [Theory]
    [InlineData("https://v.douyin.com/h94R-IulXc8/")]
    [InlineData("https://example.com/no/video/here")]
    [InlineData("")]
    [InlineData(null)]
    public void ExtractVideoId_Without_Id_Returns_Null(string? url)
    {
        Assert.Null(DouyinShareParser.ExtractVideoId(url));
    }
}
