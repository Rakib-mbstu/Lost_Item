using Lost_Item.DTOs;

namespace Lost_Item.Services;

public interface IComplaintService
{
    Task<(ComplaintResponse? Result, string? Error)> CreateAsync(
        int userId, int productId, string locationStolen, IFormFile policeReport);

    Task<List<ComplaintResponse>> GetAllAsync();
    Task<List<ComplaintResponse>> GetByUserAsync(int userId);
    Task<ComplaintResponse?> GetByIdAsync(int id);
    Task<(bool Success, string? Error)> ApproveAsync(int complaintId, int adminId);
    Task<(bool Success, string? Error)> RejectAsync(int complaintId, int adminId);
    Task<(bool Success, string? Error)> ResolveAsync(int complaintId, int adminId);
    Task<(bool Success, string? Error)> DeleteAsync(int complaintId, int userId, bool isAdmin);
}