using authorization;
using GameLogic.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic.Interfaces{
   public interface IDecisionMaker
    {
        public string MakeMessage(GameContext context);
        public UserId MakeVote(GameContext context);
    }

    public interface IBotFactory
    {
        IDecisionMaker CreateBot();
    }
}
