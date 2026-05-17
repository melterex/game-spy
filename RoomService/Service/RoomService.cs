using System.Collections.Concurrent;
using authorization;
using GameLogic;

namespace RoomService
{
    public class RoomService() : IRoomService
    {
        private ConcurrentDictionary<Guid, Room> _rooms = new();

        public void CreateRoom(RoomType type, UserId creatorId)
        {
            var room = new Room(creatorId, type);
            _rooms[room.RoomId] = room;   
        }

        public Room? GetRoomByUserId(UserId id){
            foreach (var room in _rooms.Values)
                if( room.Session.GetPlayers().TryGetValue(id, out var _))
                    return room;
            
            Logger.Log($"Haven`t found Player with id {id}");
            return null;
        }

        public Room? GetRoomByRoomId(Guid id)
        {
            if (_rooms.TryGetValue(id, out var room))
                return room;
            
            Logger.Log($"Haven`t found Room with id {id}");
            return null;
        }
        public IReadOnlyDictionary<Guid, Room> GetRooms() => _rooms.AsReadOnly();
    }
}