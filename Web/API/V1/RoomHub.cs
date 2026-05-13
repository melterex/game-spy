using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WebAPI.API.V1;

[Authorize]
public class RoomHub : Hub
{
    public async Task EnterRoom()
    {
        await Clients.Group("roomId").SendAsync("EnteredRoom", "id", "nickname", "order");
    }
    
    public async Task MakeReady(bool isReady)
    {
        await Clients.Group("roomId").SendAsync("Ready", "id", true);
    }
    public async Task KickUser(string userId)
    {
        await Clients.Group("roomId").SendAsync("KickUser", userId);
    }

    public async Task StartGame()
    {
        await Clients.Group("roomId").SendAsync("StartGame");
    }

    public async Task MakeTurn(string message)
    {
        await Clients.Group("roomId").SendAsync("TurnMade", "userId", true/*has message*/, "message", true/*has next user to play or start voting*/,"nextUserId");
    }

    public async Task MakeVote(string userId)
    {
        await Clients.Group("roomId").SendAsync("VoteChange", "userId1", "newValue1", "userId2", "newValue2");
    }

    private async Task VoteFinish()
    {
        await Clients.Group("roomId").SendAsync("VoteFinish", "userIdToKick", "wasAmogus");
    }
}