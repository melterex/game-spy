using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace RoomService
{
    public class OupService(IDistributedCache cache, ILogger<OupService> logger)
    : IOupService
    {
        private static readonly TimeSpan OupLifetime = TimeSpan.FromMinutes(15);

        public async Task<string> CreateOup(Guid roomId)
        {
            var oup = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16))
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
            var cacheKey = $"oup:{roomId}:{oup}";

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = OupLifetime
            };

            await cache.SetStringAsync(cacheKey, "valid", options);

            logger.LogDebug("OUP created for room {RoomId} with TTL {TtlMinutes} minutes",
                roomId, OupLifetime.TotalMinutes);

            return oup;
        }

        public async Task<bool> ConsumeOup(Guid roomId, string oup)
        {
            if (string.IsNullOrWhiteSpace(oup))
                return false;

            var cacheKey = $"oup:{roomId}:{oup}";
            var cachedValue = await cache.GetStringAsync(cacheKey);
            if (cachedValue != "valid")
                return false;

            await cache.RemoveAsync(cacheKey);
            return true;
        }
    }
}