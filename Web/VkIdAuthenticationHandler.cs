using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;

namespace WebAPI;

public class VkIdAuthenticationHandler : OAuthHandler<OAuthOptions>
{
    public VkIdAuthenticationHandler(IOptionsMonitor<OAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
    {
        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = context.Code,
            ["client_id"] = Options.ClientId,
            ["redirect_uri"] = context.RedirectUri,
            ["device_id"] = Request.Query["device_id"].ToString(),
            ["state"] = Request.Query["state"].ToString()
        };

        if (context.Properties.Items.TryGetValue(OAuthConstants.CodeVerifierKey, out var codeVerifier))
        {
            parameters[OAuthConstants.CodeVerifierKey] = codeVerifier!;
            context.Properties.Items.Remove(OAuthConstants.CodeVerifierKey);
        }

        if (!string.IsNullOrWhiteSpace(Options.ClientSecret))
            parameters["service_token"] = Options.ClientSecret;

        using var request = new HttpRequestMessage(HttpMethod.Post, Options.TokenEndpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new FormUrlEncodedContent(parameters);
        using var response = await Backchannel.SendAsync(request, Context.RequestAborted);
        var body = await response.Content.ReadAsStringAsync(Context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            return OAuthTokenResponse.Failed(new AuthenticationFailureException(
                $"VK ID token endpoint failure: {(int)response.StatusCode} {body}"));
        }

        return OAuthTokenResponse.Success(JsonDocument.Parse(body));
    }

    protected override async Task<AuthenticationTicket> CreateTicketAsync(
        ClaimsIdentity identity, AuthenticationProperties properties, OAuthTokenResponse tokens)
    {
        var userId = ReadId(tokens.Response?.RootElement);
        string? name = null;

        if (!string.IsNullOrEmpty(tokens.AccessToken))
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, Options.UserInformationEndpoint);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["access_token"] = tokens.AccessToken,
                ["client_id"] = Options.ClientId
            });
            using var response = await Backchannel.SendAsync(request, Context.RequestAborted);
            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Context.RequestAborted));
                if (document.RootElement.TryGetProperty("user", out var user))
                {
                    userId = ReadId(user) ?? userId;
                    name = $"{ReadString(user, "first_name")} {ReadString(user, "last_name")}".Trim();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(userId))
            throw new AuthenticationFailureException("VK ID did not return a user id.");

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));
        if (!string.IsNullOrWhiteSpace(name))
            identity.AddClaim(new Claim(ClaimTypes.Name, name));

        return new AuthenticationTicket(new ClaimsPrincipal(identity), properties, Scheme.Name);
    }

    private static string? ReadId(JsonElement? element)
    {
        if (element == null || element.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return null;
        if (!element.Value.TryGetProperty("user_id", out var id))
            return null;
        return id.ValueKind switch
        {
            JsonValueKind.String => id.GetString(),
            JsonValueKind.Number => id.GetRawText(),
            _ => null
        };
    }

    private static string ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}
