using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using MealPlanner.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class PlannerController : Controller
{
    private readonly MealPlannerDbContext _context;

    public PlannerController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var model = new PlannerPageViewModel();

        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);

        if (TempData["PlannerSuccess"] is string message)
        {
            ViewBag.PlannerSuccess = message;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoadDefaults(PlannerPageViewModel model)
    {
        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);
        await ApplyUserDefaultsAsync(model);

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Calculate(PlannerPageViewModel model)
    {
        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);

        ValidatePlannerInput(model);

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        await BuildResultAsync(model);

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PlannerPageViewModel model)
    {
        await FillOptionsAsync(model);
        await SetCurrentUserAsync(model);
        NormalizeMeals(model);

        ValidatePlannerInput(model);

        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        await BuildResultAsync(model);

        var saveResult = await SavePlanAsync(model);

        model.IsSaved = true;
        model.SavedPlanId = saveResult.planId;
        model.SavedShopListId = saveResult.shopListId;

        TempData["PlannerSuccess"] = "План харчування та список покупок успішно збережено в базу даних.";

        return RedirectToAction(nameof(Index));
    }

    private async Task FillOptionsAsync(PlannerPageViewModel model)
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();

        var foods = await _context.Foods
            .AsNoTracking()
            .Include(x => x.Icon)
            .Include(x => x.Per100Unit)
            .OrderBy(x => x.Name)
            .ToListAsync();

        model.UserOptions = users.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text = x.Name
        }).ToList();

        model.FoodOptions = foods.Select(x => new SelectListItem
        {
            Value = x.Id.ToString(),
            Text = $"{(x.Icon?.Emoji ?? "🍽️")} {x.Name}"
        }).ToList();
    }

    private async Task SetCurrentUserAsync(PlannerPageViewModel model)
    {
        if (model.UserId.HasValue)
        {
            return;
        }

        var firstUserId = await _context.Users
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        model.UserId = firstUserId;
    }

    private void NormalizeMeals(PlannerPageViewModel model)
    {
        if (model.MealsPerDay < 2)
        {
            model.MealsPerDay = 2;
        }

        if (model.MealsPerDay > 4)
        {
            model.MealsPerDay = 4;
        }

        if (model.Days < 1)
        {
            model.Days = 1;
        }

        if (model.Days > 7)
        {
            model.Days = 7;
        }

        model.Meals ??= new List<PlannerMealInputViewModel>();

        while (model.Meals.Count < 4)
        {
            model.Meals.Add(new PlannerMealInputViewModel());
        }

        for (var i = 0; i < 4; i++)
        {
            model.Meals[i].MealNo = i + 1;

            if (string.IsNullOrWhiteSpace(model.Meals[i].MealName))
            {
                model.Meals[i].MealName = $"Прийом {i + 1}";
            }
        }
    }

    private List<PlannerMealInputViewModel> GetActiveMeals(PlannerPageViewModel model)
    {
        return model.Meals
            .OrderBy(x => x.MealNo)
            .Take(model.MealsPerDay)
            .ToList();
    }

    private async Task ApplyUserDefaultsAsync(PlannerPageViewModel model)
    {
        if (!model.UserId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "У базі даних не знайдено користувача для роботи конструктора.");
            return;
        }

        var configuration = await _context.Configurations
            .AsNoTracking()
            .Include(x => x.ActiveMacro)
            .FirstOrDefaultAsync(x => x.UserId == model.UserId.Value);

        if (configuration == null)
        {
            ModelState.AddModelError(string.Empty, "Для поточного користувача не знайдено конфігурацію.");
            return;
        }

        if (configuration.MealsPerDay > 0)
        {
            model.MealsPerDay = Math.Clamp(configuration.MealsPerDay, 2, 4);
        }

        if (configuration.ActiveMacro != null)
        {
            model.ProteinTarget = configuration.ActiveMacro.ProteinG;
            model.CarbTarget = configuration.ActiveMacro.CarbsG;
            model.FatTarget = configuration.ActiveMacro.FatG;
        }

        NormalizeMeals(model);
    }

    private void ValidatePlannerInput(PlannerPageViewModel model)
    {
        if (!model.UserId.HasValue)
        {
            ModelState.AddModelError(string.Empty, "У базі даних не знайдено користувача для збереження плану.");
        }

        if (model.ProteinTarget <= 0)
        {
            ModelState.AddModelError("ProteinTarget", "Потрібно вказати ціль по білках.");
        }

        if (model.CarbTarget <= 0)
        {
            ModelState.AddModelError("CarbTarget", "Потрібно вказати ціль по вуглеводах.");
        }

        if (model.FatTarget <= 0)
        {
            ModelState.AddModelError("FatTarget", "Потрібно вказати ціль по жирах.");
        }

        var activeMeals = GetActiveMeals(model);

        for (var i = 0; i < activeMeals.Count; i++)
        {
            var meal = activeMeals[i];

            if (!meal.ProteinFoodId.HasValue && !meal.CarbFoodId.HasValue && !meal.FatFoodId.HasValue)
            {
                ModelState.AddModelError($"Meals[{meal.MealNo - 1}].MealName", $"Для прийому {meal.MealNo} потрібно вибрати хоча б один продукт.");
            }
        }
    }

    private async Task BuildResultAsync(PlannerPageViewModel model)
    {
        var activeMeals = GetActiveMeals(model);

        var foodIds = activeMeals
            .SelectMany(x => new[] { x.ProteinFoodId, x.CarbFoodId, x.FatFoodId })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var foods = await _context.Foods
            .AsNoTracking()
            .Include(x => x.Icon)
            .Include(x => x.Per100Unit)
            .Where(x => foodIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        model.ResultMeals.Clear();
        model.ShoppingItems.Clear();

        model.ActualProtein = 0;
        model.ActualCarb = 0;
        model.ActualFat = 0;
        model.ActualKcal = 0;

        var proteinPerMeal = model.ProteinTarget / activeMeals.Count;
        var carbPerMeal = model.CarbTarget / activeMeals.Count;
        var fatPerMeal = model.FatTarget / activeMeals.Count;

        foreach (var mealInput in activeMeals)
        {
            var mealResult = new PlannerMealResultViewModel
            {
                MealNo = mealInput.MealNo,
                MealName = mealInput.MealName
            };

            var proteinItem = BuildItem(
                mealInput.ProteinFoodId,
                "Білковий продукт",
                proteinPerMeal,
                foods,
                x => x.ProteinPer100);

            var carbItem = BuildItem(
                mealInput.CarbFoodId,
                "Вуглеводний продукт",
                carbPerMeal,
                foods,
                x => x.CarbsPer100);

            var fatItem = BuildItem(
                mealInput.FatFoodId,
                "Жировий продукт",
                fatPerMeal,
                foods,
                x => x.FatPer100);

            AddItemIfExists(mealResult, proteinItem);
            AddItemIfExists(mealResult, carbItem);
            AddItemIfExists(mealResult, fatItem);

            mealResult.ProteinTotal = mealResult.Items.Sum(x => x.Protein);
            mealResult.CarbTotal = mealResult.Items.Sum(x => x.Carb);
            mealResult.FatTotal = mealResult.Items.Sum(x => x.Fat);
            mealResult.KcalTotal = mealResult.Items.Sum(x => x.Kcal);

            model.ActualProtein += mealResult.ProteinTotal;
            model.ActualCarb += mealResult.CarbTotal;
            model.ActualFat += mealResult.FatTotal;
            model.ActualKcal += mealResult.KcalTotal;

            model.ResultMeals.Add(mealResult);
        }

        var shoppingMap = new Dictionary<Guid, PlannerShoppingItemViewModel>();

        foreach (var meal in model.ResultMeals)
        {
            foreach (var item in meal.Items)
            {
                if (!item.FoodId.HasValue)
                {
                    continue;
                }

                if (!shoppingMap.ContainsKey(item.FoodId.Value))
                {
                    shoppingMap[item.FoodId.Value] = new PlannerShoppingItemViewModel
                    {
                        FoodId = item.FoodId.Value,
                        QuantityUnitId = item.QuantityUnitId,
                        FoodName = item.FoodName,
                        Emoji = item.Emoji,
                        UnitName = item.UnitName,
                        TotalQuantity = 0
                    };
                }

                shoppingMap[item.FoodId.Value].TotalQuantity += item.QuantityValue * model.Days;
            }
        }

        model.ShoppingItems = shoppingMap.Values
            .OrderBy(x => x.FoodName)
            .ToList();

        model.ActualProtein = decimal.Round(model.ActualProtein, 1);
        model.ActualCarb = decimal.Round(model.ActualCarb, 1);
        model.ActualFat = decimal.Round(model.ActualFat, 1);
        model.ActualKcal = decimal.Round(model.ActualKcal, 0);
        model.HasResult = true;
    }

    private PlannerMealResultItemViewModel? BuildItem(
        Guid? foodId,
        string roleName,
        decimal targetMacro,
        Dictionary<Guid, Food> foods,
        Func<Food, decimal> macroSelector)
    {
        if (!foodId.HasValue)
        {
            return null;
        }

        if (!foods.TryGetValue(foodId.Value, out var food))
        {
            return null;
        }

        var macroPer100 = macroSelector(food);

        decimal quantity = 0;
        string note = string.Empty;

        if (macroPer100 <= 0)
        {
            note = "У цьому продукті немає потрібного макронутрієнта для автоматичного розрахунку.";
        }
        else
        {
            quantity = decimal.Round(targetMacro / macroPer100 * 100m, 0, MidpointRounding.AwayFromZero);
        }

        var protein = decimal.Round(quantity * food.ProteinPer100 / 100m, 1);
        var carb = decimal.Round(quantity * food.CarbsPer100 / 100m, 1);
        var fat = decimal.Round(quantity * food.FatPer100 / 100m, 1);
        var kcal = decimal.Round(quantity * food.KcalPer100 / 100m, 0);

        return new PlannerMealResultItemViewModel
        {
            FoodId = food.Id,
            QuantityUnitId = food.Per100UnitId,
            RoleName = roleName,
            FoodName = food.Name,
            Emoji = food.Icon?.Emoji ?? "🍽️",
            UnitName = food.Per100Unit.Name,
            QuantityValue = quantity,
            Protein = protein,
            Carb = carb,
            Fat = fat,
            Kcal = kcal,
            Note = note
        };
    }

    private void AddItemIfExists(PlannerMealResultViewModel mealResult, PlannerMealResultItemViewModel? item)
    {
        if (item != null)
        {
            mealResult.Items.Add(item);
        }
    }

    private async Task<(Guid planId, Guid shopListId)> SavePlanAsync(PlannerPageViewModel model)
    {
        var configuration = await _context.Configurations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == model.UserId);

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            UserId = model.UserId!.Value,
            MacroId = configuration?.ActiveMacroId,
            Days = model.Days,
            Status = "generated",
            CreatedAt = DateTime.UtcNow
        };

        _context.Plans.Add(plan);

        foreach (var day in Enumerable.Range(1, model.Days))
        {
            foreach (var mealResult in model.ResultMeals)
            {
                var meal = new Meal
                {
                    Id = Guid.NewGuid(),
                    PlanId = plan.Id,
                    DayNo = day,
                    MealNo = mealResult.MealNo,
                    Name = mealResult.MealName
                };

                _context.Meals.Add(meal);

                foreach (var item in mealResult.Items.Where(x => x.FoodId.HasValue && x.QuantityValue > 0))
                {
                    var mealItem = new MealItem
                    {
                        Id = Guid.NewGuid(),
                        MealId = meal.Id,
                        FoodId = item.FoodId!.Value,
                        QuantityValue = item.QuantityValue,
                        QuantityUnitId = item.QuantityUnitId,
                        Per100UnitId = item.QuantityUnitId,
                        ProteinPer100 = item.QuantityValue > 0 ? decimal.Round(item.Protein / item.QuantityValue * 100m, 2) : 0,
                        CarbsPer100 = item.QuantityValue > 0 ? decimal.Round(item.Carb / item.QuantityValue * 100m, 2) : 0,
                        FatPer100 = item.QuantityValue > 0 ? decimal.Round(item.Fat / item.QuantityValue * 100m, 2) : 0,
                        KcalPer100 = item.QuantityValue > 0 ? decimal.Round(item.Kcal / item.QuantityValue * 100m, 2) : 0,
                        IsLocked = false
                    };

                    _context.MealItems.Add(mealItem);
                }
            }
        }

        var shopList = new ShopList
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Days = model.Days,
            CreatedAt = DateTime.UtcNow
        };

        _context.ShopLists.Add(shopList);

        foreach (var shoppingItem in model.ShoppingItems)
        {
            var entity = new ShopItem
            {
                Id = Guid.NewGuid(),
                ListId = shopList.Id,
                FoodId = shoppingItem.FoodId,
                TotalQuantityValue = decimal.Round(shoppingItem.TotalQuantity, 0),
                QuantityUnitId = shoppingItem.QuantityUnitId
            };

            _context.ShopItems.Add(entity);
        }

        await _context.SaveChangesAsync();

        return (plan.Id, shopList.Id);
    }
}