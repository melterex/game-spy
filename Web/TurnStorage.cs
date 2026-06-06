using System.Collections.Concurrent;
using WebAPI.API.V1;

namespace WebAPI;

public class TurnStorage : ITurnStorage
{
    private readonly ConcurrentDictionary<Guid, DateTime> _turnEnds = new();
    private readonly ConcurrentDictionary<Guid, DateTime> _votingEnds = new();

    public void AddTurnEnd(DateTime date, Guid roomId)
    {
        _turnEnds.AddOrUpdate(roomId, date, (_, _) => date);
    }

    public void RemoveTurnEnd(Guid roomId)
    {
        _turnEnds.TryRemove(roomId, out _);
    }

    public void AddVotingEnd(DateTime date, Guid roomId)
    {
        _votingEnds.AddOrUpdate(roomId, date, (_, _) => date);
    }

    public void RemoveVotingEnd(Guid roomId)
    {
        _votingEnds.TryRemove(roomId, out _);
    }

    public List<Action> GetActions()
    {
        var expiredActions = new List<Action>();
        
        var now = DateTime.Now; 

        foreach (var kvp in _turnEnds)
        {
            if (kvp.Value <= now)
            {
                if (_turnEnds.TryRemove(kvp.Key, out _))
                {
                    expiredActions.Add(new Action(ActionType.Turn, kvp.Key));
                }
            }
        }

        foreach (var kvp in _votingEnds)
        {
            if (kvp.Value <= now)
            {
                if (_votingEnds.TryRemove(kvp.Key, out _))
                {
                    expiredActions.Add(new Action(ActionType.Voting, kvp.Key));
                }
            }
        }

        return expiredActions;
    }
}