using Microsoft.EntityFrameworkCore;

namespace authorization;

public static class UserStorage
{
    private static readonly IPasswordHasher Hasher = new BCryptPasswordHasher();

    public static void Initialize()
    {
        using var db = new UserDbContext();
        db.Database.Migrate();
    }

    public static User? FindByUsername(string username)
    {
        using var db = new UserDbContext();
        return db.Users.AsNoTracking().SingleOrDefault(x => x.Username == username)?.ToUser();
    }

    public static User? FindById(UserId id)
    {
        using var db = new UserDbContext();
        return db.Users.AsNoTracking().SingleOrDefault(x => x.Id == id.Id)?.ToUser();
    }

    public static User? FindByProvider(string provider, string providerKey)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(providerKey))
            return null;
        provider = provider.Trim();
        providerKey = providerKey.Trim();
        using var db = new UserDbContext();
        return db.Users.AsNoTracking()
            .SingleOrDefault(x => x.Provider == provider && x.ProviderKey == providerKey)?.ToUser();
    }

    public static bool UsernameExists(string username)
    {
        using var db = new UserDbContext();
        return db.Users.Any(x => x.Username == username);
    }

    public static User Add(string username, string password, string provider = "", string providerKey = "")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        var hasProvider = !string.IsNullOrWhiteSpace(provider);
        if (hasProvider != !string.IsNullOrWhiteSpace(providerKey))
            throw new ArgumentException("Provider and provider key must be supplied together.");
        if (!hasProvider)
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var record = new UserRecord
        {
            Username = username,
            PasswordHash = hasProvider ? null : Hasher.Hash(password),
            Provider = hasProvider ? provider.Trim() : null,
            ProviderKey = hasProvider ? providerKey.Trim() : null
        };
        using var db = new UserDbContext();
        db.Users.Add(record);
        db.SaveChanges();
        return record.ToUser();
    }

    public static bool CheckPassword(string username, string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;
        using var db = new UserDbContext();
        var hash = db.Users.Where(x => x.Username == username && x.Provider == null)
            .Select(x => x.PasswordHash).SingleOrDefault();
        return !string.IsNullOrEmpty(hash) && Hasher.Verify(password, hash);
    }

    public static void RecordGameResult(long userId, string gameId, GameRole role, bool isWinner) =>
        RecordGameResults([new GameResult(userId, gameId, role, isWinner)]);

    public static void RecordGameResults(IEnumerable<GameResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        var records = results.ToList();
        foreach (var result in records)
        {
            if (result.UserId <= 0)
                throw new ArgumentException("Invalid user ID.");
            ArgumentException.ThrowIfNullOrWhiteSpace(result.GameId);
            if (!Enum.IsDefined(result.Role))
                throw new ArgumentException("Invalid game role.");
            if (result.GameDate.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Game dates must be UTC.");
        }
        if (records.Count == 0)
            return;

        using var db = new UserDbContext();
        using var transaction = db.Database.BeginTransaction();
        foreach (var result in records)
        {
            db.Database.ExecuteSqlInterpolated($"""
                INSERT INTO game_results (user_id, game_id, role, is_winner, game_date)
                VALUES ({result.UserId}, {result.GameId}, {result.Role.ToString()},
                        {result.IsWinner}, {result.GameDate})
                ON CONFLICT (user_id, game_id) DO NOTHING
                """);
        }
        transaction.Commit();
    }

    public static PlayerStats GetPlayerStats(long userId)
    {
        using var db = new UserDbContext();
        var stats = StatsQuery(db).SingleOrDefault(x => x.UserId == userId)
            ?? new PlayerStats { UserId = userId, Username = "Unknown" };
        SetWinRate(stats);
        return stats;
    }

    public static List<GameResult> GetGameHistory(long userId)
    {
        using var db = new UserDbContext();
        return db.GameResults.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.GameDate).ThenByDescending(x => x.Id).ToList();
    }

    public static List<PlayerStats> GetLeaderboard(int topCount = 10)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(topCount);
        using var db = new UserDbContext();
        var stats = StatsQuery(db).OrderByDescending(x => x.TotalWins).ThenBy(x => x.UserId)
            .Take(topCount).ToList();
        stats.ForEach(SetWinRate);
        return stats;
    }

    private static IQueryable<PlayerStats> StatsQuery(UserDbContext db) =>
        db.Users.AsNoTracking().Select(user => new PlayerStats
        {
            UserId = user.Id,
            Username = user.Username,
            TotalGames = user.GameResults.Count(),
            TotalWins = user.GameResults.Count(x => x.IsWinner),
            WinsAsSpy = user.GameResults.Count(x => x.IsWinner && x.Role == GameRole.Spy),
            WinsAsCivilian = user.GameResults.Count(x => x.IsWinner && x.Role == GameRole.Civilian)
        });

    private static void SetWinRate(PlayerStats stats) =>
        stats.WinRate = stats.TotalGames == 0 ? 0 : 100.0 * stats.TotalWins / stats.TotalGames;
}
