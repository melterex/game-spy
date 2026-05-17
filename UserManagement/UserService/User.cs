namespace authorization;

public class UserId
{
    public long Id { get; }
    
    public UserId(long id)
    {
        Id = id;
    }

    public override bool Equals(object? obj)
    {
        return obj is UserId id && id.Id == Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return Id.ToString();
    }

    public static UserId FromString(string id)
    {
        return new UserId(long.Parse(id));
    }
}

public class User
{
    public string Username { get; }
    public UserId Id { get; }
    
    public User(string name, UserId id)
    {
        Username = name;
        Id = id;
    }
}