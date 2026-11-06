using Microsoft.EntityFrameworkCore;
using YensTelegramLuckyDraw.Models;

namespace YensTelegramLuckyDraw.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<DrawState> DrawStates { get; set; } = null!;
    public DbSet<SpinResult> SpinResults { get; set; } = null!;
    public DbSet<LateralPuzzleSession> LateralPuzzleSessions { get; set; } = null!;
    public DbSet<LateralPuzzleMessage> LateralPuzzleMessages { get; set; } = null!;
    public DbSet<LateralPuzzleParticipant> LateralPuzzleParticipants { get; set; } = null!;
    public DbSet<PlayedLateralPuzzle> PlayedLateralPuzzles { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LateralPuzzleSession>()
            .HasMany(session => session.Messages)
            .WithOne(message => message.Session)
            .HasForeignKey(message => message.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LateralPuzzleSession>()
            .HasMany(session => session.Participants)
            .WithOne(participant => participant.Session)
            .HasForeignKey(participant => participant.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LateralPuzzleParticipant>()
            .HasIndex(participant => new { participant.SessionId, participant.TelegramUserId })
            .IsUnique();

        modelBuilder.Entity<PlayedLateralPuzzle>()
            .HasIndex(played => new { played.PuzzleNumber, played.TelegramUserId })
            .IsUnique();
    }
}
