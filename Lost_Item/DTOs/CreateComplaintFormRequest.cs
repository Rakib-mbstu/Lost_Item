using System.ComponentModel.DataAnnotations;
using Lost_Item.Models;

namespace Lost_Item.DTOs;

public class CreateComplaintFormRequest
{
    public ProductType ProductType { get; set; }
    [StringLength(100)] public string Brand { get; set; } = null!;
    [StringLength(100)] public string Model { get; set; } = null!;
    [StringLength(20)]  public string? Imei { get; set; }
    [StringLength(50)]  public string? FrameNumber { get; set; }
    [StringLength(50)]  public string? EngineNumber { get; set; }
    [StringLength(100)] public string? SerialNumber { get; set; }
    [StringLength(17)]  public string? MacAddress { get; set; }
    [StringLength(200)] public string LocationStolen { get; set; } = null!;
    public IFormFile PoliceReport { get; set; } = null!;
}
