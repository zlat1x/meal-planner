namespace MealPlanner.Domain.Entities;

public class FoodScanCode
{
    public Guid Id { get; set; }

    public Guid FoodId { get; set; }
    public Guid UserId { get; set; }

    public string CodeValue { get; set; } = string.Empty;
    public string CodeType { get; set; } = "Barcode";
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Food Food { get; set; } = null!;
    public User User { get; set; } = null!;

    public ICollection<FoodScanLog> ScanLogs { get; set; } = new List<FoodScanLog>();
}
