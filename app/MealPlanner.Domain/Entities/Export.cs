namespace MealPlanner.Domain.Entities;

public class Export
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Type { get; set; } = null!;
    public Guid? PlanId { get; set; }
    public Guid? ListId { get; set; }
    public string FileUrl { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Plan? Plan { get; set; }
    public ShopList? List { get; set; }
}