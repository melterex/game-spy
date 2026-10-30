using DbConnection;
using Microsoft.EntityFrameworkCore;

namespace CardsService;

public class ThemeDbContext : DbContext
{
    public DbSet<ThemeRecord> Themes => Set<ThemeRecord>();
    public DbSet<WordRecord> Words => Set<WordRecord>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("DB_TYPE"), "PostgreSQL",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("CardsService requires DB_TYPE=PostgreSQL.");

        new DbConnectionFactory().Configure(optionsBuilder);
        optionsBuilder.UseNpgsql(options =>
            options.MigrationsHistoryTable("__CardsServiceMigrationsHistory"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ThemeRecord>(entity =>
        {
            entity.ToTable("themes", table =>
                table.HasCheckConstraint("ck_themes_name", "length(btrim(name)) > 0"));
            entity.HasKey(theme => theme.Name);
            entity.Property(theme => theme.Name).HasColumnName("name").IsRequired();
            entity.Property(theme => theme.ThemeResponses).HasColumnName("theme_responses")
                .HasColumnType("text[]").IsRequired();
        });

        modelBuilder.Entity<WordRecord>(entity =>
        {
            entity.ToTable("theme_words", table =>
            {
                table.HasCheckConstraint("ck_theme_words_word", "length(btrim(word)) > 0");
                table.HasCheckConstraint("ck_theme_words_position", "position >= 0");
            });
            entity.HasKey(word => new { word.ThemeName, word.Word });
            entity.Property(word => word.ThemeName).HasColumnName("theme_name").IsRequired();
            entity.Property(word => word.Word).HasColumnName("word").IsRequired();
            entity.Property(word => word.Position).HasColumnName("position");
            entity.Property(word => word.Descriptions).HasColumnName("descriptions")
                .HasColumnType("text[]").IsRequired();
            entity.HasOne<ThemeRecord>().WithMany(theme => theme.Words)
                .HasForeignKey(word => word.ThemeName).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(word => new { word.ThemeName, word.Position }).IsUnique()
                .HasDatabaseName("ux_theme_words_theme_position");
        });
    }
}
