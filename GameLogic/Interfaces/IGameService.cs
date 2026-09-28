using authorization;
using GameLogic.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Numerics;
using System.Text;

namespace GameLogic.Interfaces
{
    public interface IGameService
    {
        Dictionary<SlotID, Card> AssignCards(GameSession session);
        Card GetPlayerCardBySlotID(GameSession session, SlotID slotID);
        SlotID GetSlotIDByUserID(GameSession session, UserId userId);
        UserId? GetUserIDBySlotID(GameSession session, SlotID slotID);
        SlotID WhoseTurn(GameSession session);
        // Records a human turn. Notify clients before awaiting ProcessBotActionsAsync.
        void MessageReceived(GameSession session, String message);
        Task ProcessBotActionsAsync(Guid roomId, GameSession session);
        IVotingService GetVoteService(GameSession session);
        Guid CreateGameSession(List<UserId> playersIDs, GameSettings settings);
        GameSession GetGameSessionById (Guid GameSessionId);
        Task StartVotingAsync(Guid roomId, GameSession session);
        List<SlotID> GetPlayerOrder(GameSession session);
        DateTime GetCurrentTurnStartTime(GameSession session);
        DateTime GetVotingStartTime(GameSession session);
        Int32 GetCurrentTurnNumnber(GameSession session);
        void SetExtraTime(GameSession session, DateTime time);
        void SetIsUsingExtraTime(GameSession session, bool isUsing);
        DateTime GetExtraTime(GameSession session);
        bool GetIsUdingExtraTime(GameSession session);
    }
}
