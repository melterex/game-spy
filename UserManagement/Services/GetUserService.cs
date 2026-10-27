namespace authorization;

public class GetUserService : IGetUser
{
    public User? GetUser(UserId id)
    {
        return UserStorage.FindById(id);
    }
    public User? GetUserByProvider(string provider, string providerKey)
    {
        return UserStorage.FindByProvider(provider, providerKey);
    }
}