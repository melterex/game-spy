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
        void Vote(GameSession session, SlotID chooserId, SlotID choosedId);
        bool IsVotingEnded(GameSession session);
        VotingResults SummarizeResults(GameSession session);
        VotingReport GetVotingReport(GameSession session);
        void SetPlayerReadyToEndVoting(GameSession session, SlotID slotId, bool isReady);
        bool IsEveryoneReadyToEndVoting(GameSession session);
        Dictionary<SlotID, bool> GetIsPlayerReadyToEndVotingDict(GameSession session);
    }
}