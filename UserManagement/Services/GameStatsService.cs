namespace authorization;

public class GameStatsService : IGameStatsService
{
    public void RecordGameResult(long userId, string gameId, GameRole role, bool isWinner)
    {
        if (userId <= 0)
            throw new ArgumentException("Invalid user ID");

        if (string.IsNullOrWhiteSpace(gameId))
            throw new ArgumentException("Game ID cannot be empty");

        UserStorage.RecordGameResult(userId, gameId, role, isWinner);
    }

    public PlayerStats GetPlayerStats(long userId)
    {
        return UserStorage.GetPlayerStats(userId);
    }

    public List<GameResult> GetGameHistory(long userId)
    {
        return UserStorage.GetGameHistory(userId);
    }

    public List<PlayerStats> GetLeaderboard(int topCount = 10)
    {
        return UserStorage.GetLeaderboard(topCount);
    }
}
