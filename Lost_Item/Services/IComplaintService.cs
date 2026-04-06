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

    /// <summary>Returns true if the product already has a Pending or Approved complaint.</summary>
    Task<bool> HasOpenComplaintAsync(int productId);

    /// <summary>Sets or clears the admin note on a complaint. Note is state-independent.</summary>
    Task<(bool Success, string? Error)> AddNoteAsync(int complaintId, string? note);

    Task<(bool Success, string? Error)> AddUpdateAsync(int complaintId, int userId, string message);
    Task<(List<ComplaintUpdateResponse>? Updates, string? Error)> GetUpdatesAsync(int complaintId, int userId, bool isAdmin);
}