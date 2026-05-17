namespace authorization;

public class LoginData
{
    public string Username { get;}
    public string Password { get;}

    LoginData(string username, string password)
    {
        Username = username;
        Password = password;
    }
}

interface ILoginService
{
    User? Login(LoginData login);
}