namespace Lost_Item.Models;

public abstract class Product
{
    public int Id { get; set; }
    public ProductType Type { get; set; }
    public string Brand { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string TrackingId { get; set; } = null!; // IMEI / FrameNo/EngineNo / Serial
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}