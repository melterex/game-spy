using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public string Ready { get; set; }
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
    [HttpGet("rooms-list")]
    public ActionResult<Room[]> RoomList()
    {
        return NotFound();
    }

    [HttpPost("room-create")]
    public IActionResult RoomCreate([FromBody] RoomSettings roomSettings)
    {
        return NotFound();    
    }

    [HttpPost("room-enter")]
    public IActionResult RoomEnter([FromBody] RoomId roomId)
    {
        return NotFound();
    }

    [HttpGet("in-game")]
    public ActionResult<bool> InGame([FromQuery] string roomid)
    {
        return NotFound();
    }
    [HttpGet("lobby-status")]
    public ActionResult<LobbyStatus> RoomStatus()
    {
        return NotFound();
    }

    [HttpGet("game-status")]
    public ActionResult<GameStatus> GameStatus()
    {
        return NotFound();
    }
    
}