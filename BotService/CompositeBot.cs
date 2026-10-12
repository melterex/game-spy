using authorization;
using GameLogic.Entities;
using GameLogic.Interfaces;

namespace BotService
{
    internal class CompositeBot : IDecisionMaker
    {
        private readonly IVoteMaker _voteMaker;
        private readonly IMessageMaker _messageMaker;

        public CompositeBot(IVoteMaker voteMaker, IMessageMaker messageMaker)
        {
            _voteMaker = voteMaker;
            _messageMaker = messageMaker;
        }

        public SlotID MakeVote(GameContext context)
        {
            return _voteMaker.MakeVote(context);
        }

        public string MakeMessage(GameContext context)
        {
            return _messageMaker.MakeMessage(context);
        }
    }
}
