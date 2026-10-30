namespace authorization;

public class GameResult
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string GameId { get; set; }
    public GameRole Role { get; set; }
    public bool IsWinner { get; set; }
    public DateTime GameDate { get; set; }

    public GameResult(long userId, string gameId, GameRole role, bool isWinner)
    {
        UserId = userId;
        GameId = gameId;
        Role = role;
        IsWinner = isWinner;
        GameDate = DateTime.UtcNow;
    }
}
