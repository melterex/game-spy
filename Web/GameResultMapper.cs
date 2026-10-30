using authorization;
using GameLogic.Enums;

namespace WebAPI;

public static class GameResultMapper
{
    public static List<GameResult> FromSession(GameSession session, VotingResults outcome)
    {
        var playedAt = DateTime.UtcNow;
        return session.PlayerIDs.Select(player =>
        {
            var isSpy = session.PlayerCards[player.Value].IsSpy;
            var won = isSpy ? outcome == VotingResults.SpyWins : outcome == VotingResults.CivilianWins;
            return new GameResult(player.Key.Id, session.GameId.ToString(),
                isSpy ? GameRole.Spy : GameRole.Civilian, won) { GameDate = playedAt };
        }).ToList();
    }
}
