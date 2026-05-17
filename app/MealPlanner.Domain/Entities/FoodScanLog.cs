namespace MealPlanner.Domain.Entities;

public class FoodScanLog
{
    public Guid Id { get; set; }

    public Guid? FoodScanCodeId { get; set; }
    public Guid? FoodId { get; set; }
    public Guid? UserId { get; set; }

    public string ScannedCode { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string Source { get; set; } = "Web camera";

    public DateTime ScannedAt { get; set; }

    public FoodScanCode? FoodScanCode { get; set; }
    public Food? Food { get; set; }
    public User? User { get; set; }
}
