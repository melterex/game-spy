using System.Collections.Concurrent;
using authorization;

namespace RoomService
{
    public class RoomServiceLobbySession(UserId creatorId) : LobbySession
    {
        private ConcurrentDictionary<UserId, User> _players = new();
        private Dictionary<UserId, PlayerStatus> _statuses = new();

        public RoomServiceGameSession GameSession { get; } = new();
        public UserId CreatorId { get; private set; } = creatorId;
        public bool IsStartingNewGame { get; private set; } = false;
        public LobbySettings Settings { get; set; } = new(
            MaxPlayers: 8,
            Status: RoomStatus.Waiting,
            Theme: "Default"
        );

        public bool HasPlayer(UserId id) => _players.ContainsKey(id);

        public bool AddPlayerByUser(User user)
        {
            if (_players.ContainsKey(user.Id))
                return false;

            _players[user.Id] = user;
            _statuses[user.Id] = PlayerStatus.Waiting;

            return true;
        }

        public bool KickPlayer(UserId id)
        {
            if (_statuses.Remove(id, out _) && _players.Remove(id, out _))
                return true;

            return false;
        }

        public bool GiveCreatorTo(UserId id)
        {
            if (_players.TryGetValue(id, out var u))
            {
                CreatorId = u.Id;
                return true;
            }

            return false;
        }

        public RoomServiceGameSession StartGame()
        {
            foreach (var status in _statuses.Values)
                if (status != PlayerStatus.Ready)
                {
                    IsStartingNewGame = false;
                    return GameSession;
                }

            IsStartingNewGame = true;
            return GameSession;
        }

        public bool ChangePlayerStatus(UserId id, PlayerStatus status)
        {
            if (!_statuses.ContainsKey(id))
                return false;

            _statuses[id] = status;
            return true;
        }

        public bool IsAllPlayersReady()
        {
            foreach (var status in _statuses.Values)
                if (status != PlayerStatus.Ready)
                    return false;

            return true;
        }

        public IReadOnlyDictionary<UserId, User> GetPlayers() => _players.AsReadOnly();
        public IReadOnlyDictionary<UserId, PlayerStatus> GetPlayersStatuses() => _statuses.AsReadOnly();
    }
}