using System.Text.Json.Serialization;

namespace DiscordNative.Protocol.Gateway;

/// <summary>
/// Payload IDENTIFY envoyé pour s'authentifier au Gateway avec un token utilisateur.
/// IMPORTANT : se connecter avec un token de compte utilisateur (et non un token "Bot ...")
/// viole les CGU de Discord (self-bot / client non officiel). L'utilisateur de cette librairie
/// porte seul le risque associé (bannissement de compte). Voir discord.com/terms.
/// </summary>
public sealed class IdentifyPayload
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 0 = format READY "classique" (guildes complètes, DMs avec recipients).
    /// Les bits avancés (CLIENT_STATE_V2, DEDUPE_USER_OBJECTS...) changent la forme du payload.
    /// </summary>
    [JsonPropertyName("capabilities")]
    public int Capabilities { get; set; } = 0;

    [JsonPropertyName("properties")]
    public IdentifyProperties Properties { get; set; } = new();

    [JsonPropertyName("compress")]
    public bool Compress { get; set; } = false;

    [JsonPropertyName("presence")]
    public IdentifyPresence? Presence { get; set; }
}

public sealed class IdentifyProperties
{
    [JsonPropertyName("os")]
    public string Os { get; set; } = "Windows";

    [JsonPropertyName("browser")]
    public string Browser { get; set; } = "DiscordNative";

    [JsonPropertyName("device")]
    public string Device { get; set; } = "DiscordNative";
}

public sealed class IdentifyPresence
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "online";

    [JsonPropertyName("since")]
    public long Since { get; set; } = 0;

    [JsonPropertyName("activities")]
    public List<object> Activities { get; set; } = new();

    [JsonPropertyName("afk")]
    public bool Afk { get; set; } = false;
}

public sealed class HelloPayload
{
    [JsonPropertyName("heartbeat_interval")]
    public int HeartbeatInterval { get; set; }
}

public sealed class ResumePayload
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("session_id")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("seq")]
    public int Seq { get; set; }
}
