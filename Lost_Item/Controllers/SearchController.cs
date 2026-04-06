using System.ComponentModel.DataAnnotations;
using Lost_Item.Models;
using Lost_Item.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Lost_Item.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly IProductService _productService;
    public SearchController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> GetAsync([FromQuery, StringLength(100)] string trackingId, [FromQuery] ProductType type)
    {
        if (string.IsNullOrWhiteSpace(trackingId))
        {
            return BadRequest("Missing tracking id");
        }
        
        var result = await _productService.SearchByIdentifierAsync(trackingId, type);
        if (result == null)
            return NotFound("No product found with that identifier");
        return new JsonResult(result);
    }
}