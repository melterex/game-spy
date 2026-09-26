using authorization;
using GameLogic.Entities;
using GameLogic.Enums;
using GameLogic.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameLogic.Services
{
    public class VotingService : IVotingService
    {
        public void Vote(GameSession session, UserId voterId, UserId targetId)
        {
            if (session.CurrentStage != GameStage.Voting)
                throw new InvalidOperationException("Сейчас не этап голосования");
            if (voterId == targetId)
                throw new ArgumentException("Нельзя голосовать за себя");
            if (!session.PlayersIDs.Contains(targetId))
                throw new ArgumentException("Игрок не в игре");

            session.Votes[voterId] = targetId;
        }

        public bool IsVotingEnded(GameSession session) =>
            session.Votes.Count == session.PlayersIDs.Count;

        public VotingResults SummarizeResults(GameSession session)
        {
            if (session.Votes.Count == 0)
                return VotingResults.Tie;

            var mostVoted = session.Votes.Values
                .GroupBy(id => id)
                .OrderByDescending(g => g.Count())
                .First();

            int maxCount = mostVoted.Count();
            if (session.Votes.Values.GroupBy(id => id).Count(g => g.Count() == maxCount) > 1)
                return VotingResults.Tie;

            UserId votedOut = mostVoted.Key;
            var spy = session.PlayerCards.First(c => c.Value.IsSpy).Key;
            return votedOut == spy ? VotingResults.CivilianWins : VotingResults.SpyWins;
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

        public Dictionary<UserId, bool> GetIsPlayerReadyToEndVotingDict(GameSession session)
        {
            return session.IsPlayerReadyToEndVotingDict;
        }
    }
}