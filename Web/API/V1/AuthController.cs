using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using authorization;
using DbConnection;
using ImageService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI.API.V1;

public class LoginData
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class RegisterData
{
    public string Username { get; set; }
    public string Password { get; set; }
}

public class User
{
    public string Id { get; set; }
    public string Username { get; set; }
    public string AvatarHash { get; set; }
}

public class AvatarData
{
    public string Hash { get; set; }
}
[ApiController]
[Route("api/v1/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILoginService _loginService;
    private readonly IRegistrationService _registrationService;
    private readonly IGetUser _getUser;
    private readonly IAuthenticationSchemeProvider _schemes;
    private readonly IRepository<ImageModel> _images;
    public AuthController(IConfiguration configuration, ILoginService loginService,
        IRegistrationService registrationService, IGetUser getUser, IAuthenticationSchemeProvider schemes,
        IRepository<ImageModel> images)
    {
        _configuration = configuration;
        _loginService = loginService;
        _registrationService = registrationService;
        _getUser = getUser;
        _schemes = schemes;
        _images = images;
    }

    [HttpPost("login")]
    public ActionResult<string> Post([FromBody] LoginData data)
    {
        var res = _loginService.Login(new authorization.LoginData(data.Username, data.Password));
        if (res == null)
        {
            return Unauthorized("Bad username or password");
        }
        return Ok(GenerateJwtToken(res.Id.ToString()));
    }


    [HttpPost("register")]
    public ActionResult<string> Post([FromBody] RegisterData data)
    {
        var res = _registrationService.Register(new authorization.RegistrationData(data.Username, data.Password));
        switch (res)
        {
            case Status.UsernameExists:
                return Conflict("Username already exists");
            case Status.Error:
            {
                return BadRequest();
            }
            case Status.Ok:
            {
                var user = _loginService.Login(new authorization.LoginData(data.Username, data.Password));
                return user == null ? BadRequest() : Ok(GenerateJwtToken(user.Id.ToString()));
            }
            default:
            {
                return BadRequest();
            }
        }
    }

    [HttpGet("providers")]
    public async Task<ActionResult<string[]>> GetProviders()
    {
        var schemes = await _schemes.GetAllSchemesAsync();
        return Ok(schemes.Select(scheme => scheme.Name)
            .Where(name => ExternalAuthentication.Providers.Contains(name)).ToArray());
    }

    [HttpGet("login/{provider}")]
    public async Task<IActionResult> ChallengeProvider(string provider, [FromQuery] string returnUrl = "/room-list/index.html")
    {
        var canonicalProvider = ExternalAuthentication.Providers.FirstOrDefault(
            name => string.Equals(name, provider, StringComparison.OrdinalIgnoreCase));
        if (canonicalProvider == null || await _schemes.GetSchemeAsync(canonicalProvider) == null)
            return BadRequest("External authentication provider is not configured.");

        await HttpContext.SignOutAsync(ExternalAuthentication.CookieScheme);
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(ExternalLoginCallback)),
            Items =
            {
                [ExternalAuthentication.ProviderItem] = canonicalProvider,
                [ExternalAuthentication.ReturnUrlItem] = SafeReturnUrl(returnUrl)
            }
        };
        return Challenge(properties, canonicalProvider);
    }

    [HttpGet("signin-callback")]
    public async Task<IActionResult> ExternalLoginCallback()
    {
        var result = await HttpContext.AuthenticateAsync(ExternalAuthentication.CookieScheme);
        await HttpContext.SignOutAsync(ExternalAuthentication.CookieScheme);
        if (!result.Succeeded || result.Principal == null || result.Properties == null
            || result.Principal.HasClaim(ExternalAuthentication.CompletedClaim, "true"))
            return Unauthorized("External authentication failed. Please sign in again.");

        result.Properties.Items.TryGetValue(ExternalAuthentication.ProviderItem, out var provider);
        var providerKey = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (provider == null || !ExternalAuthentication.Providers.Contains(provider)
            || await _schemes.GetSchemeAsync(provider) == null || string.IsNullOrWhiteSpace(providerKey))
            return BadRequest("The provider did not return a valid user identity.");

        var user = _getUser.GetUserByProvider(provider, providerKey);
        if (user == null)
        {
            var displayName = result.Principal.FindFirstValue(ClaimTypes.Name)?.Trim();
            var baseUsername = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName;
            var username = baseUsername;
            for (var attempt = 0; attempt < 3 && user == null; attempt++)
            {
                var status = _registrationService.Register(new RegistrationData(username, "", provider, providerKey));
                user = _getUser.GetUserByProvider(provider, providerKey);
                if (user != null)
                    break;
                if (status != Status.UsernameExists)
                    return BadRequest("External user registration failed.");
                username = $"{baseUsername} ({provider}-{Guid.NewGuid():N})";
            }
        }
        if (user == null)
            return Conflict("Could not select a unique username. Please sign in again.");

        result.Properties.Items.TryGetValue(ExternalAuthentication.ReturnUrlItem, out var returnUrl);
        var properties = new AuthenticationProperties
        {
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(1),
            IsPersistent = false,
            Items = { [ExternalAuthentication.ReturnUrlItem] = SafeReturnUrl(returnUrl) }
        };
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ExternalAuthentication.CompletedClaim, "true")
        ], ExternalAuthentication.CookieScheme);
        await HttpContext.SignInAsync(ExternalAuthentication.CookieScheme, new ClaimsPrincipal(identity), properties);
        return LocalRedirect("/oauth-complete.html");
    }

    [HttpPost("external-token")]
    public async Task<IActionResult> CompleteExternalLogin()
    {
        if (Request.Headers["X-Requested-With"] != "XMLHttpRequest")
            return BadRequest("Complete the external login from this site.");

        var result = await HttpContext.AuthenticateAsync(ExternalAuthentication.CookieScheme);
        await HttpContext.SignOutAsync(ExternalAuthentication.CookieScheme);
        if (!result.Succeeded || result.Principal == null
            || !result.Principal.HasClaim(ExternalAuthentication.CompletedClaim, "true")
            || !long.TryParse(result.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized("External login expired. Please sign in again.");

        var user = _getUser.GetUser(new UserId(userId));
        if (user == null)
            return Unauthorized();
        string? returnUrl = null;
        result.Properties?.Items.TryGetValue(ExternalAuthentication.ReturnUrlItem, out returnUrl);
        return Ok(new { token = GenerateJwtToken(user.Id.ToString()), returnUrl = SafeReturnUrl(returnUrl) });
    }

    private string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && Url.IsLocalUrl(returnUrl)
            ? returnUrl : "/room-list/index.html";

    [Authorize]
    [HttpGet("/me")]
    public ActionResult<User> GetMe()
    {
        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var user = _getUser.GetUser(new UserId(userId));
        if (user == null)
            return Unauthorized();
        return Ok(new User{Id = user.Id.ToString(), Username = user.Username, AvatarHash = user.AvatarHash});
    }

    [Authorize]
    [HttpPut("/me/avatar")]
    public ActionResult SetAvatar([FromBody] AvatarData data)
    {
        if (data == null || string.IsNullOrWhiteSpace(data.Hash))
            return BadRequest("Hash is required");

        if (_images.Get(data.Hash) == null)
            return NotFound("Image not found");

        if (!long.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Unauthorized();
        var id = new UserId(userId);
        if (_getUser.GetUser(id) == null)
            return Unauthorized();

        UserStorage.SetAvatarHash(id, data.Hash);
        return Ok(new { hash = data.Hash });
    }
    
    private string GenerateJwtToken(string id)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "User")
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
