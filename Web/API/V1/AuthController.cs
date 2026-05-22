using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using authorization;
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

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILoginService _loginService;
    private readonly IRegistrationService _registrationService;
    public AuthController(IConfiguration configuration,  ILoginService loginService, IRegistrationService registrationService)
    {
        _configuration = configuration;
        _loginService = loginService;
        _registrationService = registrationService;
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