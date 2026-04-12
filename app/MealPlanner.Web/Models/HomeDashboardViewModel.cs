using MealPlanner.Domain.Entities;

namespace MealPlanner.Web.Models;

public class HomeDashboardViewModel
{
    public int UsersCount { get; set; }
    public int FoodsCount { get; set; }
    public int PlansCount { get; set; }
    public int ShopListsCount { get; set; }
    public int ExportsCount { get; set; }

    public List<Food> LatestFoods { get; set; } = new();
    public List<Plan> LatestPlans { get; set; } = new();
    public List<ShopList> LatestShopLists { get; set; } = new();
    public List<Export> LatestExports { get; set; } = new();

    public List<DashboardCategoryCountItemViewModel> FoodsByCategory { get; set; } = new();
    public List<DashboardCategoryKcalItemViewModel> AverageKcalByCategory { get; set; } = new();
    public List<DashboardDaysDistributionItemViewModel> ShopListsByDays { get; set; } = new();
    public List<DashboardPlanStatusItemViewModel> PlansByStatus { get; set; } = new();
}

public class DashboardCategoryCountItemViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DashboardCategoryKcalItemViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal AverageKcal { get; set; }
}

public class DashboardDaysDistributionItemViewModel
{
    public int Days { get; set; }
    public int Count { get; set; }
}

public class DashboardPlanStatusItemViewModel
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}