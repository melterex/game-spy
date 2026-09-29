using System.Security.Claims;
using authorization;
using GameLogic.Entities;
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
    private readonly IGameWorker gameWorker;
    private readonly ILogger<RoomHub> logger;

    public RoomHub(IRoomService roomService, ILobbyService lobbyService, IGameService gameService,
        IGetUser getUserService, IGameWorker gameWorker, ITurnStorage turnStorage, ILogger<RoomHub> logger)
    {
        this.roomService = roomService;
        this.lobbyService = lobbyService;
        this.gameService = gameService;
        this.getUserService = getUserService;
        this.turnStorage = turnStorage;
        this.gameWorker = gameWorker;
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

        if (lobbyService.KickUserByUserId(adminId, UserId.FromString(userId), room.Session))
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

        var gameSession = lobbyService.StartGame(room.Session);
        if (gameSession == null)
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
        await gameService.ProcessBotActionsAsync(room.RoomId, gameSession);
    }

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

        var gameSession = lobbyService.GetGameSession(room.Session);
        if (gameSession == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make turn: cannot find GameSession by room {roomId}",
                Context.ConnectionId, room.RoomId
                );
            throw new HubException("Not inside game");
        }

        var slotId = gameService.GetSlotIDByUserID(gameSession, userId);
        if (slotId != gameService.WhoseTurn(gameSession))
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make turn: not userId {userId} turn",
                Context.ConnectionId, userId
                );
            throw new HubException("Not your turn");
        }

        gameService.MessageReceived(gameSession, message);
        var nextSlotId = gameService.WhoseTurn(gameSession);
        await gameWorker.OnTurnMadeAsync(room.RoomId, slotId, message, nextSlotId);
        await gameService.ProcessBotActionsAsync(room.RoomId, gameSession);
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

        if (room.Status != RoomStatus.InGame)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to change voting readiness: room {RoomId} is not in game",
                Context.ConnectionId, room.RoomId);
            throw new HubException("Not in game");
        }

        var slotId = gameService.GetSlotIDByUserID(gameSession, userId);
        await gameWorker.MakeReadyEndVoteAsync(room.RoomId, gameSession, slotId, isReady);
    }

    public async Task MakeVote(string slotId)
    {
        var voterId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(voterId);
        if (room == null)
        {
            logger.LogWarning(
                "Connection {ConnectionId} failed to make vote: cannot find room by userId {UserId}",
                Context.ConnectionId, voterId
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

        if (!int.TryParse(slotId, out var targetSlotValue))
        {
            logger.LogWarning("Connection {ConnectionId} attempted to vote for invalid slot {SlotId}",
                Context.ConnectionId, slotId);
            throw new HubException("Id incorrect");
        }

        var targetSlotId = new SlotID { Id = targetSlotValue };
        var targetSlot = gameSession.PlayerSlots.FirstOrDefault(slot => slot.Id == targetSlotId);
        if (targetSlot == null)
        {
            logger.LogWarning("Connection {ConnectionId} attempted to vote for missing slot {SlotId} in room {RoomId}",
                Context.ConnectionId, targetSlotId, room.RoomId);
            throw new HubException("Id incorrect");
        }

        var voterSlotId = gameService.GetSlotIDByUserID(gameSession, voterId);

        var votingService = gameService.GetVoteService(gameSession);
        votingService.Vote(gameSession, voterSlotId, targetSlot.Id);
        logger.LogInformation("Connection {ConnectionId} successfully voted for slot {SlotId}",
            Context.ConnectionId, targetSlot.Id);
        var report = votingService.GetVotingReport(gameSession);
        await gameWorker.OnVoteMadeAsync(room.RoomId, report);
    }

}
