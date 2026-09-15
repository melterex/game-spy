using System.Security.Claims;
using authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebAPI.Rooms;

namespace WebAPI.API.V1;

[Authorize]
public sealed class RoomHub(RoomCoordinator rooms) : Hub
{
    private UserId Id => UserId.FromString(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public override async Task OnConnectedAsync()
    {
        rooms.Connected(Id, Context.ConnectionId);
        if (rooms.FindRoom(Id) is { } room) await Groups.AddToGroupAsync(Context.ConnectionId, room.ToString());
        await base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        rooms.Disconnected(Id, Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
    public async Task EnterRoom()
    {
        var room = rooms.FindRoom(Id) ?? throw new HubException("Вы не в комнате");
        await Groups.AddToGroupAsync(Context.ConnectionId, room.ToString());
    }
    private static void Run(System.Action action)
    {
        try { action(); }
        catch (RoomRuleException e) { throw new HubException(e.Message); }
    }
    private static UserId Parse(string id) => long.TryParse(id, out var value)
        ? new UserId(value) : throw new HubException("Некорректный игрок");
    public void MakeReady(bool isReady) => Run(() => rooms.Ready(Id, isReady));
    public void AddBot() => Run(() => rooms.AddBot(Id));
    public void KickUser(string userId) => Run(() => rooms.Kick(Id, Parse(userId)));
    public void StartGame() => Run(() => rooms.Start(Id));
    public void MakeTurn(string message) => Run(() => rooms.Turn(Id, message));
    public void MakeVote(string userId) => Run(() => rooms.Vote(Id, Parse(userId)));
    public void MakeReadyEndVote(bool isReady) => Run(() => rooms.ReadyToFinish(Id, isReady));
}
