using GameLogic.Enums;
using GameLogic.Interfaces;
using Microsoft.AspNetCore.SignalR;
using RoomService;
using WebAPI.API.V1;

namespace WebAPI;

public class TurnWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITurnStorage _turnStorage;
    private readonly IHubContext<RoomHub> hubContext;

    public TurnWorker(IServiceScopeFactory scopeFactory, ITurnStorage turnStorage, IHubContext<RoomHub> hubContext)
    {
        _turnStorage = turnStorage;
        _scopeFactory = scopeFactory;
        this.hubContext = hubContext;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(0.5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DoWork();
        }
    }

    private async Task DoWork()
    {
        using var scope = _scopeFactory.CreateScope();
        var actions = _turnStorage.GetActions();
        foreach (var action in actions)
        {
            if (action.Type == ActionType.Turn)
            {
                var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();
                var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();
                var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                var room = roomService.GetRoomByRoomId(action.RoomId);
                var gameSession = lobbyService.GetGameSession(room.Session);
                var userId = gameService.WhoseTurn(gameSession);
                gameService.MessageReceived(gameSession, "No message was provided");
                await hubContext.Clients.Group(room.RoomId.ToString()).SendAsync("TurnMade", userId.ToString(),
                    false, String.Empty, gameService.WhoseTurn(gameSession) != null,
                    gameService.WhoseTurn(gameSession) != null
                        ? gameService.WhoseTurn(gameSession).ToString()
                        : String.Empty);
                if (gameService.WhoseTurn(gameSession) != null)
                {
                    _turnStorage.AddTurnEnd(DateTime.Now + TimeSpan.FromMinutes(1), action.RoomId);
                }
                else
                {
                    _turnStorage.AddVotingEnd(DateTime.Now + TimeSpan.FromMinutes(5), action.RoomId);
                }
            }
            else
            {
                var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();
                var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();
                var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                var room = roomService.GetRoomByRoomId(action.RoomId);
                var gameSession = lobbyService.GetGameSession(room.Session);
                var votingService = gameService.GetVoteService(gameSession);
                var votingReport = votingService.GetVotingReport(gameSession);
                var results = votingService.SummarizeResults(gameSession);
                var maxUserId = votingReport.Votes.MaxBy(a => a.Value);
                bool wasAmogus = results == VotingResults.CivilianWins;
                if (results == VotingResults.Tie)
                {
                    await hubContext.Clients.Group(room.RoomId.ToString()).SendAsync("VoteFinish", "tie", false);
                }
                else
                {
                    await hubContext.Clients.Group(room.RoomId.ToString())
                        .SendAsync("VoteFinish", maxUserId.ToString(), wasAmogus);
                }

                lobbyService.EndGame(room.Session);
            } 
        }
        
    }
}