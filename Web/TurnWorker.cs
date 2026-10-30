using authorization;
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
    private readonly Dictionary<Guid, PendingCompletion> _pendingCompletions = new();
    private sealed record PendingCompletion(GameCompletionSnapshot Snapshot, DateTime RetryAtUtc);

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
        var retryGames = new Dictionary<Guid, Guid>();
        if (_pendingCompletions.Count > 0)
        {
            var rooms = scope.ServiceProvider.GetRequiredService<IRoomService>();
            var lobbies = scope.ServiceProvider.GetRequiredService<ILobbyService>();
            foreach (var (roomId, pending) in _pendingCompletions.ToArray())
            {
                var room = rooms.GetRoomByRoomId(roomId);
                var session = room == null ? null : lobbies.GetGameSession(room.Session);
                if (session == null || session.GameId != pending.Snapshot.GameId)
                {
                    _pendingCompletions.Remove(roomId);
                    continue;
                }
                if (pending.RetryAtUtc <= DateTime.UtcNow
                    && !actions.Any(action => action.Type == ActionType.Voting && action.RoomId == roomId))
                {
                    actions.Add(new Action(ActionType.Voting, roomId));
                    retryGames[roomId] = pending.Snapshot.GameId;
                }
            }
        }
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
                {
                    _pendingCompletions.Remove(action.RoomId);
                    continue;
                }
                if (retryGames.TryGetValue(action.RoomId, out var retryGameId) && gameSession.GameId != retryGameId)
                {
                    _pendingCompletions.Remove(action.RoomId);
                    continue;
                }
                if (!_pendingCompletions.TryGetValue(action.RoomId, out var pending)
                    || pending.Snapshot.GameId != gameSession.GameId)
                {
                    var snapshot = GameCompletionSnapshot.Capture(gameSession, gameService.GetVoteService(gameSession));
                    pending = new PendingCompletion(snapshot, DateTime.UtcNow);
                    _pendingCompletions[action.RoomId] = pending;
                }

                try
                {
                    UserStorage.RecordGameResults(pending.Snapshot.Results);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception,
                        "Could not save statistics for game {GameId}; retrying completion in five seconds",
                        gameSession.GameId);
                    _pendingCompletions[action.RoomId] = pending with { RetryAtUtc = DateTime.UtcNow.AddSeconds(5) };
                    continue;
                }
                _pendingCompletions.Remove(action.RoomId);
                var results = pending.Snapshot.Outcome;

                _logger.LogInformation(
                    "Voting timer expired for room {RoomId}. Forced voting end with result {Result}",
                    action.RoomId, results
                    );

                lobbyService.EndGame(room.Session);
                bool wasAmogus = results == VotingResults.CivilianWins;
                try
                {
                    if (results == VotingResults.Tie)
                    {
                        await hubContext.Clients.Group(room.RoomId.ToString()).SendAsync("VoteFinish", "tie", false);
                    }
                    else
                    {
                        await hubContext.Clients.Group(room.RoomId.ToString())
                            .SendAsync("VoteFinish", pending.Snapshot.VotedOutSlot, wasAmogus);
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Could not broadcast completion of game {GameId}", gameSession.GameId);
                }
            }
        }

    }
}
