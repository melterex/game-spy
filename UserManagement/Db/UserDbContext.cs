using DbConnection;
using Microsoft.EntityFrameworkCore;

namespace authorization;

public class UserDbContext : DbContext
{
    public DbSet<UserRecord> Users => Set<UserRecord>();
    public DbSet<GameResult> GameResults => Set<GameResult>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("DB_TYPE"), "PostgreSQL",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("UserManagement requires DB_TYPE=PostgreSQL.");

        new DbConnectionFactory().Configure(optionsBuilder);
        optionsBuilder.UseNpgsql(options =>
            options.MigrationsHistoryTable("__UserManagementMigrationsHistory"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRecord>(entity =>
        {
            entity.ToTable("users", table =>
            {
                table.HasCheckConstraint("ck_users_identity",
                    "(provider IS NULL AND provider_key IS NULL AND password_hash IS NOT NULL) OR " +
                    "(provider IS NOT NULL AND length(btrim(provider)) > 0 AND " +
                    "provider_key IS NOT NULL AND length(btrim(provider_key)) > 0)");
                table.HasCheckConstraint("ck_users_username", "length(btrim(username)) > 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            entity.Property(x => x.Username).HasColumnName("username").IsRequired();
            entity.Property(x => x.PasswordHash).HasColumnName("password_hash");
            entity.Property(x => x.Provider).HasColumnName("provider");
            entity.Property(x => x.ProviderKey).HasColumnName("provider_key");
            entity.HasIndex(x => x.Username).IsUnique().HasDatabaseName("ux_users_username");
            entity.HasIndex(x => new { x.Provider, x.ProviderKey }).IsUnique()
                .HasFilter("provider IS NOT NULL AND provider_key IS NOT NULL")
                .HasDatabaseName("ux_users_provider");
        });

        modelBuilder.Entity<GameResult>(entity =>
        {
            entity.ToTable("game_results", table =>
            {
                table.HasCheckConstraint("ck_game_results_role", "role IN ('Spy', 'Civilian')");
                table.HasCheckConstraint("ck_game_results_game_id", "length(btrim(game_id)) > 0");
            });
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.GameId).HasColumnName("game_id").IsRequired();
            entity.Property(x => x.Role).HasColumnName("role").HasConversion<string>();
            entity.Property(x => x.IsWinner).HasColumnName("is_winner");
            entity.Property(x => x.GameDate).HasColumnName("game_date")
                .HasColumnType("timestamp with time zone");
            entity.HasOne<UserRecord>().WithMany(x => x.GameResults)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.UserId, x.GameId }).IsUnique()
                .HasDatabaseName("ux_game_results_user_game");
            entity.HasIndex(x => x.GameId).HasDatabaseName("ix_game_results_game_id");
        });
    }
}
