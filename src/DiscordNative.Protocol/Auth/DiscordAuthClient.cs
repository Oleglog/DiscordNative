using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DiscordNative.Protocol.Auth;

/// <summary>
/// Reproduit le flow de login du site web Discord (POST /auth/login puis, si demandé,
/// POST /auth/mfa/totp) pour récupérer un token de compte utilisateur.
///
/// ATTENTION LÉGALE : utiliser ce token pour piloter un client tiers (non officiel) sur
/// le Gateway Discord viole les conditions d'utilisation de Discord (self-bot / client
/// non autorisé) et peut entraîner un bannissement du compte. Cette librairie ne fait que
/// reproduire un appel HTTP public utilisé par le site officiel ; l'usage qui en est fait
/// relève entièrement de la responsabilité de l'utilisateur final.
/// </summary>
public sealed class DiscordAuthClient : IDisposable
{
    private const string ApiBase = "https://discord.com/api/v9";
    private readonly HttpClient _http;

    public DiscordAuthClient()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0 Safari/537.36");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<LoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{ApiBase}/auth/login",
            new LoginRequest { Login = email, Password = password }, ct);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct)
                   ?? throw new DiscordAuthException("Réponse de login illisible.");

        if (!response.IsSuccessStatusCode)
        {
            throw new DiscordAuthException(body.Message ?? "Échec de connexion (identifiants invalides ?).");
        }

        if (body.Mfa == true)
        {
            return LoginResult.RequiresMfa(body.Ticket ?? throw new DiscordAuthException("Ticket MFA manquant."));
        }

        return LoginResult.Success(body.Token ?? throw new DiscordAuthException("Token manquant dans la réponse."));
    }

    public async Task<LoginResult> SubmitTotpAsync(string ticket, string code, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{ApiBase}/auth/mfa/totp",
            new TotpRequest { Ticket = ticket, Code = code }, ct);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: ct)
                   ?? throw new DiscordAuthException("Réponse MFA illisible.");

        if (!response.IsSuccessStatusCode)
        {
            throw new DiscordAuthException(body.Message ?? "Code 2FA invalide.");
        }

        return LoginResult.Success(body.Token ?? throw new DiscordAuthException("Token manquant après MFA."));
    }

    public void Dispose() => _http.Dispose();

    private sealed class LoginRequest
    {
        [JsonPropertyName("login")] public string Login { get; set; } = string.Empty;
        [JsonPropertyName("password")] public string Password { get; set; } = string.Empty;
        [JsonPropertyName("undelete")] public bool Undelete { get; set; } = false;
        [JsonPropertyName("login_source")] public string? LoginSource { get; set; } = null;
        [JsonPropertyName("gift_code_sku_id")] public string? GiftCodeSkuId { get; set; } = null;
    }

    private sealed class TotpRequest
    {
        [JsonPropertyName("code")] public string Code { get; set; } = string.Empty;
        [JsonPropertyName("ticket")] public string Ticket { get; set; } = string.Empty;
        [JsonPropertyName("gift_code_sku_id")] public string? GiftCodeSkuId { get; set; } = null;
        [JsonPropertyName("login_source")] public string? LoginSource { get; set; } = null;
    }

    private sealed class LoginResponse
    {
        [JsonPropertyName("token")] public string? Token { get; set; }
        [JsonPropertyName("mfa")] public bool? Mfa { get; set; }
        [JsonPropertyName("ticket")] public string? Ticket { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("sms")] public bool? Sms { get; set; }
    }
}

public sealed class LoginResult
{
    public bool NeedsMfa { get; private init; }
    public string? MfaTicket { get; private init; }
    public string? Token { get; private init; }

    public static LoginResult RequiresMfa(string ticket) => new() { NeedsMfa = true, MfaTicket = ticket };
    public static LoginResult Success(string token) => new() { NeedsMfa = false, Token = token };
}

public sealed class DiscordAuthException : Exception
{
    public DiscordAuthException(string message) : base(message) { }
}
