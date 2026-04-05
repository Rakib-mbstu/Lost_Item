using Microsoft.EntityFrameworkCore;

namespace Lost_Item.Models;

[Index(nameof(IMEI), IsUnique = true)]
public class Mobile : Product
{
    public string IMEI { get; set; } = null!;
}