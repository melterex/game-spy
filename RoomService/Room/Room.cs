using authorization;
using GameLogic;

namespace RoomService
{
    public class Room(UserId creatorId, RoomType type, LobbySettings settings)
    {
        public UserId CreatorId { get; } = creatorId;
        public RoomType Type { get; } = type;
        public LobbySession? Session { get; }

        public Guid RoomId { get; } = Guid.NewGuid();
        public string Title => "Room #" + RoomId.ToString()[..4];
        public RoomStatus Status { get; private set; } = RoomStatus.Waiting;
        public LobbySettings Settings { get; } = settings;

        public void ChangeRoomStatus(RoomStatus newStatus) => Status = newStatus;
    }
}