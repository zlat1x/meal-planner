namespace MealPlanner.Domain.Entities;

public class MealItem
{
    public Guid Id { get; set; }
    public Guid MealId { get; set; }
    public Guid FoodId { get; set; }

    public decimal QuantityValue { get; set; }
    public Guid QuantityUnitId { get; set; }

    public Guid Per100UnitId { get; set; }
    public decimal ProteinPer100 { get; set; }
    public decimal CarbsPer100 { get; set; }
    public decimal FatPer100 { get; set; }
    public decimal KcalPer100 { get; set; }

    public bool IsLocked { get; set; }

    public Meal Meal { get; set; } = null!;
    public Food Food { get; set; } = null!;
    public Unit QuantityUnit { get; set; } = null!;
    public Unit Per100Unit { get; set; } = null!;
}