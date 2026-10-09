using System.Text.Json;
using System.Text.Json.Serialization;

namespace DiscordNative.Protocol.Gateway;

/// <summary>
/// Enveloppe générique de tous les messages échangés sur le WebSocket Gateway Discord.
/// Voir https://discord.com/developers/docs/topics/gateway-events#payload-structure
/// </summary>
public sealed class GatewayPayload
{
    [JsonPropertyName("op")]
    public int Op { get; set; }

    [JsonPropertyName("d")]
    public JsonElement? D { get; set; }

    [JsonPropertyName("s")]
    public int? Sequence { get; set; }

    [JsonPropertyName("t")]
    public string? EventName { get; set; }
}

/// <summary>
/// Codes opcode du Gateway Discord.
/// </summary>
public enum GatewayOpCode
{
    Dispatch = 0,
    Heartbeat = 1,
    Identify = 2,
    PresenceUpdate = 3,
    VoiceStateUpdate = 4,
    Resume = 6,
    Reconnect = 7,
    RequestGuildMembers = 8,
    InvalidSession = 9,
    Hello = 10,
    HeartbeatAck = 11,
}
