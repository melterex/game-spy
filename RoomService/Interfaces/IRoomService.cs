using authorization;

namespace RoomService
{
    public interface IRoomService
    {
        void CreateRoom(RoomType type, User creator);
        bool DeleteRoom(Guid roomId);
        Room? GetRoomByUserId(UserId id);
        Room? GetRoomByRoomId(Guid id);
        bool JoinRoomById(Guid id, User user, string inputPassword); 
        IReadOnlyDictionary<Guid, Room> GetRooms();
    }
}