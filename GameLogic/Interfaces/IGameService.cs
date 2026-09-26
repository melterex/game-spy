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
        Dictionary<UserId, Card> AssignCards(GameSession session);
        Card GetPlayerCardByID(GameSession session, UserId userID);
        UserId WhoseTurn(GameSession session);
        void MessageReceived(GameSession session, String message); // Переключает ход
        IVotingService GetVoteService(GameSession session);
        Guid CreateGameSession(List<UserId> playersIDs, GameSettings settings);
        GameSession GetGameSessionById (Guid GameSessionId);
        void StartVoting (GameSession session);
        List<UserId> GetPlayerOrder(GameSession session);
        DateTime GetCurrentTurnStartTime(GameSession session);
        DateTime GetVotingStartTime(GameSession session);
        Int32 GetCurrentTurnNumnber(GameSession session);
        void SetExtraTime(GameSession session, DateTime time);
        void SetIsUsingExtraTime(GameSession session, bool isUsing);
        DateTime GetExtraTime(GameSession session);
        bool GetIsUdingExtraTime(GameSession session);
    }
}