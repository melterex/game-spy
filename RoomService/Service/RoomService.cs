using System.Collections.Concurrent;
using authorization;

namespace RoomService
{
    public class RoomService : IRoomService
    {
        private ConcurrentDictionary<Guid, Room> _rooms = new();

        public void CreateRoom(RoomType type, User creator)
        {
            var room = new Room(creator.Id, type);
            _rooms[room.RoomId] = room;

            room.Session.AddPlayerByUser(creator);
            room.Session.OnGameStarted += () =>
                { room.ChangeRoomStatus(RoomStatus.InGame); };
        }

        public Room? GetRoomByUserId(UserId id)
        {
            foreach (var room in _rooms.Values)
                if (room.Session.GetPlayers().TryGetValue(id, out var _))
                    return room;

            return null;
        }

        public Room? GetRoomByRoomId(Guid id)
        {
            if (_rooms.TryGetValue(id, out var room))
                return room;

            return null;
        }
        public IReadOnlyDictionary<Guid, Room> GetRooms() => _rooms.AsReadOnly();
    }
}