namespace MealPlanner.Domain.Entities;

public class ShopList
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int Days { get; set; }
    public DateTime CreatedAt { get; set; }

    public Plan Plan { get; set; } = null!;
    public ICollection<ShopItem> Items { get; set; } = new List<ShopItem>();
    public ICollection<Export> Exports { get; set; } = new List<Export>();
}