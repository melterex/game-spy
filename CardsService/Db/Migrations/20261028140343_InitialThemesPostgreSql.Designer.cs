using CardsService;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CardsService.Db.Migrations
{
    [DbContext(typeof(ThemeDbContext))]
    [Migration("20261028140343_InitialThemesPostgreSql")]
    partial class InitialThemesPostgreSql
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("CardsService.ThemeRecord", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("text")
                        .HasColumnName("name");

                    b.PrimitiveCollection<string[]>("ThemeResponses")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("theme_responses");

                    b.HasKey("Name");

                    b.ToTable("themes", null, t =>
                        {
                            t.HasCheckConstraint("ck_themes_name", "length(btrim(name)) > 0");
                        });
                });

            modelBuilder.Entity("CardsService.WordRecord", b =>
                {
                    b.Property<string>("ThemeName")
                        .HasColumnType("text")
                        .HasColumnName("theme_name");

                    b.Property<string>("Word")
                        .HasColumnType("text")
                        .HasColumnName("word");

                    b.PrimitiveCollection<string[]>("Descriptions")
                        .IsRequired()
                        .HasColumnType("text[]")
                        .HasColumnName("descriptions");

                    b.Property<int>("Position")
                        .HasColumnType("integer")
                        .HasColumnName("position");

                    b.HasKey("ThemeName", "Word");

                    b.HasIndex("ThemeName", "Position")
                        .IsUnique()
                        .HasDatabaseName("ux_theme_words_theme_position");

                    b.ToTable("theme_words", null, t =>
                        {
                            t.HasCheckConstraint("ck_theme_words_position", "position >= 0");

                            t.HasCheckConstraint("ck_theme_words_word", "length(btrim(word)) > 0");
                        });
                });

            modelBuilder.Entity("CardsService.WordRecord", b =>
                {
                    b.HasOne("CardsService.ThemeRecord", null)
                        .WithMany("Words")
                        .HasForeignKey("ThemeName")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("CardsService.ThemeRecord", b =>
                {
                    b.Navigation("Words");
                });
#pragma warning restore 612, 618
        }
    }
}
