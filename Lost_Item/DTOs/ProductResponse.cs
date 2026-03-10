namespace Lost_Item.DTOs;

public record ProductResponse( int Id,
    string TrackingId,
    string Type,
    string Brand,
    string Model,
    Dictionary<string, string?> ExtraFields,
    DateTime CreatedAt
    );