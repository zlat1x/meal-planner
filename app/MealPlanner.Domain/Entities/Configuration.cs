namespace MealPlanner.Domain.Entities;

public class Configuration
{
    public Guid UserId { get; set; }
    public string Lang { get; set; } = null!;
    public int MealsPerDay { get; set; }
    public Guid? ActiveMacroId { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Macro? ActiveMacro { get; set; }
}