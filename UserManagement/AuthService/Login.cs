namespace authorization;

public class LoginData
{
    public string Username { get;}
    public string Password { get;}

    public LoginData(string username, string password)
    {
        Username = username;
        Password = password;
    }
}

public interface ILoginService
{
    User? Login(LoginData login);
}