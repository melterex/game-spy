using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace authorization;

public class RegistrationService(ILogger<RegistrationService> logger) : IRegistrationService
{
    public Status Register(RegistrationData regData)
    {
        if (string.IsNullOrWhiteSpace(regData.Username))
            return Status.Error;

        var hasProvider = !string.IsNullOrWhiteSpace(regData.Provider);
        if (hasProvider != !string.IsNullOrWhiteSpace(regData.ProviderKey))
            return Status.Error;
        if (!hasProvider && string.IsNullOrWhiteSpace(regData.Password))
            return Status.Error;

        if (UserStorage.UsernameExists(regData.Username)){
            logger.LogWarning("Registration failed. Username already exists: {Username}", regData.Username);
            return Status.UsernameExists;
        }

        try
        {
            var user = UserStorage.Add(regData.Username, regData.Password, regData.Provider, regData.ProviderKey);
            logger.LogInformation("User {Username} successfully registered with ID {UserId}", user.Username, user.Id);
            return Status.Ok;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
               { SqlState: PostgresErrorCodes.UniqueViolation } conflict)
        {
            return conflict.ConstraintName == "ux_users_username" ? Status.UsernameExists : Status.Error;
        }
    }
}
