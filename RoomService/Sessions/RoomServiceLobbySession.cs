using System.Collections.Concurrent;
using authorization;
using GameLogic;
using GameLogic.Entities;
using GameLogic.Interfaces;

namespace RoomService
{
    public class RoomServiceLobbySession(UserId creatorId) : LobbySession
    {
        private ConcurrentDictionary<UserId, User> _players = new();
        private Dictionary<UserId, PlayerStatus> _statuses = new();

        public UserId CreatorId { get; private set; } = creatorId;
        public bool IsStartingNewGame { get; private set; } = false;
        public GameSession? Session { get; private set; }
        public event Action? OnGameStarted;

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

        public GameSession? StartGame(IGameService gameService)
        {
            Session = CreateGameSession(gameService);

            foreach (var status in _statuses.Values)
                if (status != PlayerStatus.Ready)
                {
                    IsStartingNewGame = false;
                    return Session;
                }

            IsStartingNewGame = true;
            Settings = new LobbySettings(Settings.MaxPlayers, RoomStatus.InGame, Settings.Theme);
            OnGameStarted?.Invoke();

            return Session;
        }

        public void EndGame(IGameService gameService)
        {
            foreach (var player in _statuses.Keys)
                _statuses[player] = PlayerStatus.Waiting;

            IsStartingNewGame = false;
            Settings = Settings with { Status = RoomStatus.Waiting };
            Session = CreateGameSession(gameService);
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

        private GameSession CreateGameSession(IGameService gameService)
        {
            var gameSettings = new GameSettings { Theme = Settings.Theme };
            var guid = gameService.CreateGameSession(_players.Keys.ToList(), gameSettings);

            return gameService.GetGameSessionById(guid);
        }

        public IReadOnlyDictionary<UserId, User> GetPlayers() => _players.AsReadOnly();
        public IReadOnlyDictionary<UserId, PlayerStatus> GetPlayersStatuses() => _statuses.AsReadOnly();
    }
}