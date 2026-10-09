using System.Text.Json.Serialization;

namespace DiscordNative.Protocol.Models;

public sealed class DiscordUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("discriminator")]
    public string Discriminator { get; set; } = "0";

    [JsonPropertyName("global_name")]
    public string? GlobalName { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("bot")]
    public bool Bot { get; set; }

    [JsonPropertyName("banner")]
    public string? Banner { get; set; }

    [JsonPropertyName("accent_color")]
    public int? AccentColor { get; set; }

    public string DisplayName => GlobalName ?? Username;

    public string AvatarUrl => Avatar is null
        ? $"https://cdn.discordapp.com/embed/avatars/{(int.TryParse(Discriminator, out var d) ? d % 5 : 0)}.png"
        : $"https://cdn.discordapp.com/avatars/{Id}/{Avatar}.{(Avatar.StartsWith("a_") ? "gif" : "png")}?size=128";

    public string? BannerUrl => Banner is null
        ? null
        : $"https://cdn.discordapp.com/banners/{Id}/{Banner}.{(Banner.StartsWith("a_") ? "gif" : "png")}?size=512";

    public bool HasBanner => Banner is not null;

    public string UserTag => Discriminator == "0" ? $"@{Username}" : $"{Username}#{Discriminator}";
}
