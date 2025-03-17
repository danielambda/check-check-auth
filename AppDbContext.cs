using Microsoft.EntityFrameworkCore;

namespace CheckCheckAuth;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options) {
  public DbSet<User> Users { get; set; } = default!;

  protected override void OnModelCreating(ModelBuilder modelBuilder) {
    modelBuilder.Entity<User>(entity => {
      entity.HasKey(u => u.UserId);
      entity.HasIndex(u => u.Username).IsUnique();
      entity.HasIndex(u => u.TgUserId).IsUnique();
      entity.HasIndex(u => u.TgUsername).IsUnique();
    });
  }
}
