using System.Text.Json;
using authorization;
using Microsoft.Data.Sqlite;

namespace WebAPI.Rooms;

public sealed class ProfileStore
{
    private readonly string connectionString;
    public ProfileStore(IConfiguration configuration)
    {
        var path = Path.GetFullPath(configuration["Storage:GameDatabase"] ?? "game-data.db");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        connectionString = new SqliteConnectionStringBuilder { DataSource = path }.ToString();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS Avatars (UserId INTEGER PRIMARY KEY, ContentType TEXT NOT NULL, Data BLOB NOT NULL, Version TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS Games (Id TEXT PRIMARY KEY, FinishedAt TEXT NOT NULL, Json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS GamePlayers (GameId TEXT NOT NULL, UserId INTEGER NOT NULL, Outcome TEXT NOT NULL, IsSpy INTEGER NOT NULL, PRIMARY KEY(GameId, UserId));
            CREATE INDEX IF NOT EXISTS IX_GamePlayers_UserId ON GamePlayers(UserId);
            """;
        command.ExecuteNonQuery();
    }
    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }
    public string? AvatarUrl(UserId user)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Version FROM Avatars WHERE UserId=$id";
        command.Parameters.AddWithValue("$id", user.Id);
        return command.ExecuteScalar() is string version ? $"/api/v1/profile/{user}/avatar?v={version}" : null;
    }
    public (byte[] Data, string ContentType)? Avatar(UserId user)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Data, ContentType FROM Avatars WHERE UserId=$id";
        command.Parameters.AddWithValue("$id", user.Id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? ((byte[])reader[0], reader.GetString(1)) : null;
    }
    public void SetAvatar(UserId user, byte[] data, string contentType)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Avatars VALUES ($id,$type,$data,$version) ON CONFLICT(UserId) DO UPDATE SET ContentType=$type,Data=$data,Version=$version";
        command.Parameters.AddWithValue("$id", user.Id);
        command.Parameters.AddWithValue("$type", contentType);
        command.Parameters.AddWithValue("$data", data);
        command.Parameters.AddWithValue("$version", Guid.NewGuid().ToString("N"));
        command.ExecuteNonQuery();
    }
    public void SaveResult(GameResult result)
    {
        using var connection = Open();
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "INSERT OR IGNORE INTO Games VALUES ($id,$date,$json)";
        command.Parameters.AddWithValue("$id", result.GameId.ToString());
        command.Parameters.AddWithValue("$date", result.FinishedAt.ToString("O"));
        command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(result));
        command.ExecuteNonQuery();
        foreach (var player in result.Players.Where(p => !p.IsBot))
        {
            using var participant = connection.CreateCommand();
            participant.Transaction = transaction;
            participant.CommandText = "INSERT OR IGNORE INTO GamePlayers VALUES ($game,$user,$outcome,$spy)";
            participant.Parameters.AddWithValue("$game", result.GameId.ToString());
            participant.Parameters.AddWithValue("$user", long.Parse(player.Id));
            participant.Parameters.AddWithValue("$spy", player.IsSpy);
            participant.Parameters.AddWithValue("$outcome", result.Outcome == "tie" ? "tie"
                : (result.Outcome == "spy") == player.IsSpy ? "win" : "loss");
            participant.ExecuteNonQuery();
        }
        transaction.Commit();
    }
    public object History(UserId user, int offset = 0)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT g.Json FROM Games g JOIN GamePlayers p ON g.Id=p.GameId WHERE p.UserId=$user ORDER BY g.FinishedAt DESC LIMIT 20 OFFSET $offset";
        command.Parameters.AddWithValue("$user", user.Id);
        command.Parameters.AddWithValue("$offset", Math.Max(0, offset));
        var results = new List<GameResult>();
        using (var reader = command.ExecuteReader())
            while (reader.Read()) results.Add(JsonSerializer.Deserialize<GameResult>(reader.GetString(0))!);
        using var stats = connection.CreateCommand();
        stats.CommandText = "SELECT COUNT(*), COALESCE(SUM(Outcome='win'),0), COALESCE(SUM(Outcome='loss'),0), COALESCE(SUM(Outcome='tie'),0), COALESCE(SUM(IsSpy),0) FROM GamePlayers WHERE UserId=$user";
        stats.Parameters.AddWithValue("$user", user.Id);
        using var row = stats.ExecuteReader();
        row.Read();
        return new { games = results, total = row.GetInt32(0), wins = row.GetInt32(1), losses = row.GetInt32(2), ties = row.GetInt32(3), spyGames = row.GetInt32(4) };
    }
}
