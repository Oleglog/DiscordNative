using System.Collections.ObjectModel;
using System.Text.Json;
using DiscordNative.Protocol.Gateway;
using DiscordNative.Protocol.Models;
using DiscordNative.Protocol.Rest;
using Microsoft.Extensions.Logging;

namespace DiscordNative.Protocol;

/// <summary>
/// Point d'entrée unique de la librairie : combine REST + Gateway et maintient
/// un état en mémoire (cache) des guildes/salons/messages, avec des évènements
/// pour que l'UI (WinUI) puisse réagir en temps réel.
/// </summary>
public sealed class DiscordClient : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly string _token;
    private readonly ILoggerFactory? _loggerFactory;
    private DiscordGatewayClient? _gateway;

    public DiscordRestClient Rest { get; }
    public DiscordUser? CurrentUser { get; set; }
    public ObservableCollection<DiscordGuild> Guilds { get; } = new();
    public ObservableCollection<DiscordChannel> PrivateChannels { get; } = new();
    public ObservableCollection<DiscordRelationship> Friends { get; } = new();

    public event Action<DiscordMessage>? MessageReceived;
    public event Action<DiscordMessage>? MessageUpdated;
    public event Action<string, string>? MessageDeleted; // channelId, messageId
    public event Action<string, string, DiscordEmoji>? ReactionAdded; // channelId, messageId, emoji
    public event Action<GatewayConnectionState>? ConnectionStateChanged;

    public DiscordClient(string token, ILoggerFactory? loggerFactory = null)
    {
        _token = token;
        _loggerFactory = loggerFactory;
        Rest = new DiscordRestClient(token);
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        CurrentUser = await Rest.GetCurrentUserAsync(ct);

        try
        {
            var privateChannels = await Rest.GetPrivateChannelsAsync(ct);
            App.RunOnUiThread(() =>
            {
                PrivateChannels.Clear();
                foreach (var c in privateChannels) PrivateChannels.Add(c);
            });
        }
        catch { /* pas critique */ }

        try
        {
            var relationships = await Rest.GetRelationshipsAsync(ct);
            var friends = relationships.Where(r => r.Type == DiscordRelationshipType.Friend).ToList();
            App.RunOnUiThread(() =>
            {
                Friends.Clear();
                foreach (var f in friends) Friends.Add(f);
            });
        }
        catch { /* pas critique si ça échoue */ }

        try
        {
            var guilds = await Rest.GetGuildsAsync(ct);
            if (guilds.Count > 0)
            {
                App.RunOnUiThread(() =>
                {
                    foreach (var g in guilds.OrderBy(g => g.Name))
                    {
                        var existing = Guilds.FirstOrDefault(item => item.Id == g.Id);
                        if (existing != null)
                        {
                            var idx = Guilds.IndexOf(existing);
                            Guilds[idx] = g;
                        }
                        else
                        {
                            Guilds.Add(g);
                        }
                    }
                });
            }
        }
        catch { /* pas critique */ }

        _gateway = new DiscordGatewayClient(_token, _loggerFactory?.CreateLogger<DiscordGatewayClient>());
        _gateway.EventReceived += OnDispatchEvent;
        _gateway.StateChanged += state => ConnectionStateChanged?.Invoke(state);
        await _gateway.ConnectAsync(ct);
    }

    private void OnDispatchEvent(DiscordDispatchEvent evt)
    {
        switch (evt.Name)
        {
            case "READY":
                HandleReady(evt.Data);
                break;
            case "MESSAGE_CREATE":
                var message = evt.Data.Deserialize<DiscordMessage>(JsonOptions);
                if (message is not null) MessageReceived?.Invoke(message);
                break;
            case "MESSAGE_UPDATE":
                var updatedMsg = evt.Data.Deserialize<DiscordMessage>(JsonOptions);
                if (updatedMsg is not null) MessageUpdated?.Invoke(updatedMsg);
                break;
            case "MESSAGE_DELETE":
                if (evt.Data.TryGetProperty("id", out var idProp) && evt.Data.TryGetProperty("channel_id", out var chProp))
                {
                    MessageDeleted?.Invoke(chProp.GetString() ?? "", idProp.GetString() ?? "");
                }
                break;
            case "MESSAGE_REACTION_ADD":
                if (evt.Data.TryGetProperty("channel_id", out var rCh) &&
                    evt.Data.TryGetProperty("message_id", out var rMsg) &&
                    evt.Data.TryGetProperty("emoji", out var emojiEl))
                {
                    var emoji = emojiEl.Deserialize<DiscordEmoji>(JsonOptions);
                    if (emoji is not null) ReactionAdded?.Invoke(rCh.GetString() ?? "", rMsg.GetString() ?? "", emoji);
                }
                break;
            case "GUILD_CREATE":
                var guild = evt.Data.Deserialize<DiscordGuild>(JsonOptions);
                if (guild is not null && !string.IsNullOrEmpty(guild.Name))
                {
                    App.RunOnUiThread(() =>
                    {
                        var existing = Guilds.FirstOrDefault(g => g.Id == guild.Id);
                        if (existing != null)
                        {
                            var idx = Guilds.IndexOf(existing);
                            Guilds[idx] = guild;
                        }
                        else
                        {
                            Guilds.Add(guild);
                        }
                    });
                }
                break;
        }
    }

    private void HandleReady(JsonElement data)
    {
        if (data.TryGetProperty("guilds", out var guildsEl) && guildsEl.ValueKind == JsonValueKind.Array)
        {
            var validGuilds = new List<DiscordGuild>();
            foreach (var gEl in guildsEl.EnumerateArray())
            {
                var g = gEl.Deserialize<DiscordGuild>(JsonOptions);
                if (g is null) continue;
                if (gEl.TryGetProperty("properties", out var props) && props.ValueKind == JsonValueKind.Object)
                {
                    if (string.IsNullOrEmpty(g.Name) && props.TryGetProperty("name", out var n)) g.Name = n.GetString() ?? "";
                    if (g.Icon is null && props.TryGetProperty("icon", out var i) && i.ValueKind == JsonValueKind.String) g.Icon = i.GetString();
                }
                if (!string.IsNullOrEmpty(g.Name))
                {
                    validGuilds.Add(g);
                }
            }

            if (validGuilds.Count > 0)
            {
                App.RunOnUiThread(() =>
                {
                    foreach (var g in validGuilds)
                    {
                        var existing = Guilds.FirstOrDefault(item => item.Id == g.Id);
                        if (existing != null)
                        {
                            var idx = Guilds.IndexOf(existing);
                            Guilds[idx] = g;
                        }
                        else
                        {
                            Guilds.Add(g);
                        }
                    }
                });
            }
        }

        if (data.TryGetProperty("private_channels", out var dmEl))
        {
            var dms = dmEl.Deserialize<List<DiscordChannel>>(JsonOptions) ?? new();
            // Si les destinataires ne sont pas inclus, on garde la liste REST (plus complète).
            if (dms.Count > 0 && dms.All(c => c.Recipients is { Count: > 0 } || c.Type == DiscordChannelType.GroupDm))
            {
                var sorted = dms.OrderByDescending(c => c.LastMessageId?.Length ?? 0).ThenByDescending(c => c.LastMessageId).ToList();
                App.RunOnUiThread(() =>
                {
                    PrivateChannels.Clear();
                    foreach (var c in sorted) PrivateChannels.Add(c);
                });
            }
        }
    }

    public Task JoinVoiceChannelAsync(string guildId, string? channelId, bool mute = false, bool deaf = false)
    {
        if (_gateway is not null) return _gateway.SendVoiceStateUpdateAsync(guildId, channelId, mute, deaf);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        Rest.Dispose();
        if (_gateway is not null) await _gateway.DisposeAsync();
    }
}

/// <summary>
/// Petit hook statique pour marshaller les mises à jour de collections vers le thread UI
/// sans coupler la librairie protocole à WinUI. L'application définit ce délégué au démarrage.
/// </summary>
public static class App
{
    public static Action<Action> RunOnUiThread { get; set; } = action => action();
}


