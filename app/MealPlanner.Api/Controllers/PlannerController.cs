using MealPlanner.Api.Models;
using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PlannerController : ControllerBase
{
    private readonly MealPlannerDbContext _context;

    public PlannerController(MealPlannerDbContext context)
    {
        _context = context;
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<PlannerCalculationResponse>> Calculate(PlannerCalculationRequest request)
    {
        if (request.MealsPerDay < 2 || request.MealsPerDay > 4)
        {
            return BadRequest("Meals per day must be from 2 to 4.");
        }

        if (request.Days < 1 || request.Days > 14)
        {
            return BadRequest("Days must be from 1 to 14.");
        }

        var selectedIds = request.ProteinFoodIds
            .Concat(request.CarbFoodIds)
            .Concat(request.FatFoodIds)
            .Distinct()
            .ToList();

        if (selectedIds.Count == 0)
        {
            return BadRequest("At least one food must be selected.");
        }

        var foods = await _context.Foods
            .Include(x => x.Per100Unit)
            .Where(x => selectedIds.Contains(x.Id))
            .ToListAsync();

        if (foods.Count != selectedIds.Count)
        {
            return BadRequest("One or more selected foods do not exist.");
        }

        var weights = GetMealWeights(request.MealsPerDay);
        var response = new PlannerCalculationResponse();

        for (var mealIndex = 0; mealIndex < request.MealsPerDay; mealIndex++)
        {
            var meal = new PlannerMealResponse
            {
                MealNo = mealIndex + 1,
                MealName = GetMealName(mealIndex + 1),
            };

            AddRoleItems(
                meal,
                foods,
                request.ProteinFoodIds,
                "Білковий продукт",
                request.ProteinTarget * weights[mealIndex],
                x => x.ProteinPer100);

            AddRoleItems(
                meal,
                foods,
                request.CarbFoodIds,
                "Вуглеводний продукт",
                request.CarbTarget * weights[mealIndex],
                x => x.CarbsPer100);

            AddRoleItems(
                meal,
                foods,
                request.FatFoodIds,
                "Жировий продукт",
                request.FatTarget * weights[mealIndex],
                x => x.FatPer100);

            meal.Protein = meal.Items.Sum(x => x.Protein);
            meal.Carb = meal.Items.Sum(x => x.Carb);
            meal.Fat = meal.Items.Sum(x => x.Fat);
            meal.Kcal = meal.Items.Sum(x => x.Kcal);

            response.Meals.Add(meal);
        }

        response.ActualProtein = response.Meals.Sum(x => x.Protein);
        response.ActualCarb = response.Meals.Sum(x => x.Carb);
        response.ActualFat = response.Meals.Sum(x => x.Fat);
        response.ActualKcal = response.Meals.Sum(x => x.Kcal);

        response.ShoppingItems = response.Meals
            .SelectMany(x => x.Items)
            .GroupBy(x => new { x.FoodId, x.FoodName, x.UnitName })
            .Select(x => new ShoppingItemResponse
            {
                FoodId = x.Key.FoodId,
                FoodName = x.Key.FoodName,
                UnitName = x.Key.UnitName,
                TotalQuantityValue = Math.Round(x.Sum(i => i.QuantityValue) * request.Days, 1)
            })
            .OrderBy(x => x.FoodName)
            .ToList();

        return Ok(response);
    }

    private static void AddRoleItems(
        PlannerMealResponse meal,
        List<Food> foods,
        List<Guid> roleFoodIds,
        string role,
        decimal targetMacro,
        Func<Food, decimal> mainMacroSelector)
    {
        if (roleFoodIds.Count == 0 || targetMacro <= 0)
        {
            return;
        }

        var selectedFoods = foods
            .Where(x => roleFoodIds.Contains(x.Id))
            .ToList();

        if (selectedFoods.Count == 0)
        {
            return;
        }

        var targetPerFood = targetMacro / selectedFoods.Count;

        foreach (var food in selectedFoods)
        {
            var mainMacro = mainMacroSelector(food);

            if (mainMacro <= 0)
            {
                continue;
            }

            var quantity = Math.Round(targetPerFood * 100 / mainMacro, 1);
            var factor = quantity / 100;

            meal.Items.Add(new PlannerMealItemResponse
            {
                FoodId = food.Id,
                FoodName = food.Name,
                Role = role,
                QuantityValue = quantity,
                UnitName = food.Per100Unit.Name,
                Protein = Math.Round(food.ProteinPer100 * factor, 1),
                Carb = Math.Round(food.CarbsPer100 * factor, 1),
                Fat = Math.Round(food.FatPer100 * factor, 1),
                Kcal = Math.Round(food.KcalPer100 * factor, 1)
            });
        }
    }

    private static List<decimal> GetMealWeights(int mealsPerDay)
    {
        return mealsPerDay switch
        {
            2 => new List<decimal> { 0.45m, 0.55m },
            3 => new List<decimal> { 0.30m, 0.35m, 0.35m },
            4 => new List<decimal> { 0.25m, 0.25m, 0.25m, 0.25m },
            _ => new List<decimal> { 1m }
        };
    }

    private static string GetMealName(int mealNo)
    {
        return mealNo switch
        {
            1 => "Сніданок",
            2 => "Обід",
            3 => "Вечеря",
            4 => "Перекус",
            _ => $"Прийом {mealNo}"
        };
    }
}
