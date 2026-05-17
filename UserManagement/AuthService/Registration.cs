namespace authorization;

public enum Status
{
    UsernameExists,
    Ok,
    Error
}

public class RegistrationData
{
    public string Username { get;}
    public string Password { get;}

    public RegistrationData(string username, string password)
    {
        Username = username;
        Password = password;
    }
}

public interface IRegistrationService
{
    Status Register(RegistrationData regData);
}