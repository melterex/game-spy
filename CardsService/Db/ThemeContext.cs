using Microsoft.EntityFrameworkCore;
using DbConnection;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CardsService
{
    public class ThemeContext : DbContext
    {
        public DbSet<ThemeModel> Themes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
                new DbConnectionFactory().Configure(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var parser = new ThemesJsonParser();
            var repo = new ThemesJsonRepository(parser);
            var themesFromFile = repo.GetAll();

            modelBuilder.Entity<ThemeModel>().ToTable("Themes");

            modelBuilder.Entity<ThemeModel>()
                .PrimitiveCollection(t => t.Words)
                .HasColumnName("Words");

            if (themesFromFile != null)
            {
                int currentId = 1;
                var seededThemes = new List<ThemeModel>();

                foreach (var item in themesFromFile)
                    seededThemes.Add(item with { Id = currentId++ });

                modelBuilder.Entity<ThemeModel>().HasData(seededThemes);
            }
        }
    }
}