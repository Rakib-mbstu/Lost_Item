namespace Lost_Item.DTOs;

public record CreateMobileRequest(
    string Brand,
    string Model,
    string IMEI);