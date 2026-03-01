namespace MealPlanner.Domain.Entities;

public class ShopItem
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public Guid FoodId { get; set; }

    public decimal TotalQuantityValue { get; set; }
    public Guid QuantityUnitId { get; set; }

    public ShopList List { get; set; } = null!;
    public Food Food { get; set; } = null!;
    public Unit QuantityUnit { get; set; } = null!;
}