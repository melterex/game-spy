namespace RoomService
{
    public record LobbySettings(
        string Name, 
        string Password, 
        int MaxPlayers, 
        int BotCount,
        RoomStatus Status, 
        string Theme, 
        ThemesMode Mode,
        TimeSpan MoveTime
        );
}