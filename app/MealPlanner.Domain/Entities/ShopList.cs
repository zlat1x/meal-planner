namespace MealPlanner.Domain.Entities;

public class ShopList
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int Days { get; set; }
    public DateTime CreatedAt { get; set; }

    public Plan Plan { get; set; } = null!;
    public List<ShopItem> Items { get; set; } = new();
    public List<Export> Exports { get; set; } = new();
}