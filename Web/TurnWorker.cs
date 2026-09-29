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
    private readonly ILogger<TurnWorker> _logger;

    public TurnWorker(IServiceScopeFactory scopeFactory, ITurnStorage turnStorage,
            IHubContext<RoomHub> hubContext, ILogger<TurnWorker> logger)
    {
        _turnStorage = turnStorage;
        _scopeFactory = scopeFactory;
        this.hubContext = hubContext;
        _logger = logger;
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
                var gameSession = room == null ? null : lobbyService.GetGameSession(room.Session);
                if (gameSession == null || gameSession.CurrentStage != GameStage.Round)
                    continue;

                var slotId = gameService.WhoseTurn(gameSession);
                gameService.MessageReceived(gameSession, "No message was provided");

                _logger.LogInformation(
                    "Turn timer expired for room {RoomId}. Forced skip for slot {SlotId}",
                    action.RoomId, slotId
                    );

                await hubContext.Clients.Group(room.RoomId.ToString()).SendAsync("TurnMade", slotId.ToString(),
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
                await gameService.ProcessBotActionsAsync(room.RoomId, gameSession);
            }
            else
            {
                var roomService = scope.ServiceProvider.GetRequiredService<IRoomService>();
                var lobbyService = scope.ServiceProvider.GetRequiredService<ILobbyService>();
                var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
                var room = roomService.GetRoomByRoomId(action.RoomId);
                var gameSession = room == null ? null : lobbyService.GetGameSession(room.Session);
                if (gameSession == null || gameSession.CurrentStage != GameStage.Voting)
                    continue;
                var votingService = gameService.GetVoteService(gameSession);
                var votingReport = votingService.GetVotingReport(gameSession);
                var results = votingService.SummarizeResults(gameSession);

                _logger.LogInformation(
                    "Voting timer expired for room {RoomId}. Forced voting end with result {Result}",
                    action.RoomId, results
                    );

                var maxSlotId = votingReport.Votes.MaxBy(a => a.Value).Key;
                bool wasAmogus = results == VotingResults.CivilianWins;
                if (results == VotingResults.Tie)
                {
                    await hubContext.Clients.Group(room.RoomId.ToString()).SendAsync("VoteFinish", "tie", false);
                }
                else
                {
                    await hubContext.Clients.Group(room.RoomId.ToString())
                        .SendAsync("VoteFinish", maxSlotId.ToString(), wasAmogus);
                }

                lobbyService.EndGame(room.Session);
            }
        }

    }
}
