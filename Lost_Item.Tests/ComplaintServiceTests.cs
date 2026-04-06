using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Lost_Item.Data;
using Lost_Item.Models;
using Lost_Item.Services;
using Lost_Item.Tests.Helpers;

namespace Lost_Item.Tests;

public class ComplaintServiceTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static (ComplaintService Service, AppDbContext Db) BuildService(string dbName)
    {
        var db = DbContextFactory.Create(dbName);
        var env = new Mock<IWebHostEnvironment>();
        env.Setup(e => e.ContentRootPath).Returns(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar));
        var svc = new ComplaintService(db, env.Object, NullLogger<ComplaintService>.Instance);
        return (svc, db);
    }

    private static IFormFile CreateFakeFile(string fileName = "report.pdf", long size = 1024)
    {
        var ms = new MemoryStream(new byte[size]);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(size);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default))
            .Returns(Task.CompletedTask);
        return fileMock.Object;
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ValidData_CreatesComplaint()
    {
        var (svc, db) = BuildService(nameof(CreateAsync_ValidData_CreatesComplaint));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "CREATE-IMEI-001");
        var file = CreateFakeFile("report.pdf");

        var (result, error) = await svc.CreateAsync(user.Id, mobile.Id, "Dhaka City", file);

        Assert.Null(error);
        Assert.NotNull(result);
        Assert.Equal("Pending", result.Status);
        Assert.Equal("Dhaka City", result.LocationStolen);
    }

    [Fact]
    public async Task CreateAsync_ProductNotFound_ReturnsError()
    {
        var (svc, _) = BuildService(nameof(CreateAsync_ProductNotFound_ReturnsError));
        var file = CreateFakeFile();

        var (result, error) = await svc.CreateAsync(1, 9999, "Dhaka", file);

        Assert.Null(result);
        Assert.Contains("not found", error);
    }

    [Fact]
    public async Task CreateAsync_EmptyLocation_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(CreateAsync_EmptyLocation_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "CREATE-IMEI-002");
        var file = CreateFakeFile();

        var (result, error) = await svc.CreateAsync(user.Id, mobile.Id, "   ", file);

        Assert.Null(result);
        Assert.Equal("Location stolen is required", error);
    }

    [Fact]
    public async Task CreateAsync_FileTooLarge_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(CreateAsync_FileTooLarge_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "CREATE-IMEI-003");
        var file = CreateFakeFile("big.pdf", 11 * 1024 * 1024); // 11 MB

        var (result, error) = await svc.CreateAsync(user.Id, mobile.Id, "Dhaka", file);

        Assert.Null(result);
        Assert.Contains("10 MB", error);
    }

    [Fact]
    public async Task CreateAsync_InvalidFileExtension_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(CreateAsync_InvalidFileExtension_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "CREATE-IMEI-004");
        var file = CreateFakeFile("report.exe");

        var (result, error) = await svc.CreateAsync(user.Id, mobile.Id, "Dhaka", file);

        Assert.Null(result);
        Assert.Contains("PDF, JPG, or PNG", error);
    }

    // ── HasOpenComplaintAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task HasOpenComplaint_PendingExists_ReturnsTrue()
    {
        var (svc, db) = BuildService(nameof(HasOpenComplaint_PendingExists_ReturnsTrue));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "HAS-OPEN-001");
        DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);

        var hasOpen = await svc.HasOpenComplaintAsync(mobile.Id);

        Assert.True(hasOpen);
    }

    [Fact]
    public async Task HasOpenComplaint_ApprovedExists_ReturnsTrue()
    {
        var (svc, db) = BuildService(nameof(HasOpenComplaint_ApprovedExists_ReturnsTrue));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "HAS-OPEN-002");
        DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Approved);

        var hasOpen = await svc.HasOpenComplaintAsync(mobile.Id);

        Assert.True(hasOpen);
    }

    [Fact]
    public async Task HasOpenComplaint_OnlyResolved_ReturnsFalse()
    {
        var (svc, db) = BuildService(nameof(HasOpenComplaint_OnlyResolved_ReturnsFalse));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "HAS-OPEN-003");
        DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Resolved);

        var hasOpen = await svc.HasOpenComplaintAsync(mobile.Id);

        Assert.False(hasOpen);
    }

    [Fact]
    public async Task HasOpenComplaint_NoComplaints_ReturnsFalse()
    {
        var (svc, db) = BuildService(nameof(HasOpenComplaint_NoComplaints_ReturnsFalse));
        var mobile = DbContextFactory.SeedMobile(db, "HAS-OPEN-004");

        var hasOpen = await svc.HasOpenComplaintAsync(mobile.Id);

        Assert.False(hasOpen);
    }

    // ── ApproveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveAsync_PendingComplaint_Succeeds()
    {
        var (svc, db) = BuildService(nameof(ApproveAsync_PendingComplaint_Succeeds));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "APPROVE-001");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);

        var (success, error) = await svc.ApproveAsync(complaint.Id, adminId: 1);

        Assert.True(success);
        Assert.Null(error);
        var updated = await db.Complaints.FindAsync(complaint.Id);
        Assert.Equal(ComplaintStatus.Approved, updated!.Status);
        Assert.NotNull(updated.ReviewedAt);
    }

    [Fact]
    public async Task ApproveAsync_AlreadyApproved_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(ApproveAsync_AlreadyApproved_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "APPROVE-002");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Approved);

        var (success, error) = await svc.ApproveAsync(complaint.Id, adminId: 1);

        Assert.False(success);
        Assert.Contains("pending", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApproveAsync_NotFound_ReturnsError()
    {
        var (svc, _) = BuildService(nameof(ApproveAsync_NotFound_ReturnsError));

        var (success, error) = await svc.ApproveAsync(9999, adminId: 1);

        Assert.False(success);
        Assert.Equal("Complaint not found", error);
    }

    // ── RejectAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task RejectAsync_PendingComplaint_Succeeds()
    {
        var (svc, db) = BuildService(nameof(RejectAsync_PendingComplaint_Succeeds));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "REJECT-001");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);

        var (success, error) = await svc.RejectAsync(complaint.Id, adminId: 1);

        Assert.True(success);
        Assert.Null(error);
        var updated = await db.Complaints.FindAsync(complaint.Id);
        Assert.Equal(ComplaintStatus.Rejected, updated!.Status);
    }

    [Fact]
    public async Task RejectAsync_AlreadyRejected_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(RejectAsync_AlreadyRejected_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "REJECT-002");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Rejected);

        var (success, error) = await svc.RejectAsync(complaint.Id, adminId: 1);

        Assert.False(success);
        Assert.NotNull(error);
    }

    [Fact]
    public async Task RejectAsync_NotFound_ReturnsError()
    {
        var (svc, _) = BuildService(nameof(RejectAsync_NotFound_ReturnsError));

        var (success, error) = await svc.RejectAsync(9999, adminId: 1);

        Assert.False(success);
        Assert.Equal("Complaint not found", error);
    }

    // ── ResolveAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ResolveAsync_ApprovedComplaint_Succeeds()
    {
        var (svc, db) = BuildService(nameof(ResolveAsync_ApprovedComplaint_Succeeds));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "RESOLVE-001");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Approved);

        var (success, error) = await svc.ResolveAsync(complaint.Id, adminId: 1);

        Assert.True(success);
        Assert.Null(error);
        var updated = await db.Complaints.FindAsync(complaint.Id);
        Assert.Equal(ComplaintStatus.Resolved, updated!.Status);
        Assert.NotNull(updated.ResolvedAt);
    }

    [Fact]
    public async Task ResolveAsync_PendingComplaint_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(ResolveAsync_PendingComplaint_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "RESOLVE-002");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);

        var (success, error) = await svc.ResolveAsync(complaint.Id, adminId: 1);

        Assert.False(success);
        Assert.Contains("approved", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResolveAsync_NotFound_ReturnsError()
    {
        var (svc, _) = BuildService(nameof(ResolveAsync_NotFound_ReturnsError));

        var (success, error) = await svc.ResolveAsync(9999, adminId: 1);

        Assert.False(success);
        Assert.Equal("Complaint not found", error);
    }

    // ── AddNoteAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AddNoteAsync_SetsNote()
    {
        var (svc, db) = BuildService(nameof(AddNoteAsync_SetsNote));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "NOTE-001");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id);

        var (success, error) = await svc.AddNoteAsync(complaint.Id, "Admin note here");

        Assert.True(success);
        Assert.Null(error);
        var updated = await db.Complaints.FindAsync(complaint.Id);
        Assert.Equal("Admin note here", updated!.AdminNote);
    }

    [Fact]
    public async Task AddNoteAsync_ClearsNote_WhenNull()
    {
        var (svc, db) = BuildService(nameof(AddNoteAsync_ClearsNote_WhenNull));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "NOTE-002");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id);

        await svc.AddNoteAsync(complaint.Id, "Some note");
        var (success, error) = await svc.AddNoteAsync(complaint.Id, null);

        Assert.True(success);
        var updated = await db.Complaints.FindAsync(complaint.Id);
        Assert.Null(updated!.AdminNote);
    }

    [Fact]
    public async Task AddNoteAsync_NotFound_ReturnsError()
    {
        var (svc, _) = BuildService(nameof(AddNoteAsync_NotFound_ReturnsError));

        var (success, error) = await svc.AddNoteAsync(9999, "note");

        Assert.False(success);
        Assert.Equal("Complaint not found", error);
    }

    // ── AddUpdateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task AddUpdateAsync_PendingComplaint_Succeeds()
    {
        var (svc, db) = BuildService(nameof(AddUpdateAsync_PendingComplaint_Succeeds));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "UPDATE-001");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);

        var (success, error) = await svc.AddUpdateAsync(complaint.Id, user.Id, "My update message");

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task AddUpdateAsync_ApprovedComplaint_Succeeds()
    {
        var (svc, db) = BuildService(nameof(AddUpdateAsync_ApprovedComplaint_Succeeds));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "UPDATE-002");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Approved);

        var (success, error) = await svc.AddUpdateAsync(complaint.Id, user.Id, "Another update");

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task AddUpdateAsync_RejectedComplaint_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(AddUpdateAsync_RejectedComplaint_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "UPDATE-003");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Rejected);

        var (success, error) = await svc.AddUpdateAsync(complaint.Id, user.Id, "Should fail");

        Assert.False(success);
        Assert.Contains("Pending or Approved", error);
    }

    [Fact]
    public async Task AddUpdateAsync_ResolvedComplaint_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(AddUpdateAsync_ResolvedComplaint_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "UPDATE-004");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Resolved);

        var (success, error) = await svc.AddUpdateAsync(complaint.Id, user.Id, "Should fail");

        Assert.False(success);
        Assert.Contains("Pending or Approved", error);
    }

    [Fact]
    public async Task AddUpdateAsync_WrongUser_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(AddUpdateAsync_WrongUser_ReturnsError));
        var owner = DbContextFactory.SeedUser(db, id: 20);
        var other = DbContextFactory.SeedUser(db, id: 21);
        var mobile = DbContextFactory.SeedMobile(db, "UPDATE-005");
        var complaint = DbContextFactory.SeedComplaint(db, owner.Id, mobile.Id, ComplaintStatus.Pending);

        var (success, error) = await svc.AddUpdateAsync(complaint.Id, other.Id, "Not authorized");

        Assert.False(success);
        Assert.Contains("not authorized", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddUpdateAsync_EmptyMessage_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(AddUpdateAsync_EmptyMessage_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "UPDATE-006");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);

        var (success, error) = await svc.AddUpdateAsync(complaint.Id, user.Id, "  ");

        Assert.False(success);
        Assert.Contains("empty", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddUpdateAsync_MessageTooLong_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(AddUpdateAsync_MessageTooLong_ReturnsError));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "UPDATE-007");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);

        var (success, error) = await svc.AddUpdateAsync(complaint.Id, user.Id, new string('x', 501));

        Assert.False(success);
        Assert.Contains("500", error);
    }

    [Fact]
    public async Task AddUpdateAsync_NotFound_ReturnsError()
    {
        var (svc, _) = BuildService(nameof(AddUpdateAsync_NotFound_ReturnsError));

        var (success, error) = await svc.AddUpdateAsync(9999, 1, "message");

        Assert.False(success);
        Assert.Equal("Complaint not found", error);
    }

    // ── GetUpdatesAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetUpdatesAsync_Owner_CanViewUpdates()
    {
        var (svc, db) = BuildService(nameof(GetUpdatesAsync_Owner_CanViewUpdates));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "GETUPDATES-001");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);
        await svc.AddUpdateAsync(complaint.Id, user.Id, "Hello update");

        var (updates, error) = await svc.GetUpdatesAsync(complaint.Id, user.Id, isAdmin: false);

        Assert.Null(error);
        Assert.NotNull(updates);
        Assert.Single(updates);
        Assert.Equal("Hello update", updates[0].Message);
    }

    [Fact]
    public async Task GetUpdatesAsync_Admin_CanViewAnyComplaintsUpdates()
    {
        var (svc, db) = BuildService(nameof(GetUpdatesAsync_Admin_CanViewAnyComplaintsUpdates));
        var user = DbContextFactory.SeedUser(db, id: 30);
        var admin = DbContextFactory.SeedUser(db, id: 31, isAdmin: true);
        var mobile = DbContextFactory.SeedMobile(db, "GETUPDATES-002");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id, ComplaintStatus.Pending);
        await svc.AddUpdateAsync(complaint.Id, user.Id, "Owner update");

        var (updates, error) = await svc.GetUpdatesAsync(complaint.Id, admin.Id, isAdmin: true);

        Assert.Null(error);
        Assert.NotNull(updates);
        Assert.Single(updates);
    }

    [Fact]
    public async Task GetUpdatesAsync_NonOwnerNonAdmin_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(GetUpdatesAsync_NonOwnerNonAdmin_ReturnsError));
        var owner = DbContextFactory.SeedUser(db, id: 40);
        var other = DbContextFactory.SeedUser(db, id: 41);
        var mobile = DbContextFactory.SeedMobile(db, "GETUPDATES-003");
        var complaint = DbContextFactory.SeedComplaint(db, owner.Id, mobile.Id, ComplaintStatus.Pending);

        var (updates, error) = await svc.GetUpdatesAsync(complaint.Id, other.Id, isAdmin: false);

        Assert.Null(updates);
        Assert.Contains("not authorized", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetUpdatesAsync_NotFound_ReturnsError()
    {
        var (svc, _) = BuildService(nameof(GetUpdatesAsync_NotFound_ReturnsError));

        var (updates, error) = await svc.GetUpdatesAsync(9999, 1, isAdmin: true);

        Assert.Null(updates);
        Assert.Equal("Complaint not found", error);
    }

    // ── GetAllAsync / GetByUserAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsAllComplaints()
    {
        var (svc, db) = BuildService(nameof(GetAllAsync_ReturnsAllComplaints));
        var user = DbContextFactory.SeedUser(db, id: 50);
        var m1 = DbContextFactory.SeedMobile(db, "ALL-001");
        var m2 = DbContextFactory.SeedMobile(db, "ALL-002");
        DbContextFactory.SeedComplaint(db, user.Id, m1.Id);
        DbContextFactory.SeedComplaint(db, user.Id, m2.Id);

        var list = await svc.GetAllAsync();

        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyUserComplaints()
    {
        var (svc, db) = BuildService(nameof(GetByUserAsync_ReturnsOnlyUserComplaints));
        var user1 = DbContextFactory.SeedUser(db, id: 60);
        var user2 = DbContextFactory.SeedUser(db, id: 61);
        var m1 = DbContextFactory.SeedMobile(db, "BYUSER-001");
        var m2 = DbContextFactory.SeedMobile(db, "BYUSER-002");
        DbContextFactory.SeedComplaint(db, user1.Id, m1.Id);
        DbContextFactory.SeedComplaint(db, user2.Id, m2.Id);

        var list = await svc.GetByUserAsync(user1.Id);

        Assert.Single(list);
        Assert.Equal(m1.Id, list[0].ProductId);
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_Owner_CanDelete()
    {
        var (svc, db) = BuildService(nameof(DeleteAsync_Owner_CanDelete));
        var user = DbContextFactory.SeedUser(db);
        var mobile = DbContextFactory.SeedMobile(db, "DEL-COMPLAINT-001");
        var complaint = DbContextFactory.SeedComplaint(db, user.Id, mobile.Id);

        var (success, error) = await svc.DeleteAsync(complaint.Id, user.Id, isAdmin: false);

        Assert.True(success);
        Assert.Null(error);
        Assert.Null(await db.Complaints.FindAsync(complaint.Id));
    }

    [Fact]
    public async Task DeleteAsync_NonOwner_ReturnsError()
    {
        var (svc, db) = BuildService(nameof(DeleteAsync_NonOwner_ReturnsError));
        var owner = DbContextFactory.SeedUser(db, id: 70);
        var other = DbContextFactory.SeedUser(db, id: 71);
        var mobile = DbContextFactory.SeedMobile(db, "DEL-COMPLAINT-002");
        var complaint = DbContextFactory.SeedComplaint(db, owner.Id, mobile.Id);

        var (success, error) = await svc.DeleteAsync(complaint.Id, other.Id, isAdmin: false);

        Assert.False(success);
        Assert.Contains("not authorized", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteAsync_Admin_CanDeleteAnyComplaint()
    {
        var (svc, db) = BuildService(nameof(DeleteAsync_Admin_CanDeleteAnyComplaint));
        var owner = DbContextFactory.SeedUser(db, id: 80);
        var admin = DbContextFactory.SeedUser(db, id: 81, isAdmin: true);
        var mobile = DbContextFactory.SeedMobile(db, "DEL-COMPLAINT-003");
        var complaint = DbContextFactory.SeedComplaint(db, owner.Id, mobile.Id);

        var (success, error) = await svc.DeleteAsync(complaint.Id, admin.Id, isAdmin: true);

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsError()
    {
        var (svc, _) = BuildService(nameof(DeleteAsync_NotFound_ReturnsError));

        var (success, error) = await svc.DeleteAsync(9999, 1, isAdmin: true);

        Assert.False(success);
        Assert.Equal("Complaint not found", error);
    }
}
