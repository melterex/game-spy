using authorization;
using GameLogic.Entities;
using GameLogic.Enums;
using GameLogic.Interfaces;

public abstract class GameSession
{
    public Guid GameId { get; set; }
    public List<PlayerSlot> PlayerSlots { get; set; }
    public Dictionary<UserId, SlotID> PlayerIDs { get; set; }
    public Dictionary<SlotID, Card> PlayerCards { get; set; }
    public GameStage CurrentStage { get; set; }
    public Int32 CurrentRound { get; set; }
    internal List<SlotID> CurrentPlayerOrder { get; set; }
    public String CurrentWord { get; set; }
    public Int32 CurrentTurnNumber { get; set; }
    public List<Message> MessagesList { get; set; }
    public GameSettings GameSettings { get; set; }
    public Int32 CurrentPlayerIndex { get; set; }
    public Dictionary<SlotID, SlotID> Votes { get; set; } = new();
    public bool VotingEnded { get; set; } = false;
    public DateTime CurrentTurnStartTime { get; set; }
    public DateTime VotingStartTime { get; set; }
    public Dictionary<SlotID, bool> IsPlayerReadyToEndVotingDict { get; set; } = new();
    public DateTime ExtraTime { get; set; } = DateTime.MinValue;
    public bool IsUsingExtraTime { get; set; } = false;
    public Dictionary<SlotID, IDecisionMaker> Bots { get; set; } = new();
    public Dictionary<SlotID, string> PlayerComments { get; set; } = new();
}