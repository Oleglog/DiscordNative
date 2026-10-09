using System.Text.Json.Serialization;

namespace DiscordNative.Protocol.Models;

public enum DiscordRelationshipType
{
    None = 0,
    Friend = 1,
    Blocked = 2,
    PendingIncoming = 3,
    PendingOutgoing = 4,
}

public sealed class DiscordRelationship
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public DiscordRelationshipType Type { get; set; }

    [JsonPropertyName("user")]
    public DiscordUser User { get; set; } = new();
}
