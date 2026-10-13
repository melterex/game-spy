using authorization;

namespace RoomService
{
    public interface IOupService
    {
        Task<string> CreateOup(Guid roomId);
        Task<string> ConsumeOup(Guid roomId, string oup);
    }
}