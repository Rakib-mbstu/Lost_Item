using System.ComponentModel.DataAnnotations;

namespace Lost_Item.Models;

public class ComplaintUpdate
{
    public int Id { get; set; }
    public int ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    [MaxLength(500)] public string Message { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
