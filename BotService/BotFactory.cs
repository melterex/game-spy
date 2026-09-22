using BotService;
using GameLogic.Interfaces;

public class BotFactory : IBotFactory
{
    public IDecisionMaker CreateBot()
    {
        return new DummyBot();
    }
}