namespace authorization;

public interface IGetUser
{
    User? GetUser(UserId id);
    User? GetUserByProvider(string provider, string providerKey);
}