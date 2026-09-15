using System.Security.Claims;
using authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebAPI.Rooms;

namespace WebAPI.API.V1;

public sealed record JoinRequest(string? Password);

[ApiController, Authorize, Route("api/v1/rooms")]
public class RoomController(RoomCoordinator rooms, IGetUser users, CardsService.IThemesService themes) : ControllerBase
{
    private UserId Id => UserId.FromString(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private authorization.User CurrentUser => users.GetUser(Id) ?? throw new RoomRuleException("Войдите снова", 401);
    [HttpGet] public IActionResult List() => Ok(rooms.List());
    [HttpGet("themes")] public IActionResult Themes() => Ok(themes.GetThemes());
    [HttpPost, EnableRateLimiting("sensitive")]
    public IActionResult Create(RoomOptions options) => Ok(rooms.Create(CurrentUser, options));
    [HttpPost("{roomId:guid}/enter"), EnableRateLimiting("sensitive")]
    public IActionResult Enter(Guid roomId, JoinRequest request)
    {
        rooms.Enter(roomId, CurrentUser, request.Password);
        return Ok();
    }
    [HttpGet("my-room")]
    public IActionResult Mine() => rooms.FindRoom(Id) is { } id ? Ok(new { id }) : NotFound();
    [HttpGet("my-room/state")]
    public IActionResult State() => Ok(rooms.Snapshot(Id));
    [HttpPost("my-room/leave")]
    public IActionResult Leave() { rooms.Leave(Id); return Ok(); }
}
