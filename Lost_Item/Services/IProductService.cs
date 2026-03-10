using Lost_Item.DTOs;
using Lost_Item.Models;

namespace Lost_Item.Services;

public interface IProductService
{
    Task<(Product? Product, string? Error)> CreateAsync(
        ProductType productType, string brand, string model,
        string? imei, string? frameNumber, string? engineNumber,
        string? serialNumber, string? macAddress);
    Task<SearchResult?> SearchAsync(string trackingId);
    Task<List<ProductResponse>> GetAllAsync();
    Task<ProductResponse?> GetByIdAsync(int id);
    Task<(ProductResponse? Result, string? Error)> CreateMobileAsync(CreateMobileRequest req);
    Task<(ProductResponse? Result, string? Error)> CreateBikeAsync(CreateBikeRequest req);
    Task<(ProductResponse? Result, string? Error)> CreateLaptopAsync(CreateLaptopRequest req);
    Task<(bool Success, string? Error)> UpdateAsync(int id, UpdateProductRequest req);
    Task<(bool Success, string? Error)> DeleteAsync(int id);
    Task<SearchResult?> SearchByIdentifierAsync(string query);
}