namespace Lost_Item.DTOs;

public record SearchResult(int ProductId,
    string TrackingId,
    string Type,
    string Brand,
    string Model,
    bool IsStolen,
    List<ComplaintSummary> OpenComplaints
    );