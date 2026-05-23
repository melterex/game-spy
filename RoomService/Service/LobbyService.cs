using authorization;
using GameLogic;
using GameLogic.Interfaces;

namespace RoomService
{
    public class LobbyService(IGameService gameService) : ILobbyService
    {
        public bool PlayerIsReady(UserId id, LobbySession session)
        {
            var serviceSession = GetSession(session);

            if (serviceSession.GetPlayersStatuses().TryGetValue(id, out var status))
                return status == PlayerStatus.Ready;

            return false;
        }

        public GameSession? StartGame(LobbySession session)
        {
            var serviceSession = GetSession(session);

            if (serviceSession.IsAllPlayersReady() && !serviceSession.IsStartingNewGame)
                return serviceSession.StartGame(gameService);

            Logger.Log($"Not all players ready or already ingame");
            return null;
        }

        public void EndGame(LobbySession session) => 
            GetSession(session).EndGame();
    
        public bool IsStartingNewGame(LobbySession session) => 
            GetSession(session).IsStartingNewGame;

        public GameSession? GetGameSession(LobbySession session) => 
            GetSession(session).Session;

        public bool TryToEnter(LobbySession session, User user) =>
            GetSession(session).AddPlayerByUser(user);

        public IReadOnlyDictionary<UserId, PlayerStatus> GetPlayersStatuses(LobbySession session) =>
            GetSession(session).GetPlayersStatuses();

        public LobbySettings GetLobbySettings(LobbySession session) =>
            GetSession(session).Settings;
        
        public bool MakeReady(UserId id, LobbySession session) => 
            GetSession(session).ChangePlayerStatus(id, PlayerStatus.Ready);

        public bool KickUserByUserId(UserId id, LobbySession session) =>
            GetSession(session).KickPlayer(id);

        public bool SetLobbySettings(LobbySettings settings, LobbySession session)
        {
            var serviceSession = GetSession(session);

            if (settings.Status == RoomStatus.InGame || settings.Status == RoomStatus.Closed)
                return false;
            
            if (string.IsNullOrWhiteSpace(settings.Theme))
                return false;

            serviceSession.Settings = settings;
            return true;
        }

        private RoomServiceLobbySession GetSession(LobbySession session)
        {
            if (session is RoomServiceLobbySession s)
                return s;

            throw new ArgumentException(
                $"LobbyService expects LobbySession, but got {session.GetType().Name}",
                nameof(session));
        }

    }
}