using authorization;
using GameLogic.Interfaces;
using Microsoft.Extensions.Logging;

namespace RoomService
{
    public class LobbyService(IGameService gameService, ILogger<LobbyService> logger) : ILobbyService
    {
        public bool PlayerIsReady(UserId id, LobbySession session)
        {
            var serviceSession = GetSession(session);

            if (serviceSession.GetPlayersStatuses().TryGetValue(id, out var status))
                return status == PlayerStatus.Ready;

            logger.LogInformation("Failed to find user status by userId {UserId}", id);
            return false;
        }

        public GameSession? StartGame(LobbySession session)
        {
            var serviceSession = GetSession(session);

            if (serviceSession.IsAllPlayersReady() && !serviceSession.IsStartingNewGame)
                return serviceSession.StartGame(gameService);

            logger.LogInformation(
                "Failed to start game: not all players are ready or session is already in-game. CreatorId: {CreatorId}",
                serviceSession.CreatorId
                );
            return null;
        }

        public void EndGame(LobbySession session) =>
            GetSession(session).EndGame();

        public bool IsStartingNewGame(LobbySession session) =>
            GetSession(session).IsStartingNewGame;

        public GameSession? GetGameSession(LobbySession session) =>
            GetSession(session).Session;

        public bool TryToEnter(LobbySession session, User user, string inputPassword) =>
            GetSession(session).AddPlayerByUser(user, inputPassword);

        public bool TryToEnter(LobbySession session, User user) =>
            GetSession(session).AddPlayerByUser(user);

        public IReadOnlyDictionary<UserId, PlayerStatus> GetPlayersStatuses(LobbySession session) =>
            GetSession(session).GetPlayersStatuses();

        public LobbySettings GetLobbySettings(LobbySession session) =>
            GetSession(session).Settings;

        public bool MakeReady(UserId id, LobbySession session) =>
            GetSession(session).ChangePlayerStatus(id, PlayerStatus.Ready);

        public bool KickUserByUserId(UserId initiator, UserId target, LobbySession session)
        {
            var serviceSession = GetSession(session);

            if (serviceSession.CreatorId != initiator || initiator == target)
            {
                logger.LogInformation(
                    "Failed to kick user by userId: user {initiator} is not creator or voiting for yourself",
                    initiator
                    );
                return false;
            }

            return serviceSession.KickPlayer(target);
        }

        public bool SetLobbySettings(LobbySettings settings, LobbySession session)
        {
            var serviceSession = GetSession(session);

            if (settings.Status == RoomStatus.InGame || settings.Status == RoomStatus.Closed)
            {
                logger.LogInformation("Failed to set LobbySettings: room already InGame or Closed");
                return false;
            }

            if (string.IsNullOrWhiteSpace(settings.Theme))
            {
                logger.LogInformation("Failed to set LobbySettings: theme can`t be null");
                return false;
            }

            logger.LogDebug("LobbySetting successfully set");
            serviceSession.Settings = settings;
            return true;
        }

        private RoomServiceLobbySession GetSession(LobbySession session)
        {
            if (session is RoomServiceLobbySession s)
                return s;

            logger.LogCritical("LobbyService expects LobbySession, but got {Session}", session.GetType().Name);
            throw new ArgumentException(
                $"LobbyService expects LobbySession, but got {session.GetType().Name}",
                nameof(session));
        }

    }
}
