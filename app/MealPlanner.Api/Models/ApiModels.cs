using MealPlanner.Domain.Entities;

namespace MealPlanner.Api.Models;

public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class CreateUserRequest
{
    public string Email { get; set; } = "";
    public string Name { get; set; } = "";
}

public class FoodResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = "";
    public FoodCategory Category { get; set; }
    public decimal ProteinPer100 { get; set; }
    public decimal CarbsPer100 { get; set; }
    public decimal FatPer100 { get; set; }
    public decimal KcalPer100 { get; set; }
    public string UnitCode { get; set; } = "";
    public string UnitName { get; set; } = "";
    public bool IsCustom { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateFoodRequest
{
    public Guid UserId { get; set; }
    public Guid Per100UnitId { get; set; }
    public Guid? IconId { get; set; }
    public string Name { get; set; } = "";
    public FoodCategory Category { get; set; }
    public decimal ProteinPer100 { get; set; }
    public decimal CarbsPer100 { get; set; }
    public decimal FatPer100 { get; set; }
    public decimal KcalPer100 { get; set; }
}

public class UpdateFoodRequest
{
    public string? Name { get; set; }
    public FoodCategory? Category { get; set; }
    public decimal? ProteinPer100 { get; set; }
    public decimal? CarbsPer100 { get; set; }
    public decimal? FatPer100 { get; set; }
    public decimal? KcalPer100 { get; set; }
    public Guid? IconId { get; set; }
    public Guid? Per100UnitId { get; set; }
}

public class PlanSummaryResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public int Days { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public int MealsCount { get; set; }
}

public class PlanDetailsResponse : PlanSummaryResponse
{
    public List<PlanMealResponse> Meals { get; set; } = new();
}

public class PlanMealResponse
{
    public Guid Id { get; set; }
    public int DayNo { get; set; }
    public int MealNo { get; set; }
    public string Name { get; set; } = "";
    public List<PlanMealItemResponse> Items { get; set; } = new();
}

public class PlanMealItemResponse
{
    public Guid FoodId { get; set; }
    public string FoodName { get; set; } = "";
    public decimal QuantityValue { get; set; }
    public string UnitName { get; set; } = "";
}

public class PlannerCalculationRequest
{
    public Guid UserId { get; set; }
    public int Days { get; set; } = 1;
    public int MealsPerDay { get; set; } = 3;
    public decimal ProteinTarget { get; set; }
    public decimal CarbTarget { get; set; }
    public decimal FatTarget { get; set; }
    public List<Guid> ProteinFoodIds { get; set; } = new();
    public List<Guid> CarbFoodIds { get; set; } = new();
    public List<Guid> FatFoodIds { get; set; } = new();
}

public class PlannerCalculationResponse
{
    public decimal ActualProtein { get; set; }
    public decimal ActualCarb { get; set; }
    public decimal ActualFat { get; set; }
    public decimal ActualKcal { get; set; }
    public List<PlannerMealResponse> Meals { get; set; } = new();
    public List<ShoppingItemResponse> ShoppingItems { get; set; } = new();
}

public class PlannerMealResponse
{
    public int MealNo { get; set; }
    public string MealName { get; set; } = "";
    public decimal Protein { get; set; }
    public decimal Carb { get; set; }
    public decimal Fat { get; set; }
    public decimal Kcal { get; set; }
    public List<PlannerMealItemResponse> Items { get; set; } = new();
}

public class PlannerMealItemResponse
{
    public Guid FoodId { get; set; }
    public string FoodName { get; set; } = "";
    public string Role { get; set; } = "";
    public decimal QuantityValue { get; set; }
    public string UnitName { get; set; } = "";
    public decimal Protein { get; set; }
    public decimal Carb { get; set; }
    public decimal Fat { get; set; }
    public decimal Kcal { get; set; }
}

public class ShoppingItemResponse
{
    public Guid FoodId { get; set; }
    public string FoodName { get; set; } = "";
    public decimal TotalQuantityValue { get; set; }
    public string UnitName { get; set; } = "";
}
