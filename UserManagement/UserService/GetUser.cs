namespace authorization;

public interface IGetUser
{
    User? GetUser(UserId id);
}