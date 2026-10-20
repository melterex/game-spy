using authorization;
using GameLogic.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameLogic.Interfaces{
   public interface IVoteMaker
    {
        public SlotID MakeVote(GameContext context);
    }

    public interface IMessageMaker
    {
        public string MakeMessage(GameContext context);
    }

    public interface IDecisionMaker : IVoteMaker, IMessageMaker
    {
    }

    public interface IBotFactory
    {
        IDecisionMaker CreateBot();
    }
}
