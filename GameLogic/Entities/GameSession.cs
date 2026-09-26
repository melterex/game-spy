using authorization;
using GameLogic.Entities;
using GameLogic.Enums;

public abstract class GameSession
{
    public Guid GameId { get; set; }
    public List<UserId> PlayersIDs { get; set; }
    public Dictionary<UserId, Card> PlayerCards { get; set; }
    public GameStage CurrentStage { get; set; }
    public Int32 CurrentRound { get; set; }
    internal List<UserId> CurrentPlayerOrder { get; set; }
    public String CurrentWord { get; set; }
    public Int32 CurrentTurnNumber { get; set; }
    public List<Message> MessagesList { get; set; }
    public GameSettings GameSettings { get; set; }
    public Int32 CurrentPlayerIndex { get; set; }
    public Dictionary<UserId, UserId> Votes { get; set; } = new();
    public bool VotingEnded { get; set; } = false;
    public DateTime CurrentTurnStartTime { get; set; }
    public DateTime VotingStartTime { get; set; }
    public Dictionary<UserId, bool> IsPlayerReadyToEndVotingDict { get; set; } = new();
    public DateTime ExtraTime { get; set; } = DateTime.MinValue;
    public bool IsUsingExtraTime { get; set; } = false;
}