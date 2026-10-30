using authorization;
using GameLogic.Enums;
using GameLogic.Interfaces;

namespace WebAPI;

public sealed record GameCompletionSnapshot(
    Guid GameId, VotingResults Outcome, string VotedOutSlot, IReadOnlyList<GameResult> Results)
{
    public static GameCompletionSnapshot Capture(GameSession session, IVotingService votingService)
    {
        lock (session)
        {
            if (session.CurrentStage != GameStage.Voting || session.VotingEnded)
                throw new InvalidOperationException("Voting is already closed or has not started.");

            var outcome = votingService.SummarizeResults(session);
            var votedOutSlot = outcome == VotingResults.Tie
                ? "tie"
                : votingService.GetVotingReport(session).Votes.MaxBy(vote => vote.Value).Key.ToString();
            var results = GameResultMapper.FromSession(session, outcome).AsReadOnly();
            session.VotingEnded = true;
            return new GameCompletionSnapshot(session.GameId, outcome, votedOutSlot, results);
        }
    }
}
