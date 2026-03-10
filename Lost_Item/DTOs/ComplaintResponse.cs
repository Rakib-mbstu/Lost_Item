namespace Lost_Item.DTOs;

public record ComplaintResponse(
    int Id,
    int ProductId,
    string ProductTrackingId,
    string UserName,
    string LocationStolen,
    string PoliceReportUrl,
    string Status,
    DateTime CreatedAt,
    DateTime? ResolvedAt
  );