namespace MealPlanner.Web.Models;

public class PlannerPageViewModel
{
    public Guid? UserId { get; set; }

    public int MealsPerDay { get; set; } = 2;
    public int Days { get; set; } = 3;

    public decimal ProteinTarget { get; set; }
    public decimal CarbTarget { get; set; }
    public decimal FatTarget { get; set; }

    public decimal ActualProtein { get; set; }
    public decimal ActualCarb { get; set; }
    public decimal ActualFat { get; set; }
    public decimal ActualKcal { get; set; }

    public bool HasResult { get; set; }
    public bool IsSaved { get; set; }

    public Guid? SavedPlanId { get; set; }
    public Guid? SavedShopListId { get; set; }

    public List<PlannerPickerFoodViewModel> ProteinFoods { get; set; } = new();
    public List<PlannerPickerFoodViewModel> CarbFoods { get; set; } = new();
    public List<PlannerPickerFoodViewModel> FatFoods { get; set; } = new();

    public List<PlannerMealInputViewModel> Meals { get; set; } = new();
    public List<PlannerMealResultViewModel> ResultMeals { get; set; } = new();
    public List<PlannerShoppingItemViewModel> ShoppingItems { get; set; } = new();
}

public class PlannerPickerFoodViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🍽️";

    public decimal ProteinPer100 { get; set; }
    public decimal CarbPer100 { get; set; }
    public decimal FatPer100 { get; set; }
}

public class PlannerMealInputViewModel
{
    public int MealNo { get; set; }
    public string MealName { get; set; } = string.Empty;

    public Guid? ProteinFoodId { get; set; }
    public Guid? CarbFoodId { get; set; }
    public Guid? FatFoodId { get; set; }
}

public class PlannerMealResultViewModel
{
    public int MealNo { get; set; }
    public string MealName { get; set; } = string.Empty;

    public decimal ProteinTotal { get; set; }
    public decimal CarbTotal { get; set; }
    public decimal FatTotal { get; set; }
    public decimal KcalTotal { get; set; }

    public List<PlannerMealResultItemViewModel> Items { get; set; } = new();
}

public class PlannerMealResultItemViewModel
{
    public Guid? FoodId { get; set; }
    public Guid QuantityUnitId { get; set; }

    public string RoleName { get; set; } = string.Empty;
    public string FoodName { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🍽️";

    public string UnitName { get; set; } = "gram";
    public string DisplayUnitName { get; set; } = "gram";
    public string Note { get; set; } = string.Empty;

    public decimal QuantityValue { get; set; }
    public decimal DisplayQuantityValue { get; set; }

    public decimal Protein { get; set; }
    public decimal Carb { get; set; }
    public decimal Fat { get; set; }
    public decimal Kcal { get; set; }
}

public class PlannerShoppingItemViewModel
{
    public Guid FoodId { get; set; }
    public Guid QuantityUnitId { get; set; }

    public string FoodName { get; set; } = string.Empty;
    public string Emoji { get; set; } = "🍽️";

    public string UnitName { get; set; } = "gram";
    public string DisplayUnitName { get; set; } = "gram";

    public decimal TotalQuantity { get; set; }
    public decimal DisplayQuantityValue { get; set; }
}