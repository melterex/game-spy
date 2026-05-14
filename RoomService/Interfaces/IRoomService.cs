using authorization;

namespace RoomService
{
    public interface IRoomService
    {
        void CreateRoom(RoomType type, UserId creatorId);
        Room? GetRoomByUserId(UserId id);
        Room? GetRoomByRoomId(Guid id);
        IReadOnlyDictionary<Guid, Room> GetRooms();
    }
    
}