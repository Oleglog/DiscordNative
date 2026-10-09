using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.ApplicationModel;

namespace DiscordNative.App.Services;

public sealed class UpdateInfo
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }
}

public static class UpdateService
{
    private const string RepoOwner = "djell1l";
    private const string RepoName = "DiscordNative";
    private static readonly HttpClient Http = new();

    public static async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases");
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("DiscordNative", "1.0"));

            var resp = await Http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<List<UpdateInfo>>(json);
            if (releases is null || releases.Count == 0) return null;

            var latest = releases[0];
            var currentVersion = "v0.2.0";

            if (latest.TagName != currentVersion && !latest.TagName.Equals(currentVersion + "-exp", StringComparison.OrdinalIgnoreCase))
            {
                return latest;
            }
        }
        catch { }

        return null;
    }
}
