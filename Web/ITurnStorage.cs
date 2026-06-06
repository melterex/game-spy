using WebAPI.API.V1;

namespace WebAPI;

public enum ActionType
{
    Turn,
    Voting
}

public class Action
{
    public ActionType Type { get; set; }
    public Guid RoomId { get; set; }

    public Action(ActionType type, Guid roomId)
    {
        Type = type;
        RoomId = roomId;
    }
}
public interface ITurnStorage
{
    void AddTurnEnd(DateTime date, Guid roomId);
    void RemoveTurnEnd(Guid roomId);
    void AddVotingEnd(DateTime date, Guid roomId);
    void RemoveVotingEnd(Guid roomId);
    List<Action> GetActions();
}