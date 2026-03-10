namespace Lost_Item.DTOs;

public record CreateLaptopRequest(string Brand, string Model, string SerialNumber, string? MacAddress);