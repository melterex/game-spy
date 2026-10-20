using authorization;

namespace RoomService
{
    public class Room(UserId creatorId, RoomType type)
    {
        public UserId CreatorId { get; } = creatorId;
        public RoomType Type { get; } = type;
        public RoomServiceLobbySession Session { get; set; }
        public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

        public Guid RoomId { get; } = Guid.NewGuid();
        public RoomStatus Status => Session?.Settings.Status ?? RoomStatus.Waiting;
        private string _title = string.Empty;

        public string Title
        {
            get => string.IsNullOrEmpty(_title) ? $"Room #{RoomId.ToString()[..4]}" : _title;
            set => _title = value;
        }
    }
}
