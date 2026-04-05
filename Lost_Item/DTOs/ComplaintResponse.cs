namespace Lost_Item.DTOs;

public record ComplaintResponse(
    int Id,
    int ProductId,
    string ProductTrackingId,
    string ProductBrand,
    string ProductModel,
    string ProductType,
    string UserName,
    string UserEmail,
    string LocationStolen,
    string PoliceReportUrl,
    string Status,
    DateTime CreatedAt,
    DateTime? ReviewedAt,
    DateTime? ResolvedAt
  );