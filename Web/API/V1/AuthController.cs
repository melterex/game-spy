using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public string? Provider { get; set; }
    public string? ProviderKey { get; set; }
}
[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILoginService _loginService;
    private readonly IRegistrationService _registrationService;
    private readonly IGetUser _getUser;
    public AuthController(IConfiguration configuration, ILoginService loginService, IRegistrationService registrationService, IGetUser getUser)
    {
        _configuration = configuration;
        _loginService = loginService;
        _registrationService = registrationService;
        _getUser = getUser;
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
                    return Ok(GenerateJwtToken(user.Id.ToString()));
                }
            default:
                {
                    return BadRequest();
                }
        }
    }

    [HttpGet("login/{provider}")]
    public IActionResult ChallengeProvider(string provider, [FromQuery] string returnUrl = "/")
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), new { provider, returnUrl });
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };

        return Challenge(properties, provider);
    }

    [HttpGet("signin-callback")]
    public async Task<IActionResult> ExternalLoginCallback(string provider, string returnUrl = "/")
    {
        var result = await HttpContext.AuthenticateAsync(provider);
        if (!result.Succeeded || result.Principal == null)
        {
            return BadRequest("Error during external authentication.");
        }

        var providerKey = result.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = result.Principal.FindFirstValue(ClaimTypes.Name) ?? "Player";

        var providerName = result.Properties?.Items.ContainsKey(".AuthScheme") == true
            ? result.Properties.Items[".AuthScheme"]
            : "Unknown";

        var res = _getUser.GetUserByProvider(providerName, providerKey);
        var dummyPassword = Guid.NewGuid().ToString();

        if (res == null)
        {
            var userId = new UserId(UserStorage.GetNextId());
            var user1 = new User
            {
                Id = userId.ToString(),
                Username = name,
                Provider = providerName,
                ProviderKey = providerKey
            };

            var status = _registrationService.Register(
                new RegistrationData(user1.Username, dummyPassword, provider, providerKey
                ));
                
            if (status != Status.Ok)
            {
                return BadRequest("Error during user registration.");
            }
            res = _getUser.GetUserByProvider(providerName, providerKey);
            if (res == null)
            {
                return BadRequest("User registration failed to persist.");
            }
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, res.Id.ToString()),
            new Claim(ClaimTypes.Name, res.Username)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

        return LocalRedirect(returnUrl);
    }

    [Authorize]
    [HttpGet("/me")]
    public ActionResult<User> GetMe()
    {
        var user = _getUser.GetUser(UserId.FromString(User.FindFirstValue(ClaimTypes.NameIdentifier)));
        return Ok(new User { Id = user.Id.ToString(), Username = user.Username });
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
            expires: DateTime.Now.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}