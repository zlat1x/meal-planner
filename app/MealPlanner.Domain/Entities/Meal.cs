namespace MealPlanner.Domain.Entities;

public class Meal
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int DayNo { get; set; }
    public int MealNo { get; set; }
    public string Name { get; set; } = null!;

    public Plan Plan { get; set; } = null!;
    public List<MealItem> Items { get; set; } = new();
}