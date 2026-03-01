namespace MealPlanner.Domain.Entities;

public class Profile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Goal { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
} 