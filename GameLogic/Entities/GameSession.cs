using authorization;
using GameLogic.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic.Entities
{
    public abstract class GameSession
    {
        public Guid GameId { get; set; }
        public List<UserId> playersIDs { get; set; }
        public GameStage CurrentStage { get; set; }
        public Int32 CurrentRound { get; set; }
        public String CurrentWord { get; set; }
        public List<Messages> MessagesList { get; set; }
        public GameSettings GameSettings { get; set; }
    }
}