namespace MealPlanner.Domain.Entities;

public class Plan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? MacroId { get; set; }
    public int Days { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Macro? Macro { get; set; }

    public List<Meal> Meals { get; set; } = new();
    public List<ShopList> ShopLists { get; set; } = new();
    public List<Export> Exports { get; set; } = new();
}