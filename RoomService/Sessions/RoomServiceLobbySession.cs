using System.Collections.Concurrent;
using authorization;
using GameLogic.Entities;
using GameLogic.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace RoomService
{
    public class RoomServiceLobbySession(
        UserId creatorId, ILogger<RoomServiceLobbySession> logger)
        : LobbySession
    {
        private ConcurrentDictionary<UserId, User> _players = new();
        private Dictionary<UserId, PlayerStatus> _statuses = new();

        public UserId CreatorId { get; private set; } = creatorId;
        public bool IsStartingNewGame { get; private set; } = false;
        public GameSession? Session { get; private set; }

        public LobbySettings Settings { get; set; } = new(
            Name: "Room" + Guid.NewGuid().ToString()[..4],
            PasswordHash: "",
            MaxPlayers: 8,
            BotCount: 0,
            Status: RoomStatus.Waiting,
            Theme: "Default",
            Mode: ThemesMode.Fixed,
            MoveTime: TimeSpan.FromSeconds(30)
        );

        public bool HasPlayer(UserId id) => _players.ContainsKey(id);

        public bool AddPlayerByUser(User user, string inputPassword = "")
        {
            if (_players.ContainsKey(user.Id))
            {
                logger.LogWarning(
                    "User {Username} (Id: {UserId}) failed to enter: already in this lobby",
                    user.Username, user.Id
                    );
                return false;
            }
            if (_players.Count >= Settings.MaxPlayers)
            {
                logger.LogWarning(
                    "User {Username} (Id: {UserId}) failed to enter: lobby is full (Max: {MaxPlayers})",
                    user.Username, user.Id, Settings.MaxPlayers
                    );
                return false;
            }
            if (!string.IsNullOrEmpty(Settings.PasswordHash))
            {
                var hasher = new PasswordHasher<User>();
                var verifyRes = hasher.VerifyHashedPassword(user, Settings.PasswordHash, inputPassword);
                if (verifyRes == PasswordVerificationResult.Failed)
                {
                    logger.LogWarning(
                        "User {Username} (Id: {UserId}) failed to enter: incorrect password",
                        user.Username, user.Id
                        );
                    return false;
                }
            }


            _players[user.Id] = user;
            _statuses[user.Id] = PlayerStatus.Waiting;

            logger.LogInformation(
                "User {Username} (Id: {UserId}) successfully joined the lobby", user.Username, user.Id);
            return true;
        }

        public bool KickPlayer(UserId id)
        {
            if (_statuses.Remove(id, out _) && _players.Remove(id, out _))
            {
                logger.LogInformation("Player {UserId} was successfully removed from the lobby", id);
                return true;
            }

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
            if (IsStartingNewGame || !IsAllPlayersReady())
                return null;

            Session = CreateGameSession(gameService);
            IsStartingNewGame = true;
            Settings = Settings with { Status = RoomStatus.InGame };

            logger.LogInformation(
                "Lobby session state changed to InGame. Starting game process with theme {Theme} and {PlayerCount} players",
                Settings.Theme, _players.Count
                );
            return Session;
        }

        public void EndGame()
        {
            foreach (var player in _statuses.Keys)
                _statuses[player] = PlayerStatus.Waiting;

            IsStartingNewGame = false;
            Settings = Settings with { Status = RoomStatus.Waiting };
            Session = null;

            logger.LogInformation("Lobby session reset to Waiting state. Game successfully finished");
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
            int realPlayersCount = _players.Count;
            int allowedBotsCount = Settings.MaxPlayers - realPlayersCount;
            int finalBotCount = Math.Min(Settings.BotCount, allowedBotsCount);

            var gameSettings = new GameSettings
            {
                Theme = Settings.Theme,
                BotCount = finalBotCount
            };

            var guid = gameService.CreateGameSession(_players.Keys.ToList(), gameSettings);
            return gameService.GetGameSessionById(guid);
        }


        public IReadOnlyDictionary<UserId, User> GetPlayers() => _players.AsReadOnly();
        public IReadOnlyDictionary<UserId, PlayerStatus> GetPlayersStatuses() => _statuses.AsReadOnly();
    }
}
