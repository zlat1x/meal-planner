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

    public ICollection<Meal> Meals { get; set; } = new List<Meal>();
    public ICollection<ShopList> ShopLists { get; set; } = new List<ShopList>();
    public ICollection<Export> Exports { get; set; } = new List<Export>();
}