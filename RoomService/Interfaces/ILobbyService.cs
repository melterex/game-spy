using authorization;

namespace RoomService
{
    public interface ILobbyService
    {
        bool PlayerIsReady(UserId id, LobbySession session);
        IReadOnlyDictionary<UserId, PlayerStatus> GetPlayersStatuses(LobbySession session);
        GameSession? StartGame(LobbySession session);
        bool IsStartingNewGame(LobbySession session);
        GameSession? GetGameSession(LobbySession session);
        bool TryToEnter(LobbySession session, User user, string inputPassword);
        LobbySettings GetLobbySettings(LobbySession session);
        bool SetLobbySettings(LobbySettings settings, LobbySession session);
        bool MakeReady(UserId id, LobbySession session);
        bool KickUserByUserId(UserId initiator, UserId target, LobbySession session);
        void EndGame(LobbySession session);
    }
}