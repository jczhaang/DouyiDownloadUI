using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DouyiDownloadUI.Services;

public sealed class UpdateChecker
{
    private readonly HttpClient _httpClient;
    private readonly string _repo;

    public Version CurrentVersion { get; }

    public UpdateChecker(HttpClient httpClient, string repo, Version currentVersion)
    {
        _httpClient = httpClient;
        _repo = repo;
        CurrentVersion = currentVersion;
    }

    public async Task<Version?> GetLatestVersionAsync(CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.github.com/repos/{_repo}/releases/latest");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("DouyiDownloadUI", "1.0"));
            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var tag = doc.RootElement.GetProperty("tag_name").GetString();
            return Version.TryParse(tag?.TrimStart('v'), out var version)
                ? version
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
