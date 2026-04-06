using Microsoft.Extensions.Logging.Abstractions;
using Lost_Item.DTOs;
using Lost_Item.Models;
using Lost_Item.Services;
using Lost_Item.Tests.Helpers;

namespace Lost_Item.Tests;

public class ProductServiceTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static ProductService BuildService(string dbName)
    {
        var db = DbContextFactory.Create(dbName);
        return new ProductService(db, NullLogger<ProductService>.Instance);
    }

    // ── SearchByIdentifierAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SearchByIdentifier_Mobile_Found_ReturnsResult()
    {
        var db = DbContextFactory.Create(nameof(SearchByIdentifier_Mobile_Found_ReturnsResult));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "111111111111111");
        DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Approved);

        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var result = await svc.SearchByIdentifierAsync("111111111111111", ProductType.Mobile);

        Assert.NotNull(result);
        Assert.Equal("111111111111111", result.TrackingId);
        Assert.Equal("Mobile", result.Type);
        Assert.True(result.IsStolen);
        Assert.Single(result.OpenComplaints);
    }

    [Fact]
    public async Task SearchByIdentifier_Mobile_NotFound_ReturnsNull()
    {
        var svc = BuildService(nameof(SearchByIdentifier_Mobile_NotFound_ReturnsNull));

        var result = await svc.SearchByIdentifierAsync("999999999999999", ProductType.Mobile);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchByIdentifier_Bike_ByFrameNumber_ReturnsResult()
    {
        var db = DbContextFactory.Create(nameof(SearchByIdentifier_Bike_ByFrameNumber_ReturnsResult));
        var user = DbContextFactory.SeedUser(db);
        var bike = DbContextFactory.SeedBike(db, "FRAME-X", "ENG-X");
        DbContextFactory.SeedComplaint(db, user.Id, bike.Id, ComplaintStatus.Approved);

        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var result = await svc.SearchByIdentifierAsync("FRAME-X", ProductType.Bike);

        Assert.NotNull(result);
        Assert.Equal("Bike", result.Type);
        Assert.True(result.IsStolen);
    }

    [Fact]
    public async Task SearchByIdentifier_Bike_ByEngineNumber_ReturnsResult()
    {
        var db = DbContextFactory.Create(nameof(SearchByIdentifier_Bike_ByEngineNumber_ReturnsResult));
        var user = DbContextFactory.SeedUser(db);
        var bike = DbContextFactory.SeedBike(db, "FRAME-Y", "ENG-Y");
        DbContextFactory.SeedComplaint(db, user.Id, bike.Id, ComplaintStatus.Approved);

        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var result = await svc.SearchByIdentifierAsync("ENG-Y", ProductType.Bike);

        Assert.NotNull(result);
        Assert.Equal("Bike", result.Type);
    }

    [Fact]
    public async Task SearchByIdentifier_Laptop_BySerialNumber_ReturnsResult()
    {
        var db = DbContextFactory.Create(nameof(SearchByIdentifier_Laptop_BySerialNumber_ReturnsResult));
        var user = DbContextFactory.SeedUser(db);
        var laptop = DbContextFactory.SeedLaptop(db, "SN-001", "AA:BB:CC:DD:EE:01");
        DbContextFactory.SeedComplaint(db, user.Id, laptop.Id, ComplaintStatus.Approved);

        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var result = await svc.SearchByIdentifierAsync("SN-001", ProductType.Laptop);

        Assert.NotNull(result);
        Assert.Equal("Laptop", result.Type);
    }

    [Fact]
    public async Task SearchByIdentifier_Laptop_ByMacAddress_ReturnsResult()
    {
        var db = DbContextFactory.Create(nameof(SearchByIdentifier_Laptop_ByMacAddress_ReturnsResult));
        var user = DbContextFactory.SeedUser(db);
        var laptop = DbContextFactory.SeedLaptop(db, "SN-002", "AA:BB:CC:DD:EE:02");
        DbContextFactory.SeedComplaint(db, user.Id, laptop.Id, ComplaintStatus.Approved);

        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var result = await svc.SearchByIdentifierAsync("AA:BB:CC:DD:EE:02", ProductType.Laptop);

        Assert.NotNull(result);
        Assert.Equal("Laptop", result.Type);
    }

    [Fact]
    public async Task SearchByIdentifier_ResolvedComplaint_ReturnsNull()
    {
        var db = DbContextFactory.Create(nameof(SearchByIdentifier_ResolvedComplaint_ReturnsNull));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "222222222222222");
        DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Resolved);

        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var result = await svc.SearchByIdentifierAsync("222222222222222", ProductType.Mobile);

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchByIdentifier_PendingComplaint_NotShownAsStolen()
    {
        var db = DbContextFactory.Create(nameof(SearchByIdentifier_PendingComplaint_NotShownAsStolen));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "333333333333333");
        DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);

        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var result = await svc.SearchByIdentifierAsync("333333333333333", ProductType.Mobile);

        // SearchByIdentifierAsync only surfaces Approved complaints as "open".
        // HasOpenComplaintAsync treats both Pending and Approved as "open" to block duplicate filing.
        // These two methods intentionally differ: search is public-facing (only shows approved),
        // while HasOpenComplaint is used internally to prevent duplicate complaints.
        Assert.NotNull(result);
        Assert.False(result.IsStolen);
        Assert.Empty(result.OpenComplaints);
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Mobile_CreatesProductSuccessfully()
    {
        var svc = BuildService(nameof(CreateAsync_Mobile_CreatesProductSuccessfully));

        var (product, error) = await svc.CreateAsync(ProductType.Mobile, "Apple", "iPhone 14",
            imei: "444444444444444", null, null, null, null);

        Assert.Null(error);
        Assert.NotNull(product);
        Assert.IsType<Mobile>(product);
        Assert.Equal("444444444444444", ((Mobile)product).IMEI);
        Assert.Equal("Apple", product.Brand);
    }

    [Fact]
    public async Task CreateAsync_Bike_CreatesProductSuccessfully()
    {
        var svc = BuildService(nameof(CreateAsync_Bike_CreatesProductSuccessfully));

        var (product, error) = await svc.CreateAsync(ProductType.Bike, "Yamaha", "FZ",
            null, "FRAME-B1", "ENG-B1", null, null);

        Assert.Null(error);
        Assert.NotNull(product);
        Assert.IsType<Bike>(product);
        Assert.Equal("FRAME-B1", ((Bike)product).FrameNumber);
    }

    [Fact]
    public async Task CreateAsync_Laptop_CreatesProductSuccessfully()
    {
        var svc = BuildService(nameof(CreateAsync_Laptop_CreatesProductSuccessfully));

        var (product, error) = await svc.CreateAsync(ProductType.Laptop, "Lenovo", "ThinkPad",
            null, null, null, "SN-THINK-01", "11:22:33:44:55:66");

        Assert.Null(error);
        Assert.NotNull(product);
        Assert.IsType<Laptop>(product);
        Assert.Equal("SN-THINK-01", ((Laptop)product).SerialNumber);
    }

    // ── CreateMobileAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateMobileAsync_Success()
    {
        var svc = BuildService(nameof(CreateMobileAsync_Success));
        var req = new CreateMobileRequest("Samsung", "S23", "555555555555555");

        var (result, error) = await svc.CreateMobileAsync(req);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("555555555555555", result.TrackingId);
    }

    [Fact]
    public async Task CreateMobileAsync_EmptyIMEI_ReturnsError()
    {
        var svc = BuildService(nameof(CreateMobileAsync_EmptyIMEI_ReturnsError));
        var req = new CreateMobileRequest("Samsung", "S23", "");

        var (result, error) = await svc.CreateMobileAsync(req);

        Assert.Null(result);
        Assert.Equal("IMEI is required", error);
    }

    [Fact]
    public async Task CreateMobileAsync_DuplicateIMEI_ReturnsError()
    {
        var db = DbContextFactory.Create(nameof(CreateMobileAsync_DuplicateIMEI_ReturnsError));
        DbContextFactory.SeedMobile(db, "666666666666666");
        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var (result, error) = await svc.CreateMobileAsync(new CreateMobileRequest("A", "B", "666666666666666"));

        Assert.Null(result);
        Assert.NotNull(error);
        Assert.Contains("666666666666666", error);
    }

    // ── CreateBikeAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBikeAsync_Success()
    {
        var svc = BuildService(nameof(CreateBikeAsync_Success));
        var req = new CreateBikeRequest("Honda", "CB200", "FN-001", "EN-001");

        var (result, error) = await svc.CreateBikeAsync(req);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("FN-001", result.TrackingId);
    }

    [Fact]
    public async Task CreateBikeAsync_EmptyFrameNumber_ReturnsError()
    {
        var svc = BuildService(nameof(CreateBikeAsync_EmptyFrameNumber_ReturnsError));
        var req = new CreateBikeRequest("Honda", "CB200", "", "EN-002");

        var (result, error) = await svc.CreateBikeAsync(req);

        Assert.Null(result);
        Assert.Equal("Frame number is required", error);
    }

    // ── CreateLaptopAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateLaptopAsync_Success()
    {
        var svc = BuildService(nameof(CreateLaptopAsync_Success));
        var req = new CreateLaptopRequest("HP", "Spectre", "HP-SN-001", null);

        var (result, error) = await svc.CreateLaptopAsync(req);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("HP-SN-001", result.TrackingId);
    }

    [Fact]
    public async Task CreateLaptopAsync_EmptySerial_ReturnsError()
    {
        var svc = BuildService(nameof(CreateLaptopAsync_EmptySerial_ReturnsError));
        var req = new CreateLaptopRequest("HP", "Spectre", "", null);

        var (result, error) = await svc.CreateLaptopAsync(req);

        Assert.Null(result);
        Assert.Equal("Serial number is required", error);
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_UpdatesBrandAndModel()
    {
        var db = DbContextFactory.Create(nameof(UpdateAsync_UpdatesBrandAndModel));
        var mobile = DbContextFactory.SeedMobile(db, "777777777777777");
        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var (success, error) = await svc.UpdateAsync(mobile.Id, new UpdateProductRequest("NewBrand", "NewModel", null));

        Assert.True(success);
        Assert.Null(error);
        var updated = await db.Products.FindAsync(mobile.Id);
        Assert.Equal("NewBrand", updated!.Brand);
        Assert.Equal("NewModel", updated.Model);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsError()
    {
        var svc = BuildService(nameof(UpdateAsync_NotFound_ReturnsError));

        var (success, error) = await svc.UpdateAsync(9999, new UpdateProductRequest("X", "Y", null));

        Assert.False(success);
        Assert.Equal("Product not found", error);
    }

    [Fact]
    public async Task UpdateAsync_Bike_UpdatesEngineNumber()
    {
        var db = DbContextFactory.Create(nameof(UpdateAsync_Bike_UpdatesEngineNumber));
        var bike = DbContextFactory.SeedBike(db, "FRAME-UPD", "ENG-OLD");
        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var (success, error) = await svc.UpdateAsync(bike.Id, new UpdateProductRequest(null, null, "ENG-NEW"));

        Assert.True(success);
        Assert.Null(error);
        var updated = (Bike)(await db.Products.FindAsync(bike.Id))!;
        Assert.Equal("ENG-NEW", updated.EngineNumber);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_NoComplaints_DeletesProduct()
    {
        var db = DbContextFactory.Create(nameof(DeleteAsync_NoComplaints_DeletesProduct));
        var mobile = DbContextFactory.SeedMobile(db, "888888888888888");
        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var (success, error) = await svc.DeleteAsync(mobile.Id);

        Assert.True(success);
        Assert.Null(error);
        Assert.Null(await db.Products.FindAsync(mobile.Id));
    }

    [Fact]
    public async Task DeleteAsync_WithOpenComplaint_ReturnsError()
    {
        var db = DbContextFactory.Create(nameof(DeleteAsync_WithOpenComplaint_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "999999999999999");
        DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);
        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var (success, error) = await svc.DeleteAsync(mobile.Id);

        Assert.False(success);
        Assert.Contains("open complaints", error);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsError()
    {
        var svc = BuildService(nameof(DeleteAsync_NotFound_ReturnsError));

        var (success, error) = await svc.DeleteAsync(9999);

        Assert.False(success);
        Assert.Equal("Product not found", error);
    }

    // ── FindByIdentifierAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task FindByIdentifier_Mobile_ReturnsCorrectProduct()
    {
        var db = DbContextFactory.Create(nameof(FindByIdentifier_Mobile_ReturnsCorrectProduct));
        var mobile = DbContextFactory.SeedMobile(db, "123000000000001");
        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var result = await svc.FindByIdentifierAsync(ProductType.Mobile, "123000000000001", null, null);

        Assert.NotNull(result);
        Assert.Equal(mobile.Id, result.Id);
    }

    [Fact]
    public async Task FindByIdentifier_Bike_ByFrameNumber_ReturnsCorrectProduct()
    {
        var db = DbContextFactory.Create(nameof(FindByIdentifier_Bike_ByFrameNumber_ReturnsCorrectProduct));
        var bike = DbContextFactory.SeedBike(db, "FIND-FRAME", "FIND-ENG");
        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var result = await svc.FindByIdentifierAsync(ProductType.Bike, null, "FIND-FRAME", null);

        Assert.NotNull(result);
        Assert.Equal(bike.Id, result.Id);
    }

    [Fact]
    public async Task FindByIdentifier_NullIdentifier_ReturnsNull()
    {
        var svc = BuildService(nameof(FindByIdentifier_NullIdentifier_ReturnsNull));

        var result = await svc.FindByIdentifierAsync(ProductType.Mobile, null, null, null);

        Assert.Null(result);
    }

    // ── GetAllAsync / GetByIdAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts()
    {
        var db = DbContextFactory.Create(nameof(GetAllAsync_ReturnsAllProducts));
        DbContextFactory.SeedMobile(db, "GET-ALL-001");
        DbContextFactory.SeedBike(db, "GET-ALL-002", "ENG-GA-02");
        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var products = await svc.GetAllAsync();

        // InMemory DB seeds admin user (id=1) via HasData, products are added separately
        Assert.True(products.Count >= 2);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsProduct()
    {
        var db = DbContextFactory.Create(nameof(GetByIdAsync_ExistingId_ReturnsProduct));
        var mobile = DbContextFactory.SeedMobile(db, "GET-BY-ID-001");
        var svc = new ProductService(db, NullLogger<ProductService>.Instance);

        var result = await svc.GetByIdAsync(mobile.Id);

        Assert.NotNull(result);
        Assert.Equal(mobile.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentId_ReturnsNull()
    {
        var svc = BuildService(nameof(GetByIdAsync_NonExistentId_ReturnsNull));

        var result = await svc.GetByIdAsync(99999);

        Assert.Null(result);
    }
}
