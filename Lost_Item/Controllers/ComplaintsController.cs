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
    // [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var data = await _complaints.GetAllAsync();
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
        [FromForm] string? color,           // Bike
        [FromForm] string? serialNumber,    // Laptop
        [FromForm] string? macAddress,      // Laptop
        [FromForm] string locationStolen,
        [FromForm] IFormFile policeReport)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
            return Unauthorized();

        var userId = int.Parse(userIdClaim);

        if (policeReport == null || policeReport.Length == 0)
            return BadRequest("Police report file is required");

        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(policeReport.FileName).ToLower();

        if (!allowed.Contains(ext))
            return BadRequest("Only PDF, JPG, or PNG files are accepted");
        var productResult = await _products.CreateAsync(productType, brand, model, imei, frameNumber, color, serialNumber, macAddress);

        if (productResult.Error != null)
            return BadRequest("Failed to create product");

        var result = await _complaints.CreateAsync(userId, productResult.Product.Id, locationStolen, policeReport);

        Console.WriteLine(result);

        if (result.Error!=null)
            return BadRequest("Failed to create complaint");

        return Ok("Complaint created successfully");
    }

    [HttpPatch("{id}/resolve")]
    [Authorize]
    public async Task<IActionResult> Resolve(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdClaim == null)
            return Unauthorized();

        var userId = int.Parse(userIdClaim);
        var isAdmin = User.FindFirstValue("isAdmin") == "True";

        var result = await _complaints.ResolveAsync(id, userId, isAdmin);

        if (!result.Success)
            return Forbid();

        return NoContent();
    }
}