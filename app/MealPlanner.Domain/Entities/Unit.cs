namespace MealPlanner.Domain.Entities;

public class Unit
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Kind { get; set; } = null!;

    public List<Food> FoodsPer100Unit { get; set; } = new();
    public List<MealItem> MealItemsQuantityUnit { get; set; } = new();
    public List<MealItem> MealItemsPer100Unit { get; set; } = new();
    public List<ShopItem> ShopItemsQuantityUnit { get; set; } = new();
}