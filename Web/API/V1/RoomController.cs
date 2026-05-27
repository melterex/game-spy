using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using authorization;
using GameLogic.Enums;
using GameLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomService;

namespace WebAPI.API.V1;

public class Room
{
    public string Name { get; set; }
    public string Id { get; set; }
    public int UsersCount { get; set; }
    public int UserMaxCount { get; set; }
}

public class RoomId
{
    public string Id { get; set; }
}
public class RoomSettings
{
    public string Theme { get; set; }
    public int UserMaxCount { get; set; }
}

public class PlayerLobbyData
{
    public PlayerData Player { get; set; }
    public bool Ready { get; set; }
}
public class LobbyStatus
{
    public PlayerLobbyData[] Players { get; set; }
    public RoomSettings RoomSettings { get; set; }
}

public class PlayerData
{
    public string Nickname { get; set; }
    public string Id { get; set; }
}
public class GameStatus
{
    public PlayerData[] Players { get; set; }
    public bool IsVoting { get; set; }
    public int TimeToVote { get; set; }
    public int TimeToMakeTurn { get; set; }
    public string TurnPlayerId { get; set; }
    public string Card {get; set;}    
    public string Theme {get; set;}
    public Message[] Messages { get; set; }
    public int Round { get; set; }
    public VoteStat[] VoteStatistics { get; set; }
    public bool IsAmogus { get; set; }
}

public class VoteStat
{
    public string PlayerId { get; set; }
    public int VotedForHim {get; set;}  
}
public class Message
{
    public string MessageBody { get; set; }
    public string PlayerId { get; set; }
    
}
[ApiController]
[Authorize]
[Route("api/v1/rooms")]
public class RoomController : ControllerBase
{
    private readonly IRoomService roomService;
    private readonly ILobbyService lobbyService;
    private readonly IGameService gameService;
    private readonly IGetUser getUserService;

    public RoomController(IRoomService _roomService, ILobbyService _lobbyService,
        IGameService _gameService, IGetUser _getUserService)
    {
        roomService = _roomService;
        lobbyService = _lobbyService;
        gameService = _gameService;
        getUserService = _getUserService;
    }
    [HttpGet]
    public ActionResult<Room[]> RoomList()
    {
        var roomsDictionary = roomService.GetRooms();
        var result = roomsDictionary.Values.Where(rsRoom => rsRoom.Status == RoomService.RoomStatus.Waiting).Select(rsRoom =>
        {
            var playerStatuses = lobbyService.GetPlayersStatuses(rsRoom.Session);
            var lobbySettings = lobbyService.GetLobbySettings(rsRoom.Session);
            return new Room
            {
                Id = rsRoom.RoomId.ToString(),
                Name = rsRoom.Title,
                UsersCount = playerStatuses?.Count ?? 0,
                UserMaxCount = lobbySettings?.MaxPlayers ?? 0 
            };
        }).ToArray();
        return Ok(result);
    }

    [HttpPost]
    public IActionResult RoomCreate([FromBody] RoomSettings roomSettings)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return Unauthorized();
        }

        var user = getUserService.GetUser(UserId.FromString(userIdString));

        roomService.CreateRoom(RoomType.Public, user);

        var newRoom = roomService.GetRoomByUserId(user.Id);
        if (newRoom == null)
        {
            return BadRequest();
        }

        var newLobbySettings = new LobbySettings(roomSettings.UserMaxCount, RoomService.RoomStatus.Waiting, roomSettings.Theme);
        lobbyService.SetLobbySettings(newLobbySettings, newRoom.Session);
        return Ok(newRoom.RoomId.ToString());
    }

    [HttpPost("{roomId}/enter")]
    public IActionResult RoomEnter(string roomId)
    {
        var room = roomService.GetRoomByRoomId(Guid.Parse(roomId));
        if (room == null)
        {
            return BadRequest();
        }
        var user = getUserService.GetUser(UserId.FromString(User.FindFirstValue(ClaimTypes.NameIdentifier)));
        var stat = lobbyService.TryToEnter(room.Session, user);
        if (stat == false)
        {
            return BadRequest();
        }
        else
        {
            return Ok();
        }
    }

    [HttpGet("my-room/status")]
    public ActionResult<string> InGame()
    {
        var user_req = getUserService.GetUser(UserId.FromString(User.FindFirstValue(ClaimTypes.NameIdentifier)));
        var room = roomService.GetRoomByUserId(user_req.Id);
        if (room == null)
        {
            return BadRequest();
        }

        switch (room.Status)
        {
            case RoomService.RoomStatus.Closed:
            {
                return Ok("closed");
            }
            case RoomService.RoomStatus.InGame:
            {
                return Ok("ingame");
            }
            case RoomService.RoomStatus.Waiting:
            {
                return Ok("waiting");
            }
            default:
                return BadRequest();
        }
    }
    [HttpGet("my-room/lobby")]
    public ActionResult<LobbyStatus> RoomStatus()
    {
        var user_req = getUserService.GetUser(UserId.FromString(User.FindFirstValue(ClaimTypes.NameIdentifier)));
        
        var room = roomService.GetRoomByUserId(user_req.Id);
        if (room == null)
        {
            return NotFound();
        }
        if (room.Status != RoomService.RoomStatus.Waiting)
        {
            return BadRequest();
        }

        if (room.Status != RoomService.RoomStatus.Waiting)
        {
            return BadRequest();
        }
        var playerStatuses = lobbyService.GetPlayersStatuses(room.Session);
        var playersList = new List<PlayerLobbyData>();
        foreach (var kvp in playerStatuses)
        {
            var userId = kvp.Key;
            var status = kvp.Value;

            var user = getUserService.GetUser(userId);
            playersList.Add(new PlayerLobbyData
            {
                Player = new PlayerData
                {
                    Id = user.Id.ToString(),
                    Nickname = user.Username
                },
                Ready = status == PlayerStatus.Ready
            });
        }

        var settings = lobbyService.GetLobbySettings(room.Session);
    
        var apiRoomSettings = new RoomSettings();
        apiRoomSettings.Theme = settings.Theme;
        apiRoomSettings.UserMaxCount = settings.MaxPlayers; 

        var lobbyStatus = new LobbyStatus
        {
            Players = playersList.ToArray(),
            RoomSettings = apiRoomSettings
        };

        return Ok(lobbyStatus);
    }

    [HttpGet("my-room/game")]
    public ActionResult<GameStatus> GameStatus()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserId = UserId.FromString(userIdString);


        var room = roomService.GetRoomByUserId(currentUserId);
        if (room == null)
        {
            return BadRequest();
        }
        if (room.Status != RoomService.RoomStatus.InGame)
        {
            return BadRequest();
        }

        var gameSession = lobbyService.GetGameSession(room.Session);

        var playersList = new List<PlayerData>();
        foreach (var pId in gameSession.PlayersIDs)
        {
            var user = getUserService.GetUser(pId);
            if (user != null)
            {
                playersList.Add(new PlayerData
                {
                    Id = user.Id.ToString(),
                    Nickname = user.Username
                });
            }
        }

        var cards = gameService.AssignCards(gameSession);
        string userCardWord = cards[currentUserId].Word;
        bool isAmogus = cards[currentUserId].IsSpy;


        var apiMessages = new List<Message>();
        foreach (var userMessages in gameSession.MessagesList)
        {
            apiMessages.Add(new Message
            {
                PlayerId = userMessages.Id.ToString(),
                MessageBody = userMessages.MessageBody,
            });
        }

        var voteStats = new List<VoteStat>();
        var votingService = gameService.GetVoteService(gameSession);
    
        if (gameSession.CurrentStage == GameStage.Voting)
        {
            var report = votingService.GetVotingReport(gameSession);
        
            foreach (var vote in report.Votes)
            {
                voteStats.Add(new VoteStat
                {
                    PlayerId = vote.Key.ToString(),
                    VotedForHim = vote.Value
                });
            }
        }

        var lobbySettings = lobbyService.GetLobbySettings(room.Session);

        var status = new GameStatus
        {
            Players = playersList.ToArray(),
            IsVoting = gameSession.CurrentStage == GameStage.Voting,
            TimeToVote = (int)((gameService.GetVotingStartTime(gameSession) + TimeSpan.FromMinutes(5)) - DateTime.Now).TotalSeconds,
            TimeToMakeTurn = (int)((gameService.GetCurrentTurnStartTime(gameSession) + TimeSpan.FromSeconds(60)) - DateTime.Now).TotalSeconds,
            TurnPlayerId = gameService.WhoseTurn(gameSession)?.ToString() ?? string.Empty,
            Card = userCardWord,
            Theme = lobbySettings?.Theme ?? string.Empty,
            Messages = apiMessages.ToArray(),
            Round = gameSession.CurrentRound,
            VoteStatistics = voteStats.ToArray(),
            IsAmogus = isAmogus
        };

        return Ok(status);
    }
    
}