using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using MealPlanner.Web.Models;
using Microsoft.AspNetCore.Mvc;
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

        TempData["PlannerSuccess"] = "План харчування та список покупок успішно збережено.";

        return RedirectToAction(nameof(Index));
    }

    private async Task FillOptionsAsync(PlannerPageViewModel model)
    {
        var foods = await _context.Foods
            .AsNoTracking()
            .Include(x => x.Icon)
            .OrderBy(x => x.Name)
            .ToListAsync();

        model.ProteinFoods = foods
            .Where(x => x.Category == FoodCategory.Protein)
            .Select(ToPickerFood)
            .ToList();

        model.CarbFoods = foods
            .Where(x => x.Category == FoodCategory.Carb)
            .Select(ToPickerFood)
            .ToList();

        model.FatFoods = foods
            .Where(x => x.Category == FoodCategory.Fat)
            .Select(ToPickerFood)
            .ToList();
    }

    private PlannerPickerFoodViewModel ToPickerFood(Food food)
    {
        return new PlannerPickerFoodViewModel
        {
            Id = food.Id,
            Name = food.Name,
            Emoji = food.Icon?.Emoji ?? "🍽️",
            ProteinPer100 = food.ProteinPer100,
            CarbPer100 = food.CarbsPer100,
            FatPer100 = food.FatPer100
        };
    }

    private async Task SetCurrentUserAsync(PlannerPageViewModel model)
    {
        if (model.UserId.HasValue)
        {
            return;
        }

        model.UserId = await _context.Users
            .AsNoTracking()
            .OrderBy(x => x.CreatedAt)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();
    }

    private void NormalizeMeals(PlannerPageViewModel model)
    {
        model.MealsPerDay = Math.Clamp(model.MealsPerDay, 2, 4);
        model.Days = Math.Clamp(model.Days, 1, 7);

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
            ModelState.AddModelError(string.Empty, "Не знайдено користувача для роботи конструктора.");
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
            ModelState.AddModelError(string.Empty, "Не знайдено користувача для збереження плану.");
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

        foreach (var meal in GetActiveMeals(model))
        {
            if (!meal.ProteinFoodId.HasValue && !meal.CarbFoodId.HasValue && !meal.FatFoodId.HasValue)
            {
                ModelState.AddModelError($"Meals[{meal.MealNo - 1}].MealName", $"Для прийому {meal.MealNo} потрібно вибрати хоча б один продукт.");
            }
        }
    }

    private async Task BuildResultAsync(PlannerPageViewModel model)
    {
        var activeMeals = GetActiveMeals(model);

        var allIds = activeMeals
            .SelectMany(x => new[] { x.ProteinFoodId, x.CarbFoodId, x.FatFoodId })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        var foods = await _context.Foods
            .AsNoTracking()
            .Include(x => x.Icon)
            .Include(x => x.Per100Unit)
            .Where(x => allIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        model.ResultMeals.Clear();
        model.ShoppingItems.Clear();

        model.ActualProtein = 0;
        model.ActualCarb = 0;
        model.ActualFat = 0;
        model.ActualKcal = 0;

        var proteinSlotData = new List<ProteinSlotData>();
        var fixedProtein = 0m;
        var fixedFatFromProtein = 0m;
        var fixedCarbFromProtein = 0m;
        var dynamicProteinSlots = 0;

        foreach (var meal in activeMeals)
        {
            var proteinFood = GetFood(foods, meal.ProteinFoodId);

            if (proteinFood == null || IsEmptyFood(proteinFood))
            {
                proteinSlotData.Add(ProteinSlotData.Empty());
                continue;
            }

            if (IsEggOrOmelette(proteinFood))
            {
                var weight = 165m;
                proteinSlotData.Add(ProteinSlotData.Fixed(weight, 3m, "шт"));
                fixedProtein += CalcMacro(weight, proteinFood.ProteinPer100);
                fixedFatFromProtein += CalcMacro(weight, proteinFood.FatPer100);
                fixedCarbFromProtein += CalcMacro(weight, proteinFood.CarbsPer100);
                continue;
            }

            if (IsCottageCheese(proteinFood))
            {
                var weight = 200m;
                proteinSlotData.Add(ProteinSlotData.Fixed(weight, 200m, "gram"));
                fixedProtein += CalcMacro(weight, proteinFood.ProteinPer100);
                fixedFatFromProtein += CalcMacro(weight, proteinFood.FatPer100);
                fixedCarbFromProtein += CalcMacro(weight, proteinFood.CarbsPer100);
                continue;
            }

            if (IsProteinPowder(proteinFood))
            {
                var weight = 30m;
                proteinSlotData.Add(ProteinSlotData.Fixed(weight, 1m, "скуп"));
                fixedProtein += CalcMacro(weight, proteinFood.ProteinPer100);
                fixedFatFromProtein += CalcMacro(weight, proteinFood.FatPer100);
                fixedCarbFromProtein += CalcMacro(weight, proteinFood.CarbsPer100);
                continue;
            }

            dynamicProteinSlots++;
            proteinSlotData.Add(ProteinSlotData.Dynamic());
        }

        var remainingProtein = model.ProteinTarget - fixedProtein;
        if (remainingProtein < 0)
        {
            remainingProtein = 0;
        }

        var proteinPerDynamicSlot = dynamicProteinSlots > 0
            ? remainingProtein / dynamicProteinSlots
            : 0;

        var carbDynamicSlots = activeMeals.Count(x =>
        {
            var food = GetFood(foods, x.CarbFoodId);
            return food != null && !IsEmptyFood(food);
        });

        var remainingCarb = model.CarbTarget - fixedCarbFromProtein;
        if (remainingCarb < 0)
        {
            remainingCarb = 0;
        }

        var carbPerSlot = carbDynamicSlots > 0
            ? remainingCarb / carbDynamicSlots
            : 0;

        var mealsTemp = new List<MealTempData>();
        var usedProtein = fixedProtein;
        var usedCarb = fixedCarbFromProtein;
        var usedFat = fixedFatFromProtein;

        for (var index = 0; index < activeMeals.Count; index++)
        {
            var mealInput = activeMeals[index];

            var proteinFood = GetFood(foods, mealInput.ProteinFoodId);
            var carbFood = GetFood(foods, mealInput.CarbFoodId);
            var fatFood = GetFood(foods, mealInput.FatFoodId);

            var proteinItem = BuildProteinItem(proteinFood, proteinSlotData[index], proteinPerDynamicSlot);
            var carbItem = BuildCarbItem(carbFood, carbPerSlot);

            if (proteinItem != null)
            {
                usedProtein += proteinItem.Protein;
                usedFat += proteinItem.Fat;
                usedCarb += proteinItem.Carb;
            }

            if (carbItem != null)
            {
                usedProtein += carbItem.Protein;
                usedFat += carbItem.Fat;
                usedCarb += carbItem.Carb;
            }

            mealsTemp.Add(new MealTempData
            {
                MealNo = mealInput.MealNo,
                MealName = mealInput.MealName,
                ProteinItem = proteinItem,
                CarbItem = carbItem,
                FatFood = fatFood
            });
        }

        var remainingFat = model.FatTarget - usedFat;
        var dynamicFatSlots = mealsTemp.Count(x => x.FatFood != null && !IsEmptyFood(x.FatFood));

        var fatOverkill = remainingFat < -5m;

        if (remainingFat < 0)
        {
            remainingFat = 0;
        }

        var fatPerSlot = dynamicFatSlots > 0
            ? remainingFat / dynamicFatSlots
            : 0;

        foreach (var temp in mealsTemp)
        {
            var mealResult = new PlannerMealResultViewModel
            {
                MealNo = temp.MealNo,
                MealName = temp.MealName
            };

            AddItemIfExists(mealResult, temp.ProteinItem);
            AddItemIfExists(mealResult, temp.CarbItem);

            var fatItem = BuildFatItem(temp.FatFood, fatPerSlot, fatOverkill);
            AddItemIfExists(mealResult, fatItem);

            mealResult.ProteinTotal = decimal.Round(mealResult.Items.Sum(x => x.Protein), 1);
            mealResult.CarbTotal = decimal.Round(mealResult.Items.Sum(x => x.Carb), 1);
            mealResult.FatTotal = decimal.Round(mealResult.Items.Sum(x => x.Fat), 1);
            mealResult.KcalTotal = decimal.Round(mealResult.Items.Sum(x => x.Kcal), 0);

            model.ActualProtein += mealResult.ProteinTotal;
            model.ActualCarb += mealResult.CarbTotal;
            model.ActualFat += mealResult.FatTotal;
            model.ActualKcal += mealResult.KcalTotal;

            model.ResultMeals.Add(mealResult);
        }

        BuildShoppingList(model);

        model.ActualProtein = decimal.Round(model.ActualProtein, 1);
        model.ActualCarb = decimal.Round(model.ActualCarb, 1);
        model.ActualFat = decimal.Round(model.ActualFat, 1);
        model.ActualKcal = decimal.Round(model.ActualKcal, 0);
        model.HasResult = true;
    }

    private void BuildShoppingList(PlannerPageViewModel model)
    {
        var shoppingMap = new Dictionary<Guid, PlannerShoppingItemViewModel>();

        foreach (var meal in model.ResultMeals)
        {
            foreach (var item in meal.Items)
            {
                if (!item.FoodId.HasValue || item.QuantityValue <= 0)
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
                        DisplayUnitName = item.DisplayUnitName,
                        TotalQuantity = 0,
                        DisplayQuantityValue = 0
                    };
                }

                shoppingMap[item.FoodId.Value].TotalQuantity += item.QuantityValue * model.Days;
                shoppingMap[item.FoodId.Value].DisplayQuantityValue += item.DisplayQuantityValue * model.Days;
            }
        }

        model.ShoppingItems = shoppingMap.Values
            .OrderBy(x => x.FoodName)
            .ToList();
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

        foreach (var shoppingItem in model.ShoppingItems.Where(x => x.TotalQuantity > 0))
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

    private Food? GetFood(Dictionary<Guid, Food> foods, Guid? foodId)
    {
        if (!foodId.HasValue)
        {
            return null;
        }

        return foods.TryGetValue(foodId.Value, out var food) ? food : null;
    }

    private bool IsEmptyFood(Food food)
    {
        var name = food.Name.Trim().ToLower();
        return name.Contains("без м’яса") || name.Contains("без масла") || name.Contains("без мяса");
    }

    private bool IsEggOrOmelette(Food food)
    {
        var name = food.Name.Trim().ToLower();
        return name.Contains("яйця") || name.Contains("омлет") || name.Contains("яйца");
    }

    private bool IsCottageCheese(Food food)
    {
        var name = food.Name.Trim().ToLower();
        return name.Contains("сир кисломолочний") || name.Contains("творог");
    }

    private bool IsProteinPowder(Food food)
    {
        var name = food.Name.Trim().ToLower();
        return name.Contains("протеїн") || name.Contains("протеин") || name.Contains("whey");
    }

    private decimal CalcMacro(decimal weight, decimal macroPer100)
    {
        return decimal.Round((weight / 100m) * macroPer100, 1);
    }

    private PlannerMealResultItemViewModel? BuildProteinItem(Food? food, ProteinSlotData slotData, decimal targetProtein)
    {
        if (food == null || slotData.Type == ProteinSlotType.Empty)
        {
            return null;
        }

        decimal weight;
        decimal displayQuantity;
        string displayUnit;

        if (slotData.Type == ProteinSlotType.Fixed)
        {
            weight = slotData.WeightGrams;
            displayQuantity = slotData.DisplayQuantity;
            displayUnit = slotData.DisplayUnit;
        }
        else
        {
            if (food.ProteinPer100 <= 0)
            {
                return new PlannerMealResultItemViewModel
                {
                    FoodId = food.Id,
                    QuantityUnitId = food.Per100UnitId,
                    RoleName = "Білковий продукт",
                    FoodName = food.Name,
                    Emoji = food.Icon?.Emoji ?? "🍽️",
                    UnitName = food.Per100Unit.Name,
                    DisplayUnitName = food.Per100Unit.Name,
                    QuantityValue = 0,
                    DisplayQuantityValue = 0,
                    Protein = 0,
                    Carb = 0,
                    Fat = 0,
                    Kcal = 0,
                    Note = "У продукті немає достатньо білка для автоматичного розрахунку."
                };
            }

            weight = decimal.Round(targetProtein / food.ProteinPer100 * 100m, 0, MidpointRounding.AwayFromZero);
            displayQuantity = weight;
            displayUnit = food.Per100Unit.Name;
        }

        return BuildItemFromWeight(food, "Білковий продукт", weight, displayQuantity, displayUnit, string.Empty);
    }

    private PlannerMealResultItemViewModel? BuildCarbItem(Food? food, decimal targetCarb)
    {
        if (food == null || IsEmptyFood(food))
        {
            return null;
        }

        if (food.CarbsPer100 <= 0)
        {
            return new PlannerMealResultItemViewModel
            {
                FoodId = food.Id,
                QuantityUnitId = food.Per100UnitId,
                RoleName = "Вуглеводний продукт",
                FoodName = food.Name,
                Emoji = food.Icon?.Emoji ?? "🍽️",
                UnitName = food.Per100Unit.Name,
                DisplayUnitName = food.Per100Unit.Name,
                QuantityValue = 0,
                DisplayQuantityValue = 0,
                Protein = 0,
                Carb = 0,
                Fat = 0,
                Kcal = 0,
                Note = "У продукті немає достатньо вуглеводів для автоматичного розрахунку."
            };
        }

        var weight = decimal.Round(targetCarb / food.CarbsPer100 * 100m, 0, MidpointRounding.AwayFromZero);

        return BuildItemFromWeight(
            food,
            "Вуглеводний продукт",
            weight,
            weight,
            food.Per100Unit.Name,
            string.Empty);
    }

    private PlannerMealResultItemViewModel? BuildFatItem(Food? food, decimal targetFat, bool fatOverkill)
    {
        if (food == null || IsEmptyFood(food))
        {
            return null;
        }

        if (fatOverkill)
        {
            return BuildItemFromWeight(
                food,
                "Жировий продукт",
                0,
                0,
                food.Per100Unit.Name,
                "Додатковий жир не потрібен: ціль уже перекрита іншими продуктами.");
        }

        if (food.FatPer100 <= 0)
        {
            return BuildItemFromWeight(
                food,
                "Жировий продукт",
                0,
                0,
                food.Per100Unit.Name,
                "У продукті немає достатньо жирів для автоматичного розрахунку.");
        }

        var weight = decimal.Round(targetFat / food.FatPer100 * 100m, 0, MidpointRounding.AwayFromZero);

        return BuildItemFromWeight(
            food,
            "Жировий продукт",
            weight,
            weight,
            food.Per100Unit.Name,
            string.Empty);
    }

    private PlannerMealResultItemViewModel BuildItemFromWeight(
        Food food,
        string roleName,
        decimal weight,
        decimal displayQuantity,
        string displayUnitName,
        string note)
    {
        var protein = CalcMacro(weight, food.ProteinPer100);
        var carb = CalcMacro(weight, food.CarbsPer100);
        var fat = CalcMacro(weight, food.FatPer100);
        var kcal = CalcMacro(weight, food.KcalPer100);

        return new PlannerMealResultItemViewModel
        {
            FoodId = food.Id,
            QuantityUnitId = food.Per100UnitId,
            RoleName = roleName,
            FoodName = food.Name,
            Emoji = food.Icon?.Emoji ?? "🍽️",
            UnitName = food.Per100Unit.Name,
            DisplayUnitName = string.IsNullOrWhiteSpace(displayUnitName)
                ? food.Per100Unit.Name
                : displayUnitName,
            QuantityValue = weight,
            DisplayQuantityValue = displayQuantity,
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

    private sealed class MealTempData
    {
        public int MealNo { get; set; }
        public string MealName { get; set; } = string.Empty;
        public PlannerMealResultItemViewModel? ProteinItem { get; set; }
        public PlannerMealResultItemViewModel? CarbItem { get; set; }
        public Food? FatFood { get; set; }
    }

    private sealed class ProteinSlotData
    {
        public ProteinSlotType Type { get; private set; }
        public decimal WeightGrams { get; private set; }
        public decimal DisplayQuantity { get; private set; }
        public string DisplayUnit { get; private set; } = "gram";

        public static ProteinSlotData Empty() => new() { Type = ProteinSlotType.Empty };

        public static ProteinSlotData Dynamic() => new() { Type = ProteinSlotType.Dynamic };

        public static ProteinSlotData Fixed(decimal weightGrams, decimal displayQuantity, string displayUnit)
            => new()
            {
                Type = ProteinSlotType.Fixed,
                WeightGrams = weightGrams,
                DisplayQuantity = displayQuantity,
                DisplayUnit = displayUnit
            };
    }

    private enum ProteinSlotType
    {
        Empty = 0,
        Dynamic = 1,
        Fixed = 2
    }
}