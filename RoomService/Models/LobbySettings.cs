namespace RoomService
{
    public record LobbySettings(int MaxPlayers, RoomStatus Status, string Theme);
}