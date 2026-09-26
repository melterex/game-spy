using Microsoft.EntityFrameworkCore;
using DbConnection;

namespace ImageService
{
    public class ImageContext : DbContext
    {
        public DbSet<ImageModel> Images { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var factory = new DbConnectionFactory();
            factory.Configure(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ImageModel>(entity =>
            {
                entity.ToTable("images");

                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasData(
                    new ImageModel
                    {
                        Id = 1,
                        Hash = ImageConstants.DefaultAvatarHash,
                        Path = "/samples/default.png",
                        Extension = "png",
                        Source = 1
                    }
                );
            });
        }
    }
}