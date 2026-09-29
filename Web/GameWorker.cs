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
    private readonly ILogger<GameWorker> logger;

    public GameWorker(IHubContext<RoomHub> hubContext, ITurnStorage turnStorage, IVotingService votingService, ILogger<GameWorker> logger)
    {
        this.hubContext = hubContext;
        this.turnStorage = turnStorage;
        this.votingService = votingService;
        this.logger = logger;
    }

    public async Task OnTurnMadeAsync(Guid roomId, SlotID slotId, string message, SlotID? nextSlotId)
    {
        turnStorage.RemoveTurnEnd(roomId);
        if (nextSlotId != null)
        {
            turnStorage.AddTurnEnd(DateTime.Now + TimeSpan.FromMinutes(1), roomId);
        }
        else
        {
            turnStorage.AddVotingEnd(DateTime.Now + TimeSpan.FromMinutes(5), roomId);
        }

        await hubContext.Clients.Group(roomId.ToString()).SendAsync("TurnMade", slotId.ToString(), true,
            message, nextSlotId != null, nextSlotId?.ToString() ?? string.Empty);
    }

    public Task OnVoteMadeAsync(Guid roomId, VotingReport report)
    {
        List<string> slots = new();
        List<int> votes = new();
        foreach (var vote in report.Votes)
        {
            slots.Add(vote.Key.ToString());
            votes.Add(vote.Value);
        }

        return hubContext.Clients.Group(roomId.ToString()).SendAsync("VoteChange", slots, votes);
    }

    public async Task MakeReadyEndVoteAsync(Guid roomId, GameSession gameSession, SlotID slotId, bool isReady)
    {
        var readyPlayers = votingService.GetIsPlayerReadyToEndVotingDict(gameSession);
        if (readyPlayers[slotId] == isReady)
        {
            logger.LogWarning("Slot {SlotId} in room {RoomId} already has voting readiness {IsReady}",
                slotId, roomId, isReady);
            return;
        }

        var wasEveryoneReady = votingService.IsEveryoneReadyToEndVoting(gameSession);
        votingService.SetPlayerReadyToEndVoting(gameSession, slotId, isReady);
        var isEveryoneReady = votingService.IsEveryoneReadyToEndVoting(gameSession);
        var votingEnd = gameSession.VotingStartTime + TimeSpan.FromMinutes(5);

        if (!isReady && wasEveryoneReady)
        {
            turnStorage.RemoveVotingEnd(roomId);
            turnStorage.AddVotingEnd(votingEnd, roomId);
            gameSession.IsUsingExtraTime = false;
            logger.LogInformation("Slot {SlotId} cancelled early vote end. Timer reset to default for room {RoomId}",
                slotId, roomId);
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

        await clients.SendAsync("UserEarlyVoteStatusChange", slotId.ToString(), isReady);
        if (isEveryoneReady)
        {
            await clients.SendAsync("ChangeVoteEnd", TimeSpan.FromSeconds(10).TotalSeconds);
        }
    }
}
