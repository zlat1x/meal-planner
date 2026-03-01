namespace MealPlanner.Domain.Entities;

public class Macro
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int ProteinG { get; set; }
    public int CarbsG { get; set; }
    public int FatG { get; set; }
    public string Mode { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public List<Plan> Plans { get; set; } = new();
}