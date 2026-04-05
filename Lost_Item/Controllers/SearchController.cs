using Lost_Item.Models;
using Lost_Item.Services;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> GetAsync([FromQuery] string trackingId, [FromQuery] ProductType type)
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