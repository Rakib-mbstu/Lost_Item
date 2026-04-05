using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Lost_Item.Services;
using System.Security.Claims;
using Lost_Item.Models;

namespace Lost_Item.Controllers;

[ApiController]
[Route("api/complaints")]
public class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaints;
    private readonly IProductService _products;

    public ComplaintsController(IProductService products,IComplaintService complaints)
    {
        _complaints = complaints;
        _products = products;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var data = IsAdmin()
            ? await _complaints.GetAllAsync()
            : await _complaints.GetByUserAsync(GetUserId());
        return Ok(data);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromForm] ProductType productType,      // "Mobile" | "Bike" | "Laptop"
        [FromForm] string brand,
        [FromForm] string model,
        [FromForm] string? imei,            // Mobile
        [FromForm] string? frameNumber,     // Bike
        [FromForm] string? engineNumber,           // Bike
        [FromForm] string? serialNumber,    // Laptop
        [FromForm] string? macAddress,      // Laptop
        [FromForm] string locationStolen,
        [FromForm] IFormFile policeReport)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine(productType);

        if (userIdClaim == null)
            return Unauthorized();

        var userId = int.Parse(userIdClaim);

        if (policeReport == null || policeReport.Length == 0)
            return BadRequest("Police report file is required");

        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(policeReport.FileName).ToLower();

        if (!allowed.Contains(ext))
            return BadRequest("Only PDF, JPG, or PNG files are accepted");
        var productResult = await _products.CreateAsync(productType, brand, model, imei, frameNumber,engineNumber, serialNumber, macAddress);

        if (productResult.Error != null)
            return BadRequest("Failed to create product");

        var result = await _complaints.CreateAsync(userId, productResult.Product.Id, locationStolen, policeReport);

        Console.WriteLine(result);

        if (result.Error!=null)
            return BadRequest("Failed to create complaint");

        return Ok("Complaint created successfully");
    }

    [HttpPatch("{id}/approve")]
    [Authorize]
    public async Task<IActionResult> Approve(int id)
    {
        if (!IsAdmin()) return Forbid();
        var result = await _complaints.ApproveAsync(id, GetUserId());
        if (!result.Success) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPatch("{id}/reject")]
    [Authorize]
    public async Task<IActionResult> Reject(int id)
    {
        if (!IsAdmin()) return Forbid();
        var result = await _complaints.RejectAsync(id, GetUserId());
        if (!result.Success) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPatch("{id}/resolve")]
    [Authorize]
    public async Task<IActionResult> Resolve(int id)
    {
        if (!IsAdmin()) return Forbid();
        var result = await _complaints.ResolveAsync(id, GetUserId());
        if (!result.Success) return BadRequest(result.Error);
        return NoContent();
    }

    // --- Helpers ---

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin() =>
        User.FindFirstValue("isAdmin") == "True";
}