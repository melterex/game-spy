using System.Security.Claims;
using authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Rooms;

namespace WebAPI.API.V1;

[ApiController, Authorize, Route("api/v1/profile")]
public sealed class ImageController(ProfileStore profiles, RoomCoordinator rooms) : ControllerBase
{
    private UserId Id => UserId.FromString(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet] public IActionResult Profile() => Ok(new { avatarUrl = profiles.AvatarUrl(Id) });
    [HttpGet("history")] public IActionResult History([FromQuery] int offset = 0) => Ok(profiles.History(Id, offset));
    [HttpGet("{userId:long}/avatar"), AllowAnonymous]
    public IActionResult Avatar(long userId)
    {
        var avatar = profiles.Avatar(new(userId));
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return avatar.HasValue ? File(avatar.Value.Data, avatar.Value.ContentType) : NotFound();
    }
    [HttpPost("avatar"), RequestSizeLimit(2_200_000)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length is < 12 or > 2_097_152) return BadRequest("Изображение должно быть не больше 2 МБ");
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, HttpContext.RequestAborted);
        var data = buffer.ToArray();
        string? type = data.AsSpan().StartsWith(new byte[] {137,80,78,71,13,10,26,10}) ? "image/png"
            : data[0] == 255 && data[1] == 216 && data[2] == 255 ? "image/jpeg"
            : data.AsSpan(0,4).SequenceEqual("RIFF"u8) && data.AsSpan(8,4).SequenceEqual("WEBP"u8) ? "image/webp" : null;
        if (type == null) return BadRequest("Поддерживаются PNG, JPEG и WebP");
        profiles.SetAvatar(Id, data, type);
        rooms.ProfileChanged(Id);
        return Ok(new { avatarUrl = profiles.AvatarUrl(Id) });
    }
}
