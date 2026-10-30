namespace authorization;

public interface IGameStatsService
{
    void RecordGameResult(long userId, string gameId, GameRole role, bool isWinner);
    PlayerStats GetPlayerStats(long userId);
    List<GameResult> GetGameHistory(long userId);
    List<PlayerStats> GetLeaderboard(int topCount = 10);
}