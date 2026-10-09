using System.Text.Json.Serialization;

namespace DiscordNative.Protocol.Models;

public enum DiscordChannelType
{
    GuildText = 0,
    Dm = 1,
    GuildVoice = 2,
    GroupDm = 3,
    GuildCategory = 4,
    GuildAnnouncement = 5,
    AnnouncementThread = 10,
    PublicThread = 11,
    PrivateThread = 12,
    GuildStageVoice = 13,
    GuildForum = 15,
}

public sealed class DiscordChannel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public DiscordChannelType Type { get; set; }

    [JsonPropertyName("guild_id")]
    public string? GuildId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    [JsonPropertyName("position")]
    public int Position { get; set; }

    [JsonPropertyName("recipients")]
    public List<DiscordUser>? Recipients { get; set; }

    [JsonPropertyName("last_message_id")]
    public string? LastMessageId { get; set; }

    public bool IsTextLike => Type is DiscordChannelType.GuildText
        or DiscordChannelType.Dm
        or DiscordChannelType.GroupDm
        or DiscordChannelType.GuildAnnouncement
        or DiscordChannelType.PublicThread
        or DiscordChannelType.PrivateThread
        or DiscordChannelType.AnnouncementThread;

    /// <summary>Nom d'affichage : pour un DM, on utilise le nom du destinataire.</summary>
    public string DisplayName => Type switch
    {
        DiscordChannelType.Dm => Recipients?.FirstOrDefault()?.DisplayName ?? "DM inconnu",
        DiscordChannelType.GroupDm => Name ?? string.Join(", ", Recipients?.Select(r => r.DisplayName) ?? Array.Empty<string>()),
        _ => Name ?? "salon-inconnu",
    };

    /// <summary>URL de l'icône : photo de profil pour un DM.</summary>
    public string? IconUrl => Type switch
    {
        DiscordChannelType.Dm => Recipients?.FirstOrDefault()?.AvatarUrl,
        DiscordChannelType.GroupDm => null, // Discord handles group DM icons separately
        _ => null
    };
}
