using Microsoft.EntityFrameworkCore;

namespace DbConnection
{
    public class DbConnectionFactory
    {
        private readonly Dictionary<string, Action<DbContextOptionsBuilder, string>> _strategies =
        new(StringComparer.OrdinalIgnoreCase)
    {
        { "MySQL", (opt, cs) => opt.UseMySql(cs, ServerVersion.AutoDetect(cs)) },
        { "SQLite", (opt, cs) => opt.UseSqlite(cs) },
        { "PostgreSQL", (opt, cs) => opt.UseNpgsql(cs) }
    };

        public void Configure(DbContextOptionsBuilder options)
        {
            var dbType = Environment.GetEnvironmentVariable("DB_TYPE")
                         ?? throw new ArgumentException("DB_TYPE not set");

            var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
                                   ?? throw new ArgumentException("CONNECTION_STRING not set");

            if (_strategies.TryGetValue(dbType, out var configureDb))
                configureDb(options, connectionString);

            else throw new InvalidOperationException($"Database type {dbType} is not supported");
        }
    }
}