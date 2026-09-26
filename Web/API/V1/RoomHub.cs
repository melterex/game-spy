using System.Security.Claims;
using authorization;
using GameLogic.Enums;
using GameLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RoomService;

namespace WebAPI.API.V1;

[Authorize]
public class RoomHub : Hub
{
    private readonly IRoomService roomService;
    private readonly ILobbyService lobbyService;
    private readonly IGameService gameService;
    private readonly IGetUser getUserService;
    private readonly ITurnStorage turnStorage;
    private ILogger<RoomHub> logger;

    public RoomHub(IRoomService roomService, ILobbyService lobbyService, IGameService gameService,
        IGetUser getUserService, IHubContext<RoomHub> hubContext, ITurnStorage turnStorage,ILogger<RoomHub> logger)
    {
        this.roomService = roomService;
        this.lobbyService = lobbyService;
        this.gameService = gameService;
        this.getUserService = getUserService;
        this.turnStorage = turnStorage;
        this.logger = logger;
    }

    public async Task EnterRoom()
    {
        var userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(userId);
        var user = getUserService.GetUser(userId);
        if (room == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to enter room: user {UserId} has no room active", 
                Context.ConnectionId, userId
                );
            throw new HubException("Room not found");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId.ToString());
        await Clients.Group(room.RoomId.ToString()).SendAsync("EnteredRoom", user.Id.ToString(), user.Username);
    }

    public async Task MakeReady(bool isReady)
    {
        var userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(userId);
        if (room == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make user ready by userId {UserId}", 
                Context.ConnectionId, userId
                );
            throw new HubException("Room not found");
        }

        if (isReady == false)
        {
            logger.LogWarning("Connection {ConnectionId} failed: user can`t be not ready", Context.ConnectionId);
            throw new HubException("Unsupported");
        }

        lobbyService.MakeReady(userId, room.Session);
        await Clients.Group(room.RoomId.ToString()).SendAsync("Ready", userId.ToString(), true);
    }

    public async Task KickUser(string userId)
    {
        var adminId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(adminId);
        if (room == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to kick user by adminId {adminId}", 
                Context.ConnectionId, adminId
                );
            throw new HubException("Room not found");
        }

        if (!lobbyService.GetPlayersStatuses(room.Session).ContainsKey(UserId.FromString(userId)))
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to find user by userId {userId}", 
                Context.ConnectionId, userId
                );
            throw new HubException("No such user");
        }

        if (lobbyService.KickUserByUserId(UserId.FromString(userId), room.Session))
        {
            logger.LogInformation(
                "Connection {ConnectionId} successfully kicked user by userId {userId}", 
                Context.ConnectionId, userId
                );
            await Clients.Group(room.RoomId.ToString()).SendAsync("KickUser", userId);
        }
        else
        {
            logger.LogInformation(
                "Connection {ConnectionId} failed to kick user {userId}, not enough rights for adminId {adminID}", 
                Context.ConnectionId, userId, adminId
                );
            throw new HubException("Not enough rights");
        }
    }

    public async Task StartGame()
    {
        var userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(userId);
        if (room == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to start game: cannot find room by userId {userId}", 
                Context.ConnectionId, userId
                );
            throw new HubException("Room not found");
        }

        if (lobbyService.StartGame(room.Session) == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to start game: lobby session is null",
                Context.ConnectionId
                );
            throw new HubException("Can't start game");
        }
        else
        {
            logger.LogInformation(
                "Connection {ConnectionId} successfully started game with roomId {RoomId}",
                Context.ConnectionId, room.RoomId
                );
            await Clients.Group(room.RoomId.ToString()).SendAsync("StartGame");
        }

        turnStorage.AddTurnEnd(DateTime.Now + TimeSpan.FromMinutes(1), room.RoomId);
    }

    private Func<UserId, int, Func<Task>> changeTurn;

    public async Task MakeTurn(string message)
    {
        var userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(userId);
        if (room == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make turn: cannot find room by userId {userId}", 
                Context.ConnectionId, userId
                );
            throw new HubException("Room not found");
        }

        turnStorage.RemoveTurnEnd(room.RoomId);
        var gameSession = lobbyService.GetGameSession(room.Session);
        if (gameSession == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make turn: cannot find GameSession by room {roomId}", 
                Context.ConnectionId, room.RoomId
                );
            throw new HubException("Not inside game");
        }

        if (!gameService.WhoseTurn(gameSession).Equals(userId))
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make turn: not userId {userId} turn", 
                Context.ConnectionId, userId
                );
            throw new HubException("Not your turn");
        }

        gameService.MessageReceived(gameSession, message);
        var curTime = gameService.GetCurrentTurnStartTime(gameSession);
        await Clients.Group(room.RoomId.ToString()).SendAsync("TurnMade", userId.ToString(), true, message,
            gameService.WhoseTurn(gameSession) != null,
            gameService.WhoseTurn(gameSession) != null ? gameService.WhoseTurn(gameSession).ToString() : "");
        if (gameService.WhoseTurn(gameSession) != null)
        {
            turnStorage.AddTurnEnd(DateTime.Now + TimeSpan.FromMinutes(1), room.RoomId);
        }
        else
        {
            gameService.StartVoting(gameSession);
            turnStorage.AddVotingEnd(DateTime.Now + TimeSpan.FromMinutes(5), room.RoomId);
        }
    }

    public async Task MakeReadyEndVote(bool isReady)
    {
        var userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(userId);
        if (room == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make ready and vote: cannot find room by userId {userId}", 
                Context.ConnectionId, userId
                );
            throw new HubException("Room not found");
        }

        var gameSession = lobbyService.GetGameSession(room.Session);
        if (gameSession == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make ready and vote: cannot find GameSession by room {roomId}", 
                Context.ConnectionId, room.RoomId
                );
            throw new HubException("Not inside game");
        }
        var votingService = gameService.GetVoteService(gameSession);
        votingService.SetPlayerReadyToEndVoting(gameSession, userId, isReady);
        if (votingService.IsEveryoneReadyToEndVoting(gameSession))
        {
            turnStorage.RemoveVotingEnd(room.RoomId);
            var votingReport = votingService.GetVotingReport(gameSession);
            var results = votingService.SummarizeResults(gameSession);
            var maxUserId = votingReport.Votes.MaxBy(a => a.Value);
            bool wasAmogus = results == VotingResults.CivilianWins;
            if (results == VotingResults.Tie)
            {
                logger.LogInformation(
                    "Connection {ConnectionId} successfully finished with Tie ", 
                    Context.ConnectionId
                    );
                await Clients.Group(room.RoomId.ToString()).SendAsync("VoteFinish", "tie", false);
            }
            else
            {
                logger.LogInformation(
                    "Connection {ConnectionId} successfully voted for userId {userId}", 
                    Context.ConnectionId, userId
                    );
                await Clients.Group(room.RoomId.ToString())
                    .SendAsync("VoteFinish", maxUserId.ToString(), wasAmogus);
            }

            logger.LogInformation(
                    "Connection {ConnectionId} successfully ended game", 
                    Context.ConnectionId
                    );
            lobbyService.EndGame(room.Session);
        }
    }

public async Task MakeVote(string userId)
    {
        var _userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room =  roomService.GetRoomByUserId(_userId);
        var choosedUserRoom = roomService.GetRoomByUserId(UserId.FromString(userId));
        if (choosedUserRoom == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to find room by userId {userId}", 
                Context.ConnectionId, userId
                );
            throw new HubException("Id incorrect");
        }

        if (choosedUserRoom.RoomId != room.RoomId)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to vote: target user {userId} is in a different room", 
                Context.ConnectionId, userId
                );
            throw new HubException("Id incorrect");
        }
        if (room == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make vote: cannot find room by userId {_userId}", 
                Context.ConnectionId, _userId
                );
            throw new HubException("Room not found");
        }
        var gameSession = lobbyService.GetGameSession(room.Session);
        if (gameSession == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make ready and vote: cannot find GameSession by room {roomId}", 
                Context.ConnectionId, room.RoomId
                );
            throw new HubException("Not inside game");
        }

        logger.LogInformation(
            "Connection {ConnectionId} successfully voted for userId {userId}",
            Context.ConnectionId, userId
            );
        
        var votingService = gameService.GetVoteService(gameSession);
        votingService.Vote(gameSession, _userId, UserId.FromString(userId));
        var report = votingService.GetVotingReport(gameSession);
        List<string> users = new();
        List<int> votes = new ();
        foreach (var i in report.Votes)
        {
            users.Add(i.Key.ToString());
            votes.Add(i.Value);
        }
        await Clients.Group(room.RoomId.ToString()).SendAsync("VoteChange", users, votes);
    }

}