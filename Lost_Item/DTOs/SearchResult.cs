using Lost_Item.Models;

namespace Lost_Item.DTOs;

public record SearchResult(int ProductId,
    string TrackingId,
    ProductType Type,
    string Brand,
    string Model,
    bool IsStolen,
    List<ComplaintSummary> OpenComplaints
    );