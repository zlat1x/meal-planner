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
}