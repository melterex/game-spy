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

    public RoomHub(IRoomService roomService, ILobbyService lobbyService, IGameService gameService,
        IGetUser getUserService, IHubContext<RoomHub> hubContext, ITurnStorage turnStorage)
    {
        this.roomService = roomService;
        this.lobbyService = lobbyService;
        this.gameService = gameService;
        this.getUserService = getUserService;
        this.turnStorage = turnStorage;
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

    private Func<UserId, int, Func<Task>> changeTurn;

    public async Task MakeTurn(string message)
    {
        var userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room = roomService.GetRoomByUserId(userId);
        if (room == null)
        {
            throw new HubException("Room not found");
        }

        turnStorage.RemoveTurnEnd(room.RoomId);
        var gameSession = lobbyService.GetGameSession(room.Session);
        if (gameSession == null)
        {
            throw new HubException("Not inside game");
        }

        if (!gameService.WhoseTurn(gameSession).Equals(userId))
        {
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

        var votingService = gameService.GetVoteService(gameSession);
        var dict = votingService.GetIsPlayerReadyToEndVotingDict(gameSession);
        if (dict[userId] == isReady)
        {
            return;
        }
        if (!isReady && votingService.IsEveryoneReadyToEndVoting(gameSession))
        {
            await Clients.Group(room.RoomId.ToString()).SendAsync("ChangeVoteEnd",
                (int)((gameService.GetVotingStartTime(gameSession) + TimeSpan.FromMinutes(5)) - DateTime.Now)
                .TotalSeconds);
            turnStorage.RemoveVotingEnd(room.RoomId);
            turnStorage.AddVotingEnd(gameService.GetVotingStartTime(gameSession) + TimeSpan.FromMinutes(5),  room.RoomId);
            gameService.SetIsUsingExtraTime(gameSession, false);
        }
        votingService.SetPlayerReadyToEndVoting(gameSession, userId, isReady);
        await Clients.Group(room.RoomId.ToString()).SendAsync("UserEarlyVoteStatusChange", userId.ToString(), isReady);
        if (votingService.IsEveryoneReadyToEndVoting(gameSession))
        {
            turnStorage.RemoveVotingEnd(room.RoomId);
            turnStorage.AddVotingEnd(DateTime.Now + TimeSpan.FromSeconds(10), room.RoomId);
            await Clients.Group(room.RoomId.ToString()).SendAsync("ChangeVoteEnd", TimeSpan.FromSeconds(10).TotalSeconds);
            gameService.SetIsUsingExtraTime(gameSession, true);
            gameService.SetExtraTime(gameSession, DateTime.Now + TimeSpan.FromSeconds(10));
        }
    }

    public async Task MakeVote(string userId)
    {
        var _userId = UserId.FromString(Context.User.FindFirstValue(ClaimTypes.NameIdentifier));
        var room =  roomService.GetRoomByUserId(_userId);
        var choosedUserRoom = roomService.GetRoomByUserId(UserId.FromString(userId));
        if (choosedUserRoom == null)
        {
            throw new HubException("Id incorrect");
        }

        if (choosedUserRoom.RoomId != room.RoomId)
        {
            throw new HubException("Id incorrect");
        }
        if (room == null)
        {
            throw new HubException("Room not found");
        }
        var gameSession = lobbyService.GetGameSession(room.Session);
        if (gameSession == null)
        {
            throw new HubException("Not inside game");
        }
        
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