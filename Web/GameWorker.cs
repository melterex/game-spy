using authorization;
using GameLogic.Entities;
using GameLogic.Interfaces;
using Microsoft.AspNetCore.SignalR;
using WebAPI.API.V1;

namespace WebAPI;

public class GameWorker : IGameWorker
{
    private readonly IHubContext<RoomHub> hubContext;
    private readonly ITurnStorage turnStorage;
    private readonly IVotingService votingService;

    public GameWorker(IHubContext<RoomHub> hubContext, ITurnStorage turnStorage, IVotingService votingService)
    {
        this.hubContext = hubContext;
        this.turnStorage = turnStorage;
        this.votingService = votingService;
    }

    public async Task OnTurnMadeAsync(Guid roomId, UserId userId, string message, UserId? nextUserId)
    {
        turnStorage.RemoveTurnEnd(roomId);
        if (nextUserId != null)
        {
            turnStorage.AddTurnEnd(DateTime.Now + TimeSpan.FromMinutes(1), roomId);
        }
        else
        {
            turnStorage.AddVotingEnd(DateTime.Now + TimeSpan.FromMinutes(5), roomId);
        }

        await hubContext.Clients.Group(roomId.ToString()).SendAsync("TurnMade", userId.ToString(), true,
            message, nextUserId != null, nextUserId?.ToString() ?? string.Empty);
    }

    public Task OnVoteMadeAsync(Guid roomId, VotingReport report)
    {
        List<string> users = new();
        List<int> votes = new();
        foreach (var vote in report.Votes)
        {
            users.Add(vote.Key.ToString());
            votes.Add(vote.Value);
        }

        return hubContext.Clients.Group(roomId.ToString()).SendAsync("VoteChange", users, votes);
    }

    public async Task MakeReadyEndVoteAsync(Guid roomId, GameSession gameSession, UserId userId, bool isReady)
    {
        var readyPlayers = votingService.GetIsPlayerReadyToEndVotingDict(gameSession);
        if (readyPlayers[userId] == isReady)
        {
            return;
        }

        var wasEveryoneReady = votingService.IsEveryoneReadyToEndVoting(gameSession);
        votingService.SetPlayerReadyToEndVoting(gameSession, userId, isReady);
        var isEveryoneReady = votingService.IsEveryoneReadyToEndVoting(gameSession);
        var votingEnd = gameSession.VotingStartTime + TimeSpan.FromMinutes(5);

        if (!isReady && wasEveryoneReady)
        {
            turnStorage.RemoveVotingEnd(roomId);
            turnStorage.AddVotingEnd(votingEnd, roomId);
            gameSession.IsUsingExtraTime = false;
        }
        else if (isEveryoneReady)
        {
            var extraTimeEnd = DateTime.Now + TimeSpan.FromSeconds(10);
            turnStorage.RemoveVotingEnd(roomId);
            turnStorage.AddVotingEnd(extraTimeEnd, roomId);
            gameSession.IsUsingExtraTime = true;
            gameSession.ExtraTime = extraTimeEnd;
        }

        var clients = hubContext.Clients.Group(roomId.ToString());
        if (!isReady && wasEveryoneReady)
        {
            await clients.SendAsync("ChangeVoteEnd", (int)(votingEnd - DateTime.Now).TotalSeconds);
        }

        await clients.SendAsync("UserEarlyVoteStatusChange", userId.ToString(), isReady);
        if (isEveryoneReady)
        {
            await clients.SendAsync("ChangeVoteEnd", TimeSpan.FromSeconds(10).TotalSeconds);
        }
    }
}
