namespace authorization;

public class LoginService : ILoginService
{
    public User? Login(LoginData login)
    {
        if (string.IsNullOrWhiteSpace(login.Username) || string.IsNullOrEmpty(login.Password)
            || System.Text.Encoding.UTF8.GetByteCount(login.Password) > 72) return null;
        if (!UserStorage.CheckPassword(login.Username, login.Password))
            return null;
        
        return UserStorage.FindByUsername(login.Username);
    }
}
