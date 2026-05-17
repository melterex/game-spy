namespace authorization;

public class LoginService : ILoginService
{
    public User? Login(LoginData login)
    {
        if (!UserStorage.CheckPassword(login.Username, login.Password))
            return null;
        
        return UserStorage.FindByUsername(login.Username);
    }
}