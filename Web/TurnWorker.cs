using authorization;
using Microsoft.AspNetCore.SignalR;
using WebAPI.API.V1;
using WebAPI.Rooms;

namespace WebAPI;

public sealed class TurnWorker(RoomCoordinator rooms, IHubContext<RoomHub> hub, ILogger<TurnWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                rooms.Tick();
                while (rooms.Events.Reader.TryRead(out var change))
                {
                    if (change.UserId != null)
                    {
                        foreach (var connection in rooms.Connections(UserId.FromString(change.UserId)))
                            await hub.Groups.RemoveFromGroupAsync(connection, change.Group!, stoppingToken);
                        await hub.Clients.User(change.UserId).SendAsync(change.Name, change.Group, stoppingToken);
                    }
                    else if (change.Group == null) await hub.Clients.All.SendAsync(change.Name, stoppingToken);
                    else await hub.Clients.Group(change.Group).SendAsync(change.Name, stoppingToken);
                }
            }
            catch (Exception e) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(e, "Failed to process room update; will retry on next tick");
            }
        }
    }
}
