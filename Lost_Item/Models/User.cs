namespace Lost_Item.Models;

public class User
{
    public int Id { get; set; }
    public string GoogleId { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsAdmin { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}