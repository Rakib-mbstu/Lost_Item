namespace Lost_Item.DTOs;

public record UserResponse(int Id,
    string Email,
    string Name,
    bool IsAdmin,
    DateTime CreatedAt);