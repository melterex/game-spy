using BotService;
using GameLogic.Interfaces;
public class BotFactory : IBotFactory
{
    private readonly string _openRouterApiKey;
    private readonly string _model;
    public BotFactory(string apiKey, string model = "openrouter/free")
    {
        _openRouterApiKey = apiKey;
        _model = model;
    }

    public IDecisionMaker CreateBot()
    {
        if (string.IsNullOrWhiteSpace(_openRouterApiKey))
        {
            return new DummyBot();
        }

        return new OpenRouterBot(_openRouterApiKey, _model);
    }
}