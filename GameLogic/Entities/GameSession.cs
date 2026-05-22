using authorization;
using GameLogic.Enums;
using GameLogic.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic
{
    public abstract class GameSession
    {
        public Guid GameId { get; set; }
        public List<UserId> playersIDs { get; set; }
        public GameStage CurrentStage { get; set; }
        public Int32 CurrentRound { get; set; }
        public String CurrentWord { get; set; }
        public List<Message> MessagesList { get; set; }
        public GameSettings GameSettings { get; set; }
    }
}