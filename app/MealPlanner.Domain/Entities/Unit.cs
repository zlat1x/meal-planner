namespace MealPlanner.Domain.Entities;

public class Unit
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Kind { get; set; } = null!;

    public ICollection<Food> FoodsPer100Unit { get; set; } = new List<Food>();
    public ICollection<MealItem> MealItemsQuantityUnit { get; set; } = new List<MealItem>();
    public ICollection<MealItem> MealItemsPer100Unit { get; set; } = new List<MealItem>();
    public ICollection<ShopItem> ShopItemsQuantityUnit { get; set; } = new List<ShopItem>();
}