using Microsoft.EntityFrameworkCore;
using Lost_Item.Models;

namespace Lost_Item.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Mobile> Mobiles => Set<Mobile>();
    public DbSet<Bike> Bikes => Set<Bike>();
    public DbSet<Laptop> Laptops => Set<Laptop>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<ComplaintUpdate> ComplaintUpdates => Set<ComplaintUpdate>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TPH inheritance - single Products table with Discriminator column
        modelBuilder.Entity<Product>()
            .HasDiscriminator<string>("ProductType")
            .HasValue<Mobile>("Mobile")
            .HasValue<Bike>("Bike")
            .HasValue<Laptop>("Laptop");

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.TrackingId)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.GoogleId)
            .IsUnique();

        // Seed admin user
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            GoogleId = "ADMIN_SEED_GOOGLE_ID", // replace with real Google ID after first login
            Email = "admin@Lost_Item.local",
            Name = "Admin",
            IsAdmin = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
