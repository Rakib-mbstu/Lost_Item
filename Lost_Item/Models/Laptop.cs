using Microsoft.EntityFrameworkCore;

namespace Lost_Item.Models;
[Index(nameof(SerialNumber), IsUnique = true)]
[Index(nameof(MacAddress), IsUnique = true)]
public class Laptop : Product
{
    public string SerialNumber { get; set; } = null!;
    public string? MacAddress { get; set; }
}