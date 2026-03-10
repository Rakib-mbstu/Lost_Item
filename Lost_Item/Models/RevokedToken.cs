namespace Lost_Item.Models;

public class RevokedToken
{
        public int Id { get; set; }
        public string Jti { get; set; } = null!;      // JWT unique ID claim
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime RevokedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; }        // mirrors JWT exp — for cleanup
}