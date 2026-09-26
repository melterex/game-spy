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
                await Clients.Group(room.RoomId.ToString()).SendAsync("VoteFinish", "tie", false);
            }
            else
            {
                await Clients.Group(room.RoomId.ToString())
                    .SendAsync("VoteFinish", maxUserId.ToString(), wasAmogus);
            }

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