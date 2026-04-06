using Microsoft.EntityFrameworkCore;
using Lost_Item.Data;
using Lost_Item.Models;

namespace Lost_Item.Tests.Helpers;

/// <summary>
/// Creates a fresh in-memory AppDbContext for each test, pre-seeded with baseline data.
/// </summary>
public static class DbContextFactory
{
    public static AppDbContext Create(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    /// <summary>Seeds a user and returns its id.</summary>
    public static User SeedUser(AppDbContext db, int id = 10, bool isAdmin = false)
    {
        var user = new User
        {
            Id = id,
            GoogleId = $"google_{id}",
            Email = $"user{id}@test.com",
            Name = $"User {id}",
            IsAdmin = isAdmin,
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Mobile SeedMobile(AppDbContext db, string imei = "123456789012345")
    {
        var m = new Mobile
        {
            Type = ProductType.Mobile,
            Brand = "Samsung",
            Model = "Galaxy S21",
            IMEI = imei,
            TrackingId = imei,
            CreatedAt = DateTime.UtcNow
        };
        db.Mobiles.Add(m);
        db.SaveChanges();
        return m;
    }

    public static Bike SeedBike(AppDbContext db, string frameNumber = "FRAME001", string engineNumber = "ENG001")
    {
        var b = new Bike
        {
            Type = ProductType.Bike,
            Brand = "Honda",
            Model = "CB300",
            FrameNumber = frameNumber,
            EngineNumber = engineNumber,
            TrackingId = frameNumber,
            CreatedAt = DateTime.UtcNow
        };
        db.Bikes.Add(b);
        db.SaveChanges();
        return b;
    }

    public static Laptop SeedLaptop(AppDbContext db, string serial = "SN-LAPTOP-001", string? mac = "AA:BB:CC:DD:EE:FF")
    {
        var l = new Laptop
        {
            Type = ProductType.Laptop,
            Brand = "Dell",
            Model = "XPS 15",
            SerialNumber = serial,
            MacAddress = mac,
            TrackingId = serial,
            CreatedAt = DateTime.UtcNow
        };
        db.Laptops.Add(l);
        db.SaveChanges();
        return l;
    }

    public static Complaint SeedComplaint(AppDbContext db, int userId, int productId,
        ComplaintStatus status = ComplaintStatus.Pending, string location = "Dhaka")
    {
        var c = new Complaint
        {
            UserId = userId,
            ProductId = productId,
            LocationStolen = location,
            PoliceReportPath = "report.pdf",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
        db.Complaints.Add(c);
        db.SaveChanges();
        return c;
    }
}
