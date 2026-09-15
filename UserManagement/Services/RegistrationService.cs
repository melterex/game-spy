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
        
        try { UserStorage.Add(user, regData.Password); }
        catch (Microsoft.Data.Sqlite.SqliteException error) when (error.SqliteErrorCode == 19)
        { return Status.UsernameExists; }
        
        return Status.Ok;
    }
}
