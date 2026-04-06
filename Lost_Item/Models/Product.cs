using System.ComponentModel.DataAnnotations;

namespace Lost_Item.Models;

public abstract class Product
{
    public int Id { get; set; }
    public ProductType Type { get; set; }
    [MaxLength(100)] public string Brand { get; set; } = null!;
    [MaxLength(100)] public string Model { get; set; } = null!;
    [MaxLength(100)] public string TrackingId { get; set; } = null!; // IMEI / FrameNo/EngineNo / Serial
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}