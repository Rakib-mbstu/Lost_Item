namespace Lost_Item.Services;

using Microsoft.EntityFrameworkCore;
using Lost_Item.Data;
using Lost_Item.DTOs;
using Lost_Item.Models;
public class ComplaintService : IComplaintService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ComplaintService> _logger;

    private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    public ComplaintService(AppDbContext db, IWebHostEnvironment env, ILogger<ComplaintService> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task<(ComplaintResponse? Result, string? Error)> CreateAsync(
        int userId, int productId, string locationStolen, IFormFile policeReport)
    {
        // Validate product exists
        var product = await _db.Products.FindAsync(productId);
        if (product == null)
            return (null, $"Product with ID {productId} not found");

        // Validate location
        if (string.IsNullOrWhiteSpace(locationStolen))
            return (null, "Location stolen is required");

        // Validate file
        if (policeReport == null || policeReport.Length == 0)
            return (null, "Police report file is required");

        if (policeReport.Length > MaxFileSizeBytes)
            return (null, "File size must not exceed 10 MB");

        var ext = Path.GetExtension(policeReport.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return (null, "Only PDF, JPG, or PNG files are accepted");

        // Save file
        var uploadsDir = Path.Combine(_env.ContentRootPath, "Uploads");
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        try
        {
            await using var stream = File.Create(filePath);
            await policeReport.CopyToAsync(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save police report file");
            return (null, "Failed to save uploaded file");
        }

        var complaint = new Complaint
        {
            ProductId = productId,
            UserId = userId,
            LocationStolen = locationStolen.Trim(),
            PoliceReportPath = fileName,
            Status = ComplaintStatus.Pending
        };

        _db.Complaints.Add(complaint);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Complaint created: Id={Id}, ProductId={ProductId}, UserId={UserId}",
            complaint.Id, productId, userId);

        var response = await BuildResponseAsync(complaint.Id);
        return (response, null);
    }

    public async Task<List<ComplaintResponse>> GetAllAsync()
    {
        var complaints = await _db.Complaints
            .Include(c => c.User)
            .Include(c => c.Product)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return complaints.Select(MapToResponse).ToList();
    }

    public async Task<List<ComplaintResponse>> GetByUserAsync(int userId)
    {
        var complaints = await _db.Complaints
            .Include(c => c.User)
            .Include(c => c.Product)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return complaints.Select(MapToResponse).ToList();
    }

    public async Task<ComplaintResponse?> GetByIdAsync(int id)
    {
        var complaint = await _db.Complaints
            .Include(c => c.User)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

        return complaint == null ? null : MapToResponse(complaint);
    }

    public async Task<(bool Success, string? Error)> ApproveAsync(int complaintId, int adminId)
    {
        var complaint = await _db.Complaints.FindAsync(complaintId);
        if (complaint == null)
            return (false, "Complaint not found");

        if (complaint.Status != ComplaintStatus.Pending)
            return (false, "Only pending complaints can be approved");

        complaint.Status = ComplaintStatus.Approved;
        complaint.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Complaint approved: Id={Id} by AdminId={AdminId}", complaintId, adminId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RejectAsync(int complaintId, int adminId)
    {
        var complaint = await _db.Complaints.FindAsync(complaintId);
        if (complaint == null)
            return (false, "Complaint not found");

        if (complaint.Status != ComplaintStatus.Pending)
            return (false, "Only pending complaints can be rejected");

        complaint.Status = ComplaintStatus.Rejected;
        complaint.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Complaint rejected: Id={Id} by AdminId={AdminId}", complaintId, adminId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResolveAsync(int complaintId, int adminId)
    {
        var complaint = await _db.Complaints.FindAsync(complaintId);
        if (complaint == null)
            return (false, "Complaint not found");

        if (complaint.Status != ComplaintStatus.Approved)
            return (false, "Only approved complaints can be resolved");

        complaint.Status = ComplaintStatus.Resolved;
        complaint.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Complaint resolved: Id={Id} by AdminId={AdminId}", complaintId, adminId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int complaintId, int userId, bool isAdmin)
    {
        var complaint = await _db.Complaints.FindAsync(complaintId);
        if (complaint == null)
            return (false, "Complaint not found");

        if (!isAdmin && complaint.UserId != userId)
            return (false, "You are not authorized to delete this complaint");

        // Delete the uploaded file
        var filePath = Path.Combine(_env.ContentRootPath, "Uploads", complaint.PoliceReportPath);
        if (File.Exists(filePath))
        {
            try { File.Delete(filePath); }
            catch (Exception ex) { _logger.LogWarning(ex, "Could not delete file: {Path}", filePath); }
        }

        _db.Complaints.Remove(complaint);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Complaint deleted: Id={Id} by UserId={UserId}", complaintId, userId);
        return (true, null);
    }

    public Task<bool> HasOpenComplaintAsync(int productId) =>
        _db.Complaints.AnyAsync(c =>
            c.ProductId == productId &&
            (c.Status == ComplaintStatus.Pending || c.Status == ComplaintStatus.Approved));

    public async Task<(bool Success, string? Error)> AddNoteAsync(int complaintId, string? note)
    {
        var complaint = await _db.Complaints.FindAsync(complaintId);
        if (complaint == null)
            return (false, "Complaint not found");

        complaint.AdminNote = note?.Trim();
        await _db.SaveChangesAsync();
        _logger.LogInformation("Admin note updated: Id={Id}", complaintId);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> AddUpdateAsync(int complaintId, int userId, string message)
    {
        var complaint = await _db.Complaints.FindAsync(complaintId);
        if (complaint == null)
            return (false, "Complaint not found");

        if (complaint.UserId != userId)
            return (false, "You are not authorized to post updates on this complaint");

        if (complaint.Status == ComplaintStatus.Rejected || complaint.Status == ComplaintStatus.Resolved)
            return (false, "Updates can only be posted on Pending or Approved complaints");

        if (string.IsNullOrWhiteSpace(message))
            return (false, "Message cannot be empty");

        if (message.Length > 500)
            return (false, "Message cannot exceed 500 characters");

        var update = new ComplaintUpdate
        {
            ComplaintId = complaintId,
            UserId = userId,
            Message = message.Trim(),
        };

        _db.ComplaintUpdates.Add(update);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Complaint update added: ComplaintId={ComplaintId}, UserId={UserId}", complaintId, userId);
        return (true, null);
    }

    public async Task<(List<ComplaintUpdateResponse>? Updates, string? Error)> GetUpdatesAsync(
        int complaintId, int userId, bool isAdmin)
    {
        var complaint = await _db.Complaints.FindAsync(complaintId);
        if (complaint == null)
            return (null, "Complaint not found");

        if (!isAdmin && complaint.UserId != userId)
            return (null, "You are not authorized to view updates for this complaint");

        var updates = await _db.ComplaintUpdates
            .Include(u => u.User)
            .Where(u => u.ComplaintId == complaintId)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new ComplaintUpdateResponse(u.Id, u.Message, u.User.Name, u.CreatedAt))
            .ToListAsync();

        return (updates, null);
    }

    // --- Helpers ---

    private async Task<ComplaintResponse?> BuildResponseAsync(int id)
    {
        var c = await _db.Complaints
            .Include(c => c.User)
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == id);

        return c == null ? null : MapToResponse(c);
    }

    private static string GetDisplayId(Product product) => product switch
    {
        Mobile m => m.IMEI ?? product.TrackingId,
        Bike b   => b.FrameNumber ?? product.TrackingId,
        Laptop l => l.SerialNumber ?? product.TrackingId,
        _        => product.TrackingId
    };

    private static ComplaintResponse MapToResponse(Complaint c) => new(
        c.Id,
        c.ProductId,
        GetDisplayId(c.Product),
        c.Product.Brand,
        c.Product.Model,
        c.Product.Type.ToString(),
        c.User.Name,
        c.User.Email,
        c.LocationStolen,
        $"/uploads/{c.PoliceReportPath}",
        c.Status.ToString(),
        c.CreatedAt,
        c.ReviewedAt,
        c.ResolvedAt,
        c.AdminNote
    );
}