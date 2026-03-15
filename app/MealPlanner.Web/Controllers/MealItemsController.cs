using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class MealItemsController : Controller
{
    private readonly MealPlannerDbContext _context;

    public MealItemsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var mealItems = await _context.MealItems
            .Include(x => x.Meal)
            .ThenInclude(x => x.Plan)
            .Include(x => x.Food)
            .Include(x => x.QuantityUnit)
            .Include(x => x.Per100Unit)
            .OrderBy(x => x.Meal.DayNo)
            .ThenBy(x => x.Meal.MealNo)
            .ToListAsync();

        return View(mealItems);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var mealItem = await _context.MealItems
            .Include(x => x.Meal)
            .ThenInclude(x => x.Plan)
            .Include(x => x.Food)
            .Include(x => x.QuantityUnit)
            .Include(x => x.Per100Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (mealItem == null)
        {
            return NotFound();
        }

        return View(mealItem);
    }

    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MealItem mealItem)
    {
        ModelState.Remove("Meal"); 
        ModelState.Remove("Food"); 
        ModelState.Remove("QuantityUnit");
        ModelState.Remove("Per100Unit");

        await ValidateMealItemAsync(mealItem);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(mealItem.MealId, mealItem.FoodId, mealItem.QuantityUnitId, mealItem.Per100UnitId);
            return View(mealItem);
        }

        mealItem.Id = Guid.NewGuid();

        _context.MealItems.Add(mealItem);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var mealItem = await _context.MealItems.FindAsync(id);

        if (mealItem == null)
        {
            return NotFound();
        }

        await LoadListsAsync(mealItem.MealId, mealItem.FoodId, mealItem.QuantityUnitId, mealItem.Per100UnitId);
        return View(mealItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MealItem mealItem)
    {
        ModelState.Remove("Meal"); 
        ModelState.Remove("Food"); 
        ModelState.Remove("QuantityUnit");
        ModelState.Remove("Per100Unit");
        
        if (id != mealItem.Id)
        {
            return NotFound();
        }

        await ValidateMealItemAsync(mealItem);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(mealItem.MealId, mealItem.FoodId, mealItem.QuantityUnitId, mealItem.Per100UnitId);
            return View(mealItem);
        }

        var mealItemFromDb = await _context.MealItems.FindAsync(id);

        if (mealItemFromDb == null)
        {
            return NotFound();
        }

        mealItemFromDb.MealId = mealItem.MealId;
        mealItemFromDb.FoodId = mealItem.FoodId;
        mealItemFromDb.QuantityValue = mealItem.QuantityValue;
        mealItemFromDb.QuantityUnitId = mealItem.QuantityUnitId;
        mealItemFromDb.Per100UnitId = mealItem.Per100UnitId;
        mealItemFromDb.ProteinPer100 = mealItem.ProteinPer100;
        mealItemFromDb.CarbsPer100 = mealItem.CarbsPer100;
        mealItemFromDb.FatPer100 = mealItem.FatPer100;
        mealItemFromDb.KcalPer100 = mealItem.KcalPer100;
        mealItemFromDb.IsLocked = mealItem.IsLocked;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var mealItem = await _context.MealItems
            .Include(x => x.Meal)
            .Include(x => x.Food)
            .Include(x => x.QuantityUnit)
            .Include(x => x.Per100Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (mealItem == null)
        {
            return NotFound();
        }

        return View(mealItem);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var mealItem = await _context.MealItems.FindAsync(id);

        if (mealItem == null)
        {
            return NotFound();
        }

        _context.MealItems.Remove(mealItem);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadListsAsync(
        Guid? selectedMealId = null,
        Guid? selectedFoodId = null,
        Guid? selectedQuantityUnitId = null,
        Guid? selectedPer100UnitId = null)
    {
        var meals = await _context.Meals
            .OrderBy(x => x.DayNo)
            .ThenBy(x => x.MealNo)
            .ToListAsync();

        var foods = await _context.Foods
            .OrderBy(x => x.Name)
            .ToListAsync();

        var units = await _context.Units
            .OrderBy(x => x.Name)
            .ToListAsync();

        var mealItems = meals.Select(x => new
        {
            x.Id,
            Text = $"Day {x.DayNo}, Meal {x.MealNo} - {x.Name}"
        });

        ViewBag.MealId = new SelectList(mealItems, "Id", "Text", selectedMealId);
        ViewBag.FoodId = new SelectList(foods, "Id", "Name", selectedFoodId);
        ViewBag.QuantityUnitId = new SelectList(units, "Id", "Name", selectedQuantityUnitId);
        ViewBag.Per100UnitId = new SelectList(units, "Id", "Name", selectedPer100UnitId);
    }

    private async Task ValidateMealItemAsync(MealItem mealItem)
    {
        var mealExists = await _context.Meals.AnyAsync(x => x.Id == mealItem.MealId);
        if (!mealExists)
        {
            ModelState.AddModelError("MealId", "Meal is required.");
        }

        var foodExists = await _context.Foods.AnyAsync(x => x.Id == mealItem.FoodId);
        if (!foodExists)
        {
            ModelState.AddModelError("FoodId", "Food is required.");
        }

        var quantityUnitExists = await _context.Units.AnyAsync(x => x.Id == mealItem.QuantityUnitId);
        if (!quantityUnitExists)
        {
            ModelState.AddModelError("QuantityUnitId", "Quantity unit is required.");
        }

        var per100UnitExists = await _context.Units.AnyAsync(x => x.Id == mealItem.Per100UnitId);
        if (!per100UnitExists)
        {
            ModelState.AddModelError("Per100UnitId", "Per 100 unit is required.");
        }
    }
}