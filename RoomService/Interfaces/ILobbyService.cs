using authorization;
using GameLogic;

namespace RoomService
{
    public interface ILobbyService
{
    bool PlayerIsReady(UserId id, LobbySession session);
    IReadOnlyDictionary<UserId, PlayerStatus> GetPlayersStatuses(LobbySession session);
    GameSession? StartGame(LobbySession session);
    bool IsStartingNewGame(LobbySession session);
    GameSession GetGameSession(LobbySession session);
    bool TryToEnter(LobbySession session, User user);
    LobbySettings GetLobbySettings(LobbySession session);
    bool SetLobbySettings(LobbySettings settings, LobbySession session);
}
}