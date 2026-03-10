namespace Lost_Item.DTOs;

public record AuthResponse(string Token, string Name, string Email, bool IsAdmin);