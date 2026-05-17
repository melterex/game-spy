using Microsoft.Data.Sqlite;

namespace authorization;

public static class UserStorage
{
    private static readonly string _connectionString = "Data Source=users.db";
    private static readonly IPasswordHasher _hasher = new BCryptPasswordHasher();

    static UserStorage()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                PasswordHash TEXT NOT NULL
            )
        ";
        command.ExecuteNonQuery();
    }

    public static User? FindByUsername(string username)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username FROM Users WHERE Username = $username";
        command.Parameters.AddWithValue("$username", username);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new User(reader.GetString(1), new UserId(reader.GetInt64(0)));
        }
        
        return null;
    }

    public static User? FindById(UserId id)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username FROM Users WHERE Id = $id";
        command.Parameters.AddWithValue("$id", id.Id);
        
        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return new User(reader.GetString(1), new UserId(reader.GetInt64(0)));
        }
        
        return null;
    }

    public static bool UsernameExists(string username)
    {
        return FindByUsername(username) != null;
    }

    public static void Add(User user, string password)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Users (Username, PasswordHash)
            VALUES ($username, $passwordHash)
        ";
        command.Parameters.AddWithValue("$username", user.Username);
        command.Parameters.AddWithValue("$passwordHash", _hasher.Hash(password));
        
        command.ExecuteNonQuery();
    }

    public static bool CheckPassword(string username, string password)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "SELECT PasswordHash FROM Users WHERE Username = $username";
        command.Parameters.AddWithValue("$username", username);
        
        var result = command.ExecuteScalar();
        if (result == null || result == DBNull.Value)
        {
            return false;
        }
        
        string? hash = result.ToString();
        if (string.IsNullOrEmpty(hash))
        {
            return false;
        }
        
        return _hasher.Verify(password, hash);
    }
    
    public static long GetNextId()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = "SELECT COALESCE(MAX(Id), 0) FROM Users";
        var result = command.ExecuteScalar();
        long maxId = Convert.ToInt64(result);
        return maxId + 1;
    }
}