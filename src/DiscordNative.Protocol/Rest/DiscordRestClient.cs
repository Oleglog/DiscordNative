using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DiscordNative.Protocol.Models;

namespace DiscordNative.Protocol.Rest;

/// <summary>
/// Client REST minimal pour les opérations courantes (lister guildes/salons/messages, envoyer un message).
/// Utilise le même en-tête Authorization brut (sans préfixe "Bot ") que le client officiel pour un token utilisateur.
/// </summary>
public sealed class DiscordRestClient : IDisposable
{
    private const string ApiBase = "https://discord.com/api/v9";
    private readonly HttpClient _http;

    public DiscordRestClient(string token)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36");
        // Pas de préfixe "Bot" : c'est un token de compte utilisateur.
        _http.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public Task<DiscordUser?> GetCurrentUserAsync(CancellationToken ct = default) =>
        _http.GetFromJsonAsync<DiscordUser>($"{ApiBase}/users/@me", ct);

    public async Task<DiscordUser> ModifyCurrentUserAsync(string? globalName = null, string? username = null, string? bio = null, CancellationToken ct = default)
    {
        var body = new Dictionary<string, object?>();
        if (globalName is not null) body["global_name"] = globalName;
        if (username is not null) body["username"] = username;
        if (bio is not null) body["bio"] = bio;

        var response = await _http.PatchAsJsonAsync($"{ApiBase}/users/@me", body, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DiscordUser>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Réponse de mise à jour du profil illisible.");
    }

    public async Task<List<DiscordGuild>> GetGuildsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<DiscordGuild>>($"{ApiBase}/users/@me/guilds", ct) ?? new();

    public async Task<List<DiscordChannel>> GetGuildChannelsAsync(string guildId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<DiscordChannel>>($"{ApiBase}/guilds/{guildId}/channels", ct) ?? new();

    public async Task<List<DiscordChannel>> GetPrivateChannelsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<DiscordChannel>>($"{ApiBase}/users/@me/channels", ct) ?? new();

    public async Task<List<DiscordRelationship>> GetRelationshipsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<DiscordRelationship>>($"{ApiBase}/users/@me/relationships", ct) ?? new();

    public async Task<List<DiscordMessage>> GetMessagesAsync(string channelId, int limit = 50, string? beforeId = null, CancellationToken ct = default)
    {
        var url = $"{ApiBase}/channels/{channelId}/messages?limit={limit}";
        if (beforeId is not null) url += $"&before={beforeId}";
        return await _http.GetFromJsonAsync<List<DiscordMessage>>(url, ct) ?? new();
    }

    public async Task<DiscordMessage> SendMessageAsync(string channelId, string content, string? nonce = null, CancellationToken ct = default)
    {
        var payload = new SendMessageRequest
        {
            Content = content,
            Nonce = nonce ?? Guid.NewGuid().ToString("N"),
        };
        var response = await _http.PostAsJsonAsync($"{ApiBase}/channels/{channelId}/messages", payload, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DiscordMessage>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Réponse d'envoi de message illisible.");
    }

    public async Task AddReactionAsync(string channelId, string messageId, string emoji, CancellationToken ct = default)
    {
        var encoded = Uri.EscapeDataString(emoji);
        var response = await _http.PutAsync($"{ApiBase}/channels/{channelId}/messages/{messageId}/reactions/{encoded}/@me", content: null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveReactionAsync(string channelId, string messageId, string emoji, CancellationToken ct = default)
    {
        var encoded = Uri.EscapeDataString(emoji);
        var response = await _http.DeleteAsync($"{ApiBase}/channels/{channelId}/messages/{messageId}/reactions/{encoded}/@me", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<DiscordMessage> EditMessageAsync(string channelId, string messageId, string content, CancellationToken ct = default)
    {
        var payload = new { content };
        var response = await _http.PatchAsJsonAsync($"{ApiBase}/channels/{channelId}/messages/{messageId}", payload, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DiscordMessage>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Réponse d'édition illisible.");
    }

    public async Task<DiscordMessage> SendFileAsync(string channelId, string filePath, string? content = null, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        if (!string.IsNullOrEmpty(content))
        {
            form.Add(new StringContent(content), "content");
        }
        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", System.IO.Path.GetFileName(filePath));

        var response = await _http.PostAsync($"{ApiBase}/channels/{channelId}/messages", form, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DiscordMessage>(cancellationToken: ct)
               ?? throw new InvalidOperationException("Réponse d'envoi de fichier illisible.");
    }

    public async Task DeleteMessageAsync(string channelId, string messageId, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"{ApiBase}/channels/{channelId}/messages/{messageId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task TriggerTypingAsync(string channelId, CancellationToken ct = default) =>
        await _http.PostAsync($"{ApiBase}/channels/{channelId}/typing", content: null, ct);

    public void Dispose() => _http.Dispose();

    private sealed class SendMessageRequest
    {
        [JsonPropertyName("content")] public string Content { get; set; } = string.Empty;
        [JsonPropertyName("nonce")] public string Nonce { get; set; } = string.Empty;
        [JsonPropertyName("tts")] public bool Tts { get; set; } = false;
    }
}

