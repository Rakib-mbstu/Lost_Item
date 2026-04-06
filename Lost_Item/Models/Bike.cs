using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Lost_Item.Models;

[Index(nameof(FrameNumber), IsUnique = true)]
[Index(nameof(EngineNumber), IsUnique = true)]
public class Bike : Product
{
    [MaxLength(50)] public string FrameNumber { get; set; } = null!;
    [MaxLength(50)] public string EngineNumber { get; set; } = null!;
}