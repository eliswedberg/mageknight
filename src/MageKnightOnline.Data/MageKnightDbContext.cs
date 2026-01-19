using MageKnightOnline.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace MageKnightOnline.Data;

public class MageKnightDbContext : DbContext
{
    public MageKnightDbContext(DbContextOptions<MageKnightDbContext> options) 
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GamePlayer> GamePlayers => Set<GamePlayer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
        });

        // Game configuration
        modelBuilder.Entity<Game>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ScenarioId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
            
            // JSON columns (stored as TEXT in SQLite)
            entity.Property(e => e.Settings).HasColumnType("TEXT");
            entity.Property(e => e.GameState).HasColumnType("TEXT");

            // Relationships
            entity.HasOne(e => e.CreatedBy)
                  .WithMany(u => u.CreatedGames)
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // GamePlayer configuration
        modelBuilder.Entity<GamePlayer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.GameId, e.UserId }).IsUnique();
            entity.Property(e => e.HeroId).HasMaxLength(50);

            // Relationships
            entity.HasOne(e => e.Game)
                  .WithMany(g => g.Players)
                  .HasForeignKey(e => e.GameId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.GamePlayers)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
