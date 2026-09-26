namespace authorization;

public class GetUserService : IGetUser
{
    public User? GetUser(UserId id)
    {
        return UserStorage.FindById(id);
    }
}