using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Lost_Item.Models;

[Index(nameof(SerialNumber), IsUnique = true)]
[Index(nameof(MacAddress), IsUnique = true)]
public class Laptop : Product
{
    [MaxLength(100)] public string SerialNumber { get; set; } = null!;
    [MaxLength(17)]  public string? MacAddress { get; set; }
}