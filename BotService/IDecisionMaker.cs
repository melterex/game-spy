using authorization;

namespace BotService;

public class GameContext
{
    public GameContext(UserId myId, bool isPlayerAmogus, string card, List<Player> players, List<Message> messages)
    {
        MyId = myId;
        IsPlayerAmogus = isPlayerAmogus;
        Card = card;
        Players = players;
        Messages = messages;
    }

    public UserId MyId { get; set; }
    public bool IsPlayerAmogus { get; set; }
    public string Card { get; set; }
    public List<Player> Players { get; set; }
    public List<Message> Messages { get; set; }
}

public class Player
{
    public Player(string nickName, UserId id)
    {
        NickName = nickName;
        Id = id;
    }

    public string NickName { get; set; }
    public UserId Id { get; set; }
}

public class Message
{
    public Message(string messageBody, UserId sender)
    {
        MessageBody = messageBody;
        Sender = sender;
    }

    public string MessageBody { get; set; }
    public UserId Sender { get; set; }
}

public interface IDecisionMaker
{
    public string MakeMessage(GameContext context);
    public UserId MakeVote(GameContext context);
}
