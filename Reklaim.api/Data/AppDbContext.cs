namespace Reklaim.api.Data;

using Microsoft.EntityFrameworkCore;
using Reklaim.api.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<ItemPost> Posts { get; set; } = null!;
    public DbSet<ClaimRequest> Claims { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User -> Posts (One-to-Many)
        modelBuilder.Entity<User>()
            .HasMany(u => u.Posts)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> Claims (One-to-Many)
        modelBuilder.Entity<User>()
            .HasMany(u => u.Claims)
            .WithOne(c => c.ClaimerUser)
            .HasForeignKey(c => c.ClaimerUserId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading delete to avoid multiple paths

        // Post -> Claims (One-to-Many)
        modelBuilder.Entity<ItemPost>()
            .HasMany(p => p.Claims)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Make email unique constraint
        modelBuilder.Entity<User>()
            .HasIndex(u => u.StudentEmail)
            .IsUnique();
    }
}
