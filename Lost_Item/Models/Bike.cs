using Microsoft.EntityFrameworkCore;

namespace Lost_Item.Models;

[Index(nameof(FrameNumber), IsUnique = true)]
[Index(nameof(EngineNumber), IsUnique = true)]
public class Bike : Product
{
    public string FrameNumber { get; set; } = null!;
    public string EngineNumber { get; set; } = null!;
}