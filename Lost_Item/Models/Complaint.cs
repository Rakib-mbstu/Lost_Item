using System.ComponentModel.DataAnnotations;

namespace Lost_Item.Models;

public class Complaint
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(200)] public string LocationStolen { get; set; } = null!;
    public string PoliceReportPath { get; set; } = null!; // file path
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    [MaxLength(1000)] public string? AdminNote { get; set; }
    public ICollection<ComplaintUpdate> Updates { get; set; } = new List<ComplaintUpdate>();
}