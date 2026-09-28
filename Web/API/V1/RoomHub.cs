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

    public RoomHub(IRoomService roomService, ILobbyService lobbyService, IGameService gameService,
        IGetUser getUserService, IGameWorker gameWorker, ITurnStorage turnStorage)
    {
        this.roomService = roomService;
        this.lobbyService = lobbyService;
        this.gameService = gameService;
        this.getUserService = getUserService;
        this.turnStorage = turnStorage;
        this.gameWorker = gameWorker;
    }

    public async Task EnterRoom()
    {
        var userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(userId);
        var user = getUserService.GetUser(userId);
        if (room == null)
        {
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
            throw new HubException("Room not found");
        }

        if (isReady == false)
        {
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
            throw new HubException("Room not found");
        }

        if (!lobbyService.GetPlayersStatuses(room.Session).ContainsKey(UserId.FromString(userId)))
        {
            throw new HubException("No such user");
        }

        if (lobbyService.KickUserByUserId(UserId.FromString(userId), room.Session))
        {
            await Clients.Group(room.RoomId.ToString()).SendAsync("KickUser", userId);
        }
        else
        {
            throw new HubException("Not enough rights");
        }
    }

    public async Task StartGame()
    {
        var userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(userId);
        if (room == null)
        {
            throw new HubException("Room not found");
        }

        if (lobbyService.StartGame(room.Session) == null)
        {
            throw new HubException("Can't start game");
        }
        else
        {
            await Clients.Group(room.RoomId.ToString()).SendAsync("StartGame");
        }

        turnStorage.AddTurnEnd(DateTime.Now + TimeSpan.FromMinutes(1), room.RoomId);
    }

    public async Task MakeTurn(string message)
    {
        var userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(userId);
        if (room == null)
        {
            throw new HubException("Room not found");
        }

        var gameSession = lobbyService.GetGameSession(room.Session);
        if (gameSession == null)
        {
            throw new HubException("Not inside game");
        }

        var slotId = gameService.GetSlotIDByUserID(gameSession, userId);
        if (slotId != gameService.WhoseTurn(gameSession))
        {
            throw new HubException("Not your turn");
        }

        gameService.MessageReceived(gameSession, message);
        var nextSlotId = gameService.WhoseTurn(gameSession);
        if (nextSlotId == null && gameService.GetVotingStartTime(gameSession) == default)
        {
            gameService.StartVoting(gameSession);
        }

        await gameWorker.OnTurnMadeAsync(room.RoomId, slotId, message, nextSlotId);
    }

    public async Task MakeReadyEndVote(bool isReady)
    {
        var userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(userId);
        if (room == null)
        {
            throw new HubException("Room not found");
        }

        var gameSession = lobbyService.GetGameSession(room.Session);
        if (gameSession == null)
        {
            throw new HubException("Not inside game");
        }

        if (room.Status != RoomStatus.InGame)
        {
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
            throw new HubException("Room not found");
        }

        var gameSession = lobbyService.GetGameSession(room.Session);
        if (gameSession == null)
        {
            throw new HubException("Not inside game");
        }

        if (!int.TryParse(slotId, out var targetSlotValue))
        {
            throw new HubException("Id incorrect");
        }

        var targetSlotId = new SlotID { Id = targetSlotValue };
        var targetSlot = gameSession.PlayerSlots.FirstOrDefault(slot => slot.Id == targetSlotId);
        if (targetSlot == null)
        {
            throw new HubException("Id incorrect");
        }

        var voterSlotId = gameService.GetSlotIDByUserID(gameSession, voterId);
        
        var votingService = gameService.GetVoteService(gameSession);
        votingService.Vote(gameSession, voterSlotId, targetSlot.Id);
        var report = votingService.GetVotingReport(gameSession);
        await gameWorker.OnVoteMadeAsync(room.RoomId, report);
    }

}
