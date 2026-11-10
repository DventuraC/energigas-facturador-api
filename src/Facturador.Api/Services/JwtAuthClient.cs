using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Facturador.Api.Services;

public sealed class JwtAuthApiConfiguration
{
    public string Domain { get; set; } = "";
    public string GrantType { get; set; } = "Password";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}

public sealed record JwtLoginRequest(string GrantType, string ClientId, string ClientSecret, string Email, string Password);
public sealed record LoginInput(string Email, string Password);
public sealed record JwtLoginResult(bool IsSuccess, int StatusCode, JwtLoginResponse? Body, string? RawError);
public sealed record JwtLoginResponse(string Status, JwtLoginTokens? Tokens, JwtMfaSetupRequired? Setup, JwtMfaChallenge? Challenge);
public sealed record JwtLoginTokens(bool Success, string Message, string? AccessToken, string? RefreshToken, int? ExpiresIn);
public sealed record JwtMfaSetupRequired(string SetupToken, int ExpiresInSeconds);
public sealed record JwtMfaChallenge(string ChallengeToken, int ExpiresInSeconds);
internal sealed record JwtErrorResponse(string? Message);

public sealed class JwtAuthClient(IOptions<JwtAuthApiConfiguration> config, IHttpClientFactory factory, ILogger<JwtAuthClient> logger)
{
    private readonly JwtAuthApiConfiguration _config = config.Value;

    public async Task<JwtLoginResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var payload = new JwtLoginRequest(_config.GrantType, _config.ClientId, _config.ClientSecret, email, password);
        try
        {
            using var response = await factory.CreateClient("JwtAuthApi").PostAsJsonAsync("api/oauth/login", payload, ct);
            if (!response.IsSuccessStatusCode)
            {
                string? message = null;
                try { message = (await response.Content.ReadFromJsonAsync<JwtErrorResponse>(cancellationToken: ct))?.Message; }
                catch (JsonException) { }
                return new(false, (int)response.StatusCode, null, message ?? "Error de autenticación");
            }

            var body = await response.Content.ReadFromJsonAsync<JwtLoginResponse>(cancellationToken: ct);
            if (body is null || string.IsNullOrWhiteSpace(body.Status))
                return new(false, (int)HttpStatusCode.BadGateway, null, "Respuesta de autenticación inválida");
            return new(true, (int)response.StatusCode, body, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException)
        {
            logger.LogError(ex, "Fallo al contactar el servicio de autenticación");
            return new(false, (int)HttpStatusCode.ServiceUnavailable, null, "Servicio de autenticación no disponible");
        }
    }
}
