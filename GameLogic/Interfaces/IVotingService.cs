using authorization;
using GameLogic.Entities;
using GameLogic.Enums;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace GameLogic.Interfaces
{
    public interface IVotingService
    {
        void Vote(GameSession session, UserId chooserId, UserId choosedId);
        bool IsVotingEnded(GameSession session);
        VotingResults SummarizeResults(GameSession session);
        VotingReport GetVotingReport(GameSession session);
        void SetPlayerReadyToEndVoting(GameSession session, UserId userID, bool isReady);
        bool IsEveryoneReadyToEndVoting(GameSession session);
        Dictionary<UserId, bool> GetIsPlayerReadyToEndVotingDict(GameSession session);
    }
}