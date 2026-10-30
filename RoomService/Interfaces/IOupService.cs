using authorization;

namespace RoomService
{
    public interface IOupService
    {
        Task<string> CreateOup(Guid roomId);
        Task<bool> ConsumeOup(Guid roomId, string oup);
    }
}