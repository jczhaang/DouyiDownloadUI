using System.Net;
using DouyiDownloadUI.Services;

namespace DouyiDownloadUI.Tests;

public class UpdateCheckerTests
{
    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        public string Json { get; set; } = "{\"tag_name\":\"v1.2.0\"}";
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            var response = new HttpResponseMessage(Status)
            {
                Content = new StringContent(Json)
            };
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task GetLatestVersionAsync_Parses_Tag()
    {
        var handler = new FakeHttpHandler();
        var checker = new UpdateChecker(
            new HttpClient(handler), "user/repo", new Version(1, 0, 0));
        var version = await checker.GetLatestVersionAsync(CancellationToken.None);
        Assert.Equal(new Version(1, 2, 0), version);
    }

    [Fact]
    public async Task GetLatestVersionAsync_HttpError_Returns_Null()
    {
        var handler = new FakeHttpHandler { Status = HttpStatusCode.NotFound };
        var checker = new UpdateChecker(
            new HttpClient(handler), "user/repo", new Version(1, 0, 0));
        Assert.Null(await checker.GetLatestVersionAsync(CancellationToken.None));
    }
}
