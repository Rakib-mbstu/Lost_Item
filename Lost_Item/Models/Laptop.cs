namespace Lost_Item.Models;

public class Laptop : Product
{
    public string SerialNumber { get; set; } = null!;
    public string? MacAddress { get; set; }
}