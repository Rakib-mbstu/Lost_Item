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
    public async Task<IActionResult> GetAsync([FromQuery] string trackingId)
    {
        if (string.IsNullOrWhiteSpace(trackingId))
        {
            return BadRequest("Missing tracking id");
        }
        
        var result = await _productService.SearchByIdentifierAsync(trackingId);
        return new JsonResult(result);  
    }
}