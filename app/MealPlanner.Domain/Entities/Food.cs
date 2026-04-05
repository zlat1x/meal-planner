namespace MealPlanner.Domain.Entities;

public class Food
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? IconId { get; set; }
    public Guid Per100UnitId { get; set; }

    public string Name { get; set; } = null!;
    public FoodCategory Category { get; set; }

    public decimal ProteinPer100 { get; set; }
    public decimal CarbsPer100 { get; set; }
    public decimal FatPer100 { get; set; }
    public decimal KcalPer100 { get; set; }

    public bool IsCustom { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public Icon? Icon { get; set; }
    public Unit Per100Unit { get; set; } = null!;

    public ICollection<MealItem> MealItems { get; set; } = new List<MealItem>();
    public ICollection<ShopItem> ShopItems { get; set; } = new List<ShopItem>();
}