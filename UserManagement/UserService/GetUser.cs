namespace authorization;

interface IGetUser
{
    User? GetUser(UserId id);
}