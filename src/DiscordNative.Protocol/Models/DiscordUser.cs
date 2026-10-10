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

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    public string DisplayName => GlobalName ?? Username;

    public bool IsAnimatedAvatar => Avatar is not null && Avatar.StartsWith("a_");
    public bool IsAnimatedBanner => Banner is not null && Banner.StartsWith("a_");

    public string StaticAvatarUrl => Avatar is null
        ? $"https://cdn.discordapp.com/embed/avatars/{(int.TryParse(Discriminator, out var d) ? d % 5 : 0)}.png"
        : $"https://cdn.discordapp.com/avatars/{Id}/{Avatar}.png?size=128";

    public string AnimatedAvatarUrl => Avatar is null
        ? StaticAvatarUrl
        : $"https://cdn.discordapp.com/avatars/{Id}/{Avatar}.{(Avatar.StartsWith("a_") ? "gif" : "png")}?size=128";

    public string AvatarUrl => AnimatedAvatarUrl;

    public string? StaticBannerUrl => Banner is null
        ? null
        : $"https://cdn.discordapp.com/banners/{Id}/{Banner}.png?size=512";

    public string? AnimatedBannerUrl => Banner is null
        ? null
        : $"https://cdn.discordapp.com/banners/{Id}/{Banner}.{(Banner.StartsWith("a_") ? "gif" : "png")}?size=512";

    public string? BannerUrl => Banner is null
        ? null
        : $"https://cdn.discordapp.com/banners/{Id}/{Banner}.{(Banner.StartsWith("a_") ? "gif" : "png")}?size=512";

    public bool HasBanner => Banner is not null;

    public string AboutMe => string.IsNullOrWhiteSpace(Bio) ? "Grand voyageur de salons Discord." : Bio;

    public string UserTag => Discriminator == "0" ? $"@{Username}" : $"{Username}#{Discriminator}";

    public string Status { get; set; } = "online";

    public string StatusColor => Status switch
    {
        "online" => "#23A55A",
        "idle" => "#F0B232",
        "dnd" => "#F23F43",
        _ => "#80848E"
    };
}
