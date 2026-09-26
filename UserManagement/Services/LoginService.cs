using Microsoft.Extensions.Logging;

namespace authorization;

public class LoginService(ILogger<LoginService> logger) : ILoginService
{
    public User? Login(LoginData login)
    {
        if (!UserStorage.CheckPassword(login.Username, login.Password))
        {
            logger.LogWarning("Failed login attempt for user: {Username}", login.Username);
            return null;
        }
        
        logger.LogInformation("User {Username} successfully logged in", login.Username);
        return UserStorage.FindByUsername(login.Username);
    }
}