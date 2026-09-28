using authorization;

namespace RoomService
{
    public class Room(UserId creatorId, RoomType type)
    {
        public UserId CreatorId { get; } = creatorId;
        public RoomType Type { get; } = type;
        public RoomServiceLobbySession Session { get; init; }

        public Guid RoomId { get; } = Guid.NewGuid();
        public RoomStatus Status { get; private set; } = RoomStatus.Waiting;
        public string Title => "Room #" + RoomId.ToString()[..4];
    }
}