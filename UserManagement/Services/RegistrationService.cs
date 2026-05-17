namespace authorization;

public class RegistrationService : IRegistrationService
{
    public Status Register(RegistrationData regData)
    {
        if (string.IsNullOrWhiteSpace(regData.Username))
            return Status.Error;
        
        if (string.IsNullOrWhiteSpace(regData.Password))
            return Status.Error;
        
        if (UserStorage.UsernameExists(regData.Username))
            return Status.UsernameExists;
        
        var userId = new UserId(UserStorage.GetNextId());
        var user = new User(regData.Username, userId);
        
        UserStorage.Add(user, regData.Password);
        
        return Status.Ok;
    }
}