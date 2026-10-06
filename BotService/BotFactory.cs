using BotService;
using GameLogic.Interfaces;
public class BotFactory : IBotFactory
{
    private readonly string _openRouterApiKey;
    private readonly string _model;
    public BotFactory(string apiKey, string model = "deepseek/deepseek-v4-flash@provider=streamlake/fp8")
    {
        _openRouterApiKey = apiKey;
        _model = model;
    }

    public IDecisionMaker CreateBot()
    {
        if (string.IsNullOrWhiteSpace(_openRouterApiKey))
        {
            Console.Write("nooooo");
            return new DictionaryBot();
        }

        if (Random.Shared.Next(2) == 0)
        {
            return new PolzaBot(_openRouterApiKey, _model);
        }

        return new DictionaryBot();
    }
}