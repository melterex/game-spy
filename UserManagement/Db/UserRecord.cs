namespace authorization;

public class UserRecord
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? Provider { get; set; }
    public string? ProviderKey { get; set; }
    public List<GameResult> GameResults { get; set; } = [];

    public User ToUser() => new(Username, new UserId(Id), Provider ?? "", ProviderKey ?? "");
}
