using RoomService;

public class RoomCleanupWorker : BackgroundService
{
    private readonly ILogger<RoomCleanupWorker> _logger;
    private readonly IRoomService _roomService;

    public RoomCleanupWorker(
        ILogger<RoomCleanupWorker> logger, IRoomService roomService)
    {
        _logger = logger;
        _roomService = roomService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DoWork();
        }
    }

    private async Task DoWork()
    {
        try
        {
            var rooms = _roomService.GetRooms();

            if (rooms == null || rooms.Count == 0)
            {
                _logger.LogDebug("No rooms to clean up.");
                return;
            }

            var now = DateTime.UtcNow;

            foreach (var room in rooms.Values)
            {
                if (room.Status == RoomStatus.Waiting && (now - room.CreatedAtUtc).TotalMinutes > 30)
                {
                    _logger.LogDebug("Cleaning up room {RoomId} due to inactivity.", room.RoomId);
                    _roomService.DeleteRoom(room.RoomId);
                }

                var players = room.Session.GetPlayers();

                if (players == null || players.Count == 0)
                {
                    _logger.LogDebug("Cleaning up room {RoomId} due to no players.", room.RoomId);
                    _roomService.DeleteRoom(room.RoomId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while cleaning up rooms.");
        }
    }

}