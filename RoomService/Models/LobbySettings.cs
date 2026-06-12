namespace RoomService
{
    public record LobbySettings(
        string Name, 
        string PasswordHash, 
        int MaxPlayers, 
        RoomStatus Status, 
        string Theme, 
        ThemesMode Mode,
        TimeSpan MoveTime
        );
}