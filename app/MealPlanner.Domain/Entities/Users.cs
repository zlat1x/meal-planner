namespace MealPlanner.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Profile? Profile { get; set; }
    public Configuration? Configuration { get; set; }

    public List<Macro> Macros { get; set; } = new();
    public List<MacroLog> MacroLogs { get; set; } = new();
    public List<Food> Foods { get; set; } = new();
    public List<Plan> Plans { get; set; } = new();
    public List<Export> Exports { get; set; } = new();
}