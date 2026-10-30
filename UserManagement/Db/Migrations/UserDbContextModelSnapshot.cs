using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using authorization;

#nullable disable

namespace UserManagement.Db.Migrations
{
    [DbContext(typeof(UserDbContext))]
    partial class UserDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("authorization.GameResult", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<DateTime>("GameDate")
                        .HasColumnType("timestamp with time zone")
                        .HasColumnName("game_date");

                    b.Property<string>("GameId")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("game_id");

                    b.Property<bool>("IsWinner")
                        .HasColumnType("boolean")
                        .HasColumnName("is_winner");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("role");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint")
                        .HasColumnName("user_id");

                    b.HasKey("Id");

                    b.HasIndex("GameId")
                        .HasDatabaseName("ix_game_results_game_id");

                    b.HasIndex("UserId", "GameId")
                        .IsUnique()
                        .HasDatabaseName("ux_game_results_user_game");

                    b.ToTable("game_results", null, t =>
                        {
                            t.HasCheckConstraint("ck_game_results_game_id", "length(btrim(game_id)) > 0");

                            t.HasCheckConstraint("ck_game_results_role", "role IN ('Spy', 'Civilian')");
                        });
                });

            modelBuilder.Entity("authorization.UserRecord", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint")
                        .HasColumnName("id");

                    NpgsqlPropertyBuilderExtensions.UseIdentityByDefaultColumn(b.Property<long>("Id"));

                    b.Property<string>("AvatarHash")
                        .HasColumnType("text")
                        .HasColumnName("avatar_hash");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text")
                        .HasColumnName("password_hash");

                    b.Property<string>("Provider")
                        .HasColumnType("text")
                        .HasColumnName("provider");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text")
                        .HasColumnName("provider_key");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("text")
                        .HasColumnName("username");

                    b.HasKey("Id");

                    b.HasIndex("Username")
                        .IsUnique()
                        .HasDatabaseName("ux_users_username");

                    b.HasIndex("Provider", "ProviderKey")
                        .IsUnique()
                        .HasDatabaseName("ux_users_provider")
                        .HasFilter("provider IS NOT NULL AND provider_key IS NOT NULL");

                    b.ToTable("users", null, t =>
                        {
                            t.HasCheckConstraint("ck_users_identity", "(provider IS NULL AND provider_key IS NULL AND password_hash IS NOT NULL) OR (provider IS NOT NULL AND length(btrim(provider)) > 0 AND provider_key IS NOT NULL AND length(btrim(provider_key)) > 0)");

                            t.HasCheckConstraint("ck_users_username", "length(btrim(username)) > 0");
                        });
                });

            modelBuilder.Entity("authorization.GameResult", b =>
                {
                    b.HasOne("authorization.UserRecord", null)
                        .WithMany("GameResults")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("authorization.UserRecord", b =>
                {
                    b.Navigation("GameResults");
                });
#pragma warning restore 612, 618
        }
    }
}
