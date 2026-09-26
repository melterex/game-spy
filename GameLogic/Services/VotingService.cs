using authorization;
using GameLogic.Entities;
using GameLogic.Enums;
using GameLogic.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameLogic.Services
{
    public class VotingService(ILogger<VotingService> logger) : IVotingService
    {
        public void Vote(GameSession session, UserId voterId, UserId targetId)
        {
            if (session.CurrentStage != GameStage.Voting)
            {
                logger.LogWarning("It is not the voting stage right now.");
                throw new InvalidOperationException("Сейчас не этап голосования");
            }
            if (voterId == targetId)
            {
                logger.LogWarning("Player {PlayerId} attempted to vote for themselves", voterId);
                throw new ArgumentException("Нельзя голосовать за себя");
            }
            if (!session.PlayersIDs.Contains(targetId))
            {
                logger.LogWarning(
                    "Player {VoterId} attempted to vote for non-existing player {TargetId}", voterId, targetId
                    );
                throw new ArgumentException("Игрок не в игре");
            }

            session.Votes[voterId] = targetId;
            logger.LogInformation(
                "Player {VoterId} voted for {TargetId} in session {GameId}", voterId, targetId, session.GameId
                );
        }

        public bool IsVotingEnded(GameSession session) =>
            session.Votes.Count == session.PlayersIDs.Count;

        public VotingResults SummarizeResults(GameSession session)
        {
            if (session.Votes.Count == 0)
            {
                logger.LogInformation("The game ended in a draw");
                return VotingResults.Tie;
            }

            var mostVoted = session.Votes.Values
                .GroupBy(id => id)
                .OrderByDescending(g => g.Count())
                .First();

            int maxCount = mostVoted.Count();
            if (session.Votes.Values.GroupBy(id => id).Count(g => g.Count() == maxCount) > 1)
            {
                logger.LogInformation("The game ended in a draw");
                return VotingResults.Tie;
            }

            UserId votedOut = mostVoted.Key;
            var spy = session.PlayerCards.First(c => c.Value.IsSpy).Key;
            if (votedOut == spy)
            {
                logger.LogInformation(
                    "Voting finished in session {GameId}. Spy {SpyId} was caught! Civilian wins", 
                    session.GameId, spy
                    );
                return VotingResults.CivilianWins;
            }
            else
            {
                logger.LogInformation(
                    "Voting finished in session {GameId}. Civilians voted out innocent player {VotedOutId}. Spy {SpyId} wins", 
                    session.GameId, votedOut, spy
                    );
                return VotingResults.SpyWins;
            }

        }

        public VotingReport GetVotingReport(GameSession session)
        {
            return new VotingReport(
                session.Votes.Values
                    .GroupBy(id => id)
                    .ToDictionary(g => g.Key, g => g.Count())
            );
        }

        public void SetPlayerReadyToEndVoting(GameSession session, UserId userID, bool isReady)
        {
            session.IsPlayerReadyToEndVotingDict[userID] = isReady;
        }

        public bool IsEveryoneReadyToEndVoting(GameSession session)
        {
            return session.IsPlayerReadyToEndVotingDict.Values.All(v => v == true);
        }
    }
}