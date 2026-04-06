using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Lost_Item.Controllers;
using Lost_Item.DTOs;
using Lost_Item.Models;
using Lost_Item.Services;

namespace Lost_Item.Tests;

/// <summary>
/// Unit tests for <see cref="SearchController"/>.
/// </summary>
public class SearchControllerTests
{
    private static SearchController BuildController(IProductService productSvc)
    {
        var ctrl = new SearchController(productSvc);
        // Attach a default HttpContext so action results work
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return ctrl;
    }

    [Fact]
    public async Task GetAsync_ValidMobileTrackingId_ReturnsOk()
    {
        var mock = new Mock<IProductService>();
        var fakeResult = new SearchResult(1, "123456789012345", "Mobile", "Samsung", "S21",
            true, new List<ComplaintSummary>
            {
                new(1, "Dhaka", DateTime.UtcNow)
            });
        mock.Setup(s => s.SearchByIdentifierAsync("123456789012345", ProductType.Mobile))
            .ReturnsAsync(fakeResult);

        var ctrl = BuildController(mock.Object);

        var result = await ctrl.GetAsync("123456789012345", ProductType.Mobile);

        var json = Assert.IsType<JsonResult>(result);
        Assert.Equal(fakeResult, json.Value);
    }

    [Fact]
    public async Task GetAsync_NotFound_ReturnsNotFound()
    {
        var mock = new Mock<IProductService>();
        mock.Setup(s => s.SearchByIdentifierAsync(It.IsAny<string>(), It.IsAny<ProductType>()))
            .ReturnsAsync((SearchResult?)null);

        var ctrl = BuildController(mock.Object);

        var result = await ctrl.GetAsync("NONEXISTENT", ProductType.Mobile);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetAsync_EmptyTrackingId_ReturnsBadRequest()
    {
        var mock = new Mock<IProductService>();
        var ctrl = BuildController(mock.Object);

        var result = await ctrl.GetAsync("   ", ProductType.Mobile);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetAsync_LaptopType_CallsServiceWithCorrectType()
    {
        var mock = new Mock<IProductService>();
        mock.Setup(s => s.SearchByIdentifierAsync("SN-001", ProductType.Laptop))
            .ReturnsAsync(new SearchResult(2, "SN-001", "Laptop", "Dell", "XPS", true, new()));

        var ctrl = BuildController(mock.Object);

        var result = await ctrl.GetAsync("SN-001", ProductType.Laptop);

        mock.Verify(s => s.SearchByIdentifierAsync("SN-001", ProductType.Laptop), Times.Once);
        Assert.IsType<JsonResult>(result);
    }
}

/// <summary>
/// Unit tests for <see cref="AuthController"/>.
/// </summary>
public class AuthControllerTests
{
    private static AuthController BuildController(IAuthService authSvc, string? jwtSecret = "test-secret-that-is-long-enough")
    {
        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        configMock.Setup(c => c["Jwt:Secret"]).Returns(jwtSecret);

        var ctrl = new AuthController(authSvc, configMock.Object);
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return ctrl;
    }

    private static AuthController BuildControllerWithUser(IAuthService authSvc, int userId, bool isAdmin = false)
    {
        var ctrl = BuildController(authSvc);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("isAdmin", isAdmin.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        ctrl.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);
        return ctrl;
    }

    [Fact]
    public async Task GoogleLogin_ValidToken_ReturnsOk()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.AuthenticateGoogleAsync("valid-token"))
            .ReturnsAsync(new AuthResponse("jwt-token", "John", "john@test.com", false));

        var ctrl = BuildController(mock.Object);

        var result = await ctrl.GoogleLogin(new GoogleAuthRequest("valid-token"));

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(ok.Value);
        Assert.Equal("John", response.Name);
    }

    [Fact]
    public async Task GoogleLogin_InvalidToken_ReturnsUnauthorized()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.AuthenticateGoogleAsync("bad-token"))
            .ReturnsAsync((AuthResponse?)null);

        var ctrl = BuildController(mock.Object);

        var result = await ctrl.GoogleLogin(new GoogleAuthRequest("bad-token"));

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Me_ValidUser_ReturnsOk()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.GetMeAsync(5))
            .ReturnsAsync(new UserResponse(5, "john@test.com", "John", false, DateTime.UtcNow));

        var ctrl = BuildControllerWithUser(mock.Object, userId: 5);

        var result = await ctrl.Me();

        var ok = Assert.IsType<OkObjectResult>(result);
        var user = Assert.IsType<UserResponse>(ok.Value);
        Assert.Equal(5, user.Id);
    }

    [Fact]
    public async Task Me_UserNotFound_ReturnsNotFound()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.GetMeAsync(It.IsAny<int>()))
            .ReturnsAsync((UserResponse?)null);

        var ctrl = BuildControllerWithUser(mock.Object, userId: 99);

        var result = await ctrl.Me();

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAllUsers_Admin_ReturnsOk()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.GetAllUsersAsync())
            .ReturnsAsync(new List<UserResponse>
            {
                new(1, "a@a.com", "Alice", true, DateTime.UtcNow),
                new(2, "b@b.com", "Bob", false, DateTime.UtcNow)
            });

        var ctrl = BuildControllerWithUser(mock.Object, userId: 1, isAdmin: true);

        var result = await ctrl.GetAllUsers();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<UserResponse>>(ok.Value);
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task SetAdmin_UserNotFound_ReturnsNotFound()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.SetAdminAsync(99, true))
            .ReturnsAsync((false, "User not found"));

        var ctrl = BuildControllerWithUser(mock.Object, userId: 1, isAdmin: true);

        var result = await ctrl.SetAdmin(99, new SetAdminRequest(true));

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task SetAdmin_Success_ReturnsOk()
    {
        var mock = new Mock<IAuthService>();
        mock.Setup(s => s.SetAdminAsync(2, true))
            .ReturnsAsync((true, (string?)null));

        var ctrl = BuildControllerWithUser(mock.Object, userId: 1, isAdmin: true);

        var result = await ctrl.SetAdmin(2, new SetAdminRequest(true));

        Assert.IsType<OkObjectResult>(result);
    }
}

/// <summary>
/// Unit tests for <see cref="ComplaintsController"/>.
/// </summary>
public class ComplaintsControllerTests
{
    private static ComplaintsController BuildController(
        IComplaintService complaintSvc, IProductService productSvc,
        int userId = 1, bool isAdmin = false)
    {
        var ctrl = new ComplaintsController(productSvc, complaintSvc);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("isAdmin", isAdmin.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            }
        };
        return ctrl;
    }

    private static ComplaintResponse FakeComplaintResponse(int id = 1) =>
        new(id, 1, "IMEI-001", "Samsung", "S21", "Mobile",
            "Alice", "alice@test.com", "Dhaka", "/uploads/report.pdf",
            "Pending", DateTime.UtcNow, null, null, null);

    [Fact]
    public async Task GetAll_RegularUser_ReturnsOwnComplaints()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.GetByUserAsync(1))
            .ReturnsAsync(new List<ComplaintResponse> { FakeComplaintResponse() });

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 1, isAdmin: false);

        var result = await ctrl.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<ComplaintResponse>>(ok.Value);
        Assert.Single(list);
        // Verify it used the user-scoped method
        complaintSvc.Verify(s => s.GetByUserAsync(1), Times.Once);
        complaintSvc.Verify(s => s.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetAll_Admin_ReturnsAllComplaints()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.GetAllAsync())
            .ReturnsAsync(new List<ComplaintResponse>
            {
                FakeComplaintResponse(1),
                FakeComplaintResponse(2)
            });

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 1, isAdmin: true);

        var result = await ctrl.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<ComplaintResponse>>(ok.Value);
        Assert.Equal(2, list.Count);
        complaintSvc.Verify(s => s.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetMine_ReturnsCurrentUserComplaints()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.GetByUserAsync(5))
            .ReturnsAsync(new List<ComplaintResponse> { FakeComplaintResponse() });

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 5);

        var result = await ctrl.GetMine();

        var ok = Assert.IsType<OkObjectResult>(result);
        complaintSvc.Verify(s => s.GetByUserAsync(5), Times.Once);
    }

    [Fact]
    public async Task Approve_Success_ReturnsNoContent()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.ApproveAsync(1, It.IsAny<int>()))
            .ReturnsAsync((true, (string?)null));

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 1, isAdmin: true);

        var result = await ctrl.Approve(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Approve_Failure_ReturnsBadRequest()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.ApproveAsync(99, It.IsAny<int>()))
            .ReturnsAsync((false, "Only pending complaints can be approved"));

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 1, isAdmin: true);

        var result = await ctrl.Approve(99);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Reject_Success_ReturnsNoContent()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.RejectAsync(2, It.IsAny<int>()))
            .ReturnsAsync((true, (string?)null));

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 1, isAdmin: true);

        var result = await ctrl.Reject(2);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Resolve_Success_ReturnsNoContent()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.ResolveAsync(3, It.IsAny<int>()))
            .ReturnsAsync((true, (string?)null));

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 1, isAdmin: true);

        var result = await ctrl.Resolve(3);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task AddNote_Success_ReturnsNoContent()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.AddNoteAsync(1, "Admin note"))
            .ReturnsAsync((true, (string?)null));

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 1, isAdmin: true);

        var result = await ctrl.AddNote(1, new AddNoteRequest("Admin note"));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task AddNote_Failure_ReturnsBadRequest()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.AddNoteAsync(99, It.IsAny<string?>()))
            .ReturnsAsync((false, "Complaint not found"));

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 1, isAdmin: true);

        var result = await ctrl.AddNote(99, new AddNoteRequest("note"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task PostUpdate_Success_ReturnsNoContent()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.AddUpdateAsync(1, 5, "My message"))
            .ReturnsAsync((true, (string?)null));

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 5);

        var result = await ctrl.PostUpdate(1, new PostUpdateRequest("My message"));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task PostUpdate_Failure_ReturnsBadRequest()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.AddUpdateAsync(1, 5, It.IsAny<string>()))
            .ReturnsAsync((false, "You are not authorized"));

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 5);

        var result = await ctrl.PostUpdate(1, new PostUpdateRequest("x"));

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetUpdates_Success_ReturnsOk()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        var updates = new List<ComplaintUpdateResponse>
        {
            new(1, "Test message", "Alice", DateTime.UtcNow)
        };
        complaintSvc.Setup(s => s.GetUpdatesAsync(1, 5, false))
            .ReturnsAsync((updates, (string?)null));

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 5, isAdmin: false);

        var result = await ctrl.GetUpdates(1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsType<List<ComplaintUpdateResponse>>(ok.Value);
        Assert.Single(list);
    }

    [Fact]
    public async Task GetUpdates_Failure_ReturnsBadRequest()
    {
        var complaintSvc = new Mock<IComplaintService>();
        var productSvc = new Mock<IProductService>();
        complaintSvc.Setup(s => s.GetUpdatesAsync(1, 5, false))
            .ReturnsAsync(((List<ComplaintUpdateResponse>?)null, "You are not authorized"));

        var ctrl = BuildController(complaintSvc.Object, productSvc.Object, userId: 5, isAdmin: false);

        var result = await ctrl.GetUpdates(1);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
