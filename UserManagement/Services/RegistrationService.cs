using Microsoft.Extensions.Logging;

namespace authorization;

public class RegistrationService(ILogger<RegistrationService> logger) : IRegistrationService
{
    public Status Register(RegistrationData regData)
    {
        if (string.IsNullOrWhiteSpace(regData.Username))
            return Status.Error;
        
        if (string.IsNullOrWhiteSpace(regData.Password))
            return Status.Error;
        
        if (UserStorage.UsernameExists(regData.Username)){
            logger.LogWarning("Registration failed. Username already exists: {Username}", regData.Username);
            return Status.UsernameExists;
        }
        
        var userId = new UserId(UserStorage.GetNextId());
        var user = new User(regData.Username, userId);
        
        UserStorage.Add(user, regData.Password);

        logger.LogInformation("User {Username} successfully registered with ID {UserId}", regData.Username, userId);
        
        return Status.Ok;
    }
}