namespace MealPlanner.Domain.Entities;

public class Icon
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Emoji { get; set; } = null!;

    public List<Food> Foods { get; set; } = new();
}