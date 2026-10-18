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
}
