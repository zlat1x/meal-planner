namespace MealPlanner.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public Profile? Profile { get; set; }
    public Configuration? Configuration { get; set; }

    public ICollection<Macro> Macros { get; set; } = new List<Macro>();
    public ICollection<MacroLog> MacroLogs { get; set; } = new List<MacroLog>();
    public ICollection<Food> Foods { get; set; } = new List<Food>();
    public ICollection<Plan> Plans { get; set; } = new List<Plan>();
    public ICollection<Export> Exports { get; set; } = new List<Export>();
}