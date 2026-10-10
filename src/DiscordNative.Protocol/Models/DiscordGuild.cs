using System.Text.Json.Serialization;

namespace DiscordNative.Protocol.Models;

public sealed class DiscordGuild
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }

    [JsonPropertyName("channels")]
    public List<DiscordChannel> Channels { get; set; } = new();

    [JsonPropertyName("roles")]
    public List<DiscordRole> Roles { get; set; } = new();

    public bool IsAnimated => Icon is not null && Icon.StartsWith("a_");

    public string? StaticIconUrl => Icon is null
        ? null
        : $"https://cdn.discordapp.com/icons/{Id}/{Icon}.png?size=128";

    public string? AnimatedIconUrl => Icon is null
        ? null
        : $"https://cdn.discordapp.com/icons/{Id}/{Icon}.{(Icon.StartsWith("a_") ? "gif" : "png")}?size=128";

    public string? IconUrl => AnimatedIconUrl;

    public bool HasIcon => Icon is not null;
    public bool ShowInitials => Icon is null;

    /// <summary>Initiales affichées quand il n'y a pas d'icône (comme le client officiel).</summary>
    public string Initials
    {
        get
        {
            var words = Name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return words.Length switch
            {
                0 => "?",
                1 => words[0].Length >= 2 ? words[0][..2].ToUpperInvariant() : words[0].ToUpperInvariant(),
                _ => string.Concat(words.Take(3).Select(w => w[0])).ToUpperInvariant(),
            };
        }
    }
}

public sealed class DiscordRole
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("color")]
    public int Color { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }
}
