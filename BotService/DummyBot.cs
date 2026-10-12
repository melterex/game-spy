using authorization;
using System;
using System.Collections.Generic;
using System.Text;
using GameLogic.Entities;
using GameLogic.Interfaces;

namespace BotService
{
    internal class DummyMessageMaker : IMessageMaker
    {
        public string MakeMessage(GameContext context)
        {
            return "Я ботик";
        }
    }

    internal class DummyBot : CompositeBot
    {
        public DummyBot()
            : base(new RandomVoteMaker(), new DummyMessageMaker())
        {
        }
    }
}
