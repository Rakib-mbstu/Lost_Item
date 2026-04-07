using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Lost_Item.DTOs;
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

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMine()
    {
        var data = await _complaints.GetByUserAsync(GetUserId());
        return Ok(data);
    }

    [HttpPost]
    [Authorize]
    [EnableRateLimiting("complaints-create")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateComplaintFormRequest req)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        Console.WriteLine(req.ProductType);

        if (userIdClaim == null)
            return Unauthorized();

        var userId = int.Parse(userIdClaim);

        if (req.PoliceReport == null || req.PoliceReport.Length == 0)
            return BadRequest("Police report file is required");

        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var ext = Path.GetExtension(req.PoliceReport.FileName).ToLower();

        if (!allowed.Contains(ext))
            return BadRequest("Only PDF, JPG, or PNG files are accepted");

        // Duplicate guard: reuse existing product or block if already has an open complaint
        int productId;
        var existing = await _products.FindByIdentifierAsync(req.ProductType, req.Imei, req.FrameNumber, req.SerialNumber);
        if (existing != null)
        {
            if (await _complaints.HasOpenComplaintAsync(existing.Id))
                return Conflict("This product already has a pending or approved complaint. Search by its identifier to view details.");
            productId = existing.Id;
        }
        else
        {
            var productResult = await _products.CreateAsync(req.ProductType, req.Brand, req.Model, req.Imei, req.FrameNumber, req.EngineNumber, req.SerialNumber, req.MacAddress);
            if (productResult.Error != null)
                return BadRequest("Failed to create product");
            productId = productResult.Product!.Id;
        }

        var result = await _complaints.CreateAsync(userId, productId, req.LocationStolen, req.PoliceReport);

        Console.WriteLine(result);

        if (result.Error != null)
            return BadRequest("Failed to create complaint");

        return Ok("Complaint created successfully");
    }

    [HttpPatch("{id}/approve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Approve(int id)
    {
        var result = await _complaints.ApproveAsync(id, GetUserId());
        if (!result.Success) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPatch("{id}/reject")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Reject(int id)
    {
        var result = await _complaints.RejectAsync(id, GetUserId());
        if (!result.Success) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPatch("{id}/resolve")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Resolve(int id)
    {
        var result = await _complaints.ResolveAsync(id, GetUserId());
        if (!result.Success) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPatch("{id}/note")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AddNote(int id, [FromBody] AddNoteRequest body)
    {
        var result = await _complaints.AddNoteAsync(id, body.Note);
        if (!result.Success) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpPost("{id}/updates")]
    [Authorize]
    public async Task<IActionResult> PostUpdate(int id, [FromBody] PostUpdateRequest body)
    {
        var result = await _complaints.AddUpdateAsync(id, GetUserId(), body.Message);
        if (!result.Success) return BadRequest(result.Error);
        return NoContent();
    }

    [HttpGet("{id}/updates")]
    [Authorize]
    public async Task<IActionResult> GetUpdates(int id)
    {
        var result = await _complaints.GetUpdatesAsync(id, GetUserId(), IsAdmin());
        if (result.Error != null) return BadRequest(result.Error);
        return Ok(result.Updates);
    }

    // --- Helpers ---

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private bool IsAdmin() =>
        User.HasClaim("isAdmin", "True");
}

public record AddNoteRequest([StringLength(1000)] string? Note);
public record PostUpdateRequest([StringLength(500)] string Message);