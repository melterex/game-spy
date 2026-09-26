using authorization;
using GameLogic.Entities;

namespace GameLogic.Interfaces;

public interface IGameWorker
{
    Task OnTurnMadeAsync(Guid roomId, SlotID slotId, string message, SlotID? nextSlotId);

    Task OnVoteMadeAsync(Guid roomId, VotingReport report);

    Task MakeReadyEndVoteAsync(Guid roomId, GameSession gameSession, SlotID slotId, bool isReady);
}
