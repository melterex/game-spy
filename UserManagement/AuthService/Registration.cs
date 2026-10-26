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
    public string Provider { get;}
    public string ProviderKey { get;}

    public RegistrationData(string username, string password, string provider = "", string providerKey = "")
    {
        Username = username;
        Password = password;
        Provider = provider;
        ProviderKey = providerKey;
    }
}

public interface IRegistrationService
{
    Status Register(RegistrationData regData);
}