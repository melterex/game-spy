using System.Collections.Concurrent;
using authorization;
using Microsoft.Extensions.Logging;

namespace RoomService
{
    public class RoomService(ILogger<RoomService> logger, ILoggerFactory loggerFactory) : IRoomService
    {
        private ConcurrentDictionary<Guid, Room> _rooms = new();

        public void CreateRoom(RoomType type, User creator)
        {
            var sessionLogger = loggerFactory.CreateLogger<RoomServiceLobbySession>();
            
            var room = new Room(creator.Id, type)
            {
                Session = new RoomServiceLobbySession(creator.Id, sessionLogger)
            };
            _rooms[room.RoomId] = room;

            room.Session.AddPlayerByUser(creator);
            logger.LogInformation(
                "Room {RoomId} successfully created with creatorId {CreatorId}",
                room.RoomId, creator.Id
                );
        }

        public Room? GetRoomByUserId(UserId id)
        {
            foreach (var room in _rooms.Values)
                if (room.Session.GetPlayers().TryGetValue(id, out var _))
                    return room;

            logger.LogInformation("Haven`t found Player with id {Id}", id);
            return null;
        }

        public Room? GetRoomByRoomId(Guid id)
        {
            if (_rooms.TryGetValue(id, out var room))
                return room;

            logger.LogDebug("Haven`t found Room with id {Id}", id);
            return null;
        }

        public bool JoinRoomById(Guid id, User user, string inputPassword)
        {
            if (!_rooms.TryGetValue(id, out var room))
            {
                logger.LogInformation("Failed to find room by roomId {RoomId}", id);
                return false;
            }

            return room.Session.AddPlayerByUser(user, inputPassword);
        }

        public IReadOnlyDictionary<Guid, Room> GetRooms() => _rooms.AsReadOnly();
    }
}