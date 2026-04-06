using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Lost_Item.Models;

[Index(nameof(IMEI), IsUnique = true)]
public class Mobile : Product
{
    [MaxLength(20)] public string IMEI { get; set; } = null!;
}