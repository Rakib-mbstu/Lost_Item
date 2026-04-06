using Microsoft.EntityFrameworkCore;
using Lost_Item.Data;
using Lost_Item.DTOs;
using Lost_Item.Models;

namespace Lost_Item.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(AppDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<SearchResult?> SearchByIdentifierAsync(string query, ProductType type)
    {
        Product? product = type switch
        {
            ProductType.Mobile => await _db.Products
                .OfType<Mobile>()
                .FirstOrDefaultAsync(p => p.IMEI == query),

            ProductType.Bike => await _db.Products
                .OfType<Bike>()
                .FirstOrDefaultAsync(p => p.FrameNumber == query || p.EngineNumber == query),

            ProductType.Laptop => await _db.Products
                .OfType<Laptop>()
                .FirstOrDefaultAsync(p => p.SerialNumber == query || p.MacAddress == query),

            _ => null
        };

        if (product == null) return null;

        var openComplaints = await _db.Complaints
            .Where(c => c.ProductId == product.Id && c.Status == ComplaintStatus.Approved)
            .Select(c => new ComplaintSummary(
                c.Id,
                c.LocationStolen,
                c.CreatedAt
            ))
            .ToListAsync();

        // If no active stolen report, check whether the item was previously reported and resolved.
        // A resolved complaint means the item was recovered — hide it from search entirely.
        if (openComplaints.Count == 0)
        {
            var wasResolved = await _db.Complaints
                .AnyAsync(c => c.ProductId == product.Id && c.Status == ComplaintStatus.Resolved);
            if (wasResolved) return null;
        }

        var displayId = product switch
        {
            Mobile m => m.IMEI,
            Bike b => b.FrameNumber,
            Laptop l => l.SerialNumber,
            _ => product.TrackingId
        };

        return new SearchResult(
            product.Id,
            displayId,
            product.Type.ToString(),
            product.Brand,
            product.Model,
            openComplaints.Count > 0,
            openComplaints
        );
    }
    public async Task<(Product? Product, string? Error)> CreateAsync(
        ProductType productType, string brand, string model,
        string? imei, string? frameNumber, string? engineNumber,
        string? serialNumber, string? macAddress)
    {
        Product product = productType switch
        {
            ProductType.Mobile => new Mobile
            {
                IMEI = imei ?? ""
            },

            ProductType.Bike => new Bike
            {
                FrameNumber = frameNumber ?? "",
                EngineNumber = engineNumber ?? ""
            },

            ProductType.Laptop => new Laptop
            {
                SerialNumber = serialNumber ?? "",
                MacAddress = macAddress ?? ""
            },

            _ => throw new ArgumentOutOfRangeException(nameof(productType))
        };
        product.Type = productType;
        product.TrackingId = productType switch
        {
            ProductType.Mobile  => imei!.Trim(),
            ProductType.Bike    => frameNumber!.Trim(),
            ProductType.Laptop  => serialNumber!.Trim(),
            _ => throw new ArgumentOutOfRangeException(nameof(productType))
        };
        product.Brand = brand;
        product.Model = model;
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return (product, null);
    }

    public async Task<List<ProductResponse>> GetAllAsync()
    {
        var products = await _db.Products.ToListAsync();
        return products.Select(MapToResponse).ToList();
    }

    public async Task<ProductResponse?> GetByIdAsync(int id)
    {
        var p = await _db.Products.FindAsync(id);
        return p == null ? null : MapToResponse(p);
    }

    public async Task<(ProductResponse? Result, string? Error)> CreateMobileAsync(CreateMobileRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.IMEI))
            return (null, "IMEI is required");

        if (await TrackingIdExistsAsync(req.IMEI))
            return (null, $"A product with IMEI '{req.IMEI}' already exists");

        var p = new Mobile()
        {
            Type = ProductType.Mobile,
            Brand = req.Brand,
            Model = req.Model,
            IMEI = req.IMEI.Trim(),
            TrackingId = req.IMEI.Trim()
        };

        _db.Mobiles.Add(p);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Mobile created: IMEI={IMEI}", p.IMEI);
        return (MapToResponse(p), null);
    }

    public async Task<(ProductResponse? Result, string? Error)> CreateBikeAsync(CreateBikeRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.FrameNumber))
            return (null, "Frame number is required");

        if (await TrackingIdExistsAsync(req.FrameNumber))
            return (null, $"A product with frame number '{req.FrameNumber}' already exists");

        var p = new Bike
        {
            Type = ProductType.Bike,
            Brand = req.Brand,
            Model = req.Model,
            FrameNumber = req.FrameNumber.Trim(),
            TrackingId = req.FrameNumber.Trim()
        };

        _db.Bikes.Add(p);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Bike created: FrameNumber={FrameNumber}", p.FrameNumber);
        return (MapToResponse(p), null);
    }

    public async Task<(ProductResponse? Result, string? Error)> CreateLaptopAsync(CreateLaptopRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.SerialNumber))
            return (null, "Serial number is required");

        if (await TrackingIdExistsAsync(req.SerialNumber))
            return (null, $"A product with serial number '{req.SerialNumber}' already exists");

        var p = new Laptop
        {
            Type = ProductType.Laptop,
            Brand = req.Brand,
            Model = req.Model,
            SerialNumber = req.SerialNumber.Trim(),
            MacAddress = req.MacAddress?.Trim(),
            TrackingId = req.SerialNumber.Trim()
        };

        _db.Laptops.Add(p);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Laptop created: Serial={Serial}", p.SerialNumber);
        return (MapToResponse(p), null);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateProductRequest req)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return (false, "Product not found");

        if (!string.IsNullOrWhiteSpace(req.Brand)) product.Brand = req.Brand;
        if (!string.IsNullOrWhiteSpace(req.Model)) product.Model = req.Model;
        if (!string.IsNullOrWhiteSpace(req.EngineNumber) && product is Bike bike)
            bike.EngineNumber = req.EngineNumber;

        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        _logger.LogInformation("Product updated: Id={Id}", id);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int id)
    {
        var product = await _db.Products
            .Include(p => p.Complaints)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return (false, "Product not found");

        if (product.Complaints.Any(c => c.Status == ComplaintStatus.Pending || c.Status == ComplaintStatus.Approved))
            return (false, "Cannot delete a product with open complaints");

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Product deleted: Id={Id}", id);
        return (true, null);
    }

    public async Task<Product?> FindByIdentifierAsync(
        ProductType type, string? imei, string? frameNumber, string? serialNumber)
    {
        return type switch
        {
            ProductType.Mobile when !string.IsNullOrWhiteSpace(imei) =>
                await _db.Products.OfType<Mobile>().FirstOrDefaultAsync(p => p.IMEI == imei),

            ProductType.Bike when !string.IsNullOrWhiteSpace(frameNumber) =>
                await _db.Products.OfType<Bike>().FirstOrDefaultAsync(p => p.FrameNumber == frameNumber),

            ProductType.Laptop when !string.IsNullOrWhiteSpace(serialNumber) =>
                await _db.Products.OfType<Laptop>().FirstOrDefaultAsync(p => p.SerialNumber == serialNumber),

            _ => null
        };
    }

    // --- Helpers ---

    private async Task<bool> TrackingIdExistsAsync(string trackingId) =>
        await _db.Products.AnyAsync(p => p.TrackingId == trackingId.Trim());

    private static ProductResponse MapToResponse(Product p)
    {
        var extra = new Dictionary<string, string?>();

        switch (p)
        {
            case Mobile m:
                extra["IMEI"] = m.IMEI;
                break;
            case Bike b:
                extra["FrameNumber"] = b.FrameNumber;
                extra["EngineNumber"] = b.EngineNumber;
                break;
            case Laptop l:
                extra["SerialNumber"] = l.SerialNumber;
                extra["MacAddress"] = l.MacAddress;
                break;
        }

        return new ProductResponse(
            p.Id, p.TrackingId, p.Type.ToString(),
            p.Brand, p.Model, extra, p.CreatedAt);
    }
}