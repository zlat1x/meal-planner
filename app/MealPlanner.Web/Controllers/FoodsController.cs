using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class FoodsController : Controller
{
    private readonly MealPlannerDbContext _context;

    public FoodsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var foods = await _context.Foods
            .Include(x => x.User)
            .Include(x => x.Icon)
            .Include(x => x.Per100Unit)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return View(foods);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var food = await _context.Foods
            .Include(x => x.User)
            .Include(x => x.Icon)
            .Include(x => x.Per100Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (food == null)
        {
            return NotFound();
        }

        return View(food);
    }

    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Food food)
    {
        ModelState.Remove("User"); 
        ModelState.Remove("Icon"); 
        ModelState.Remove("Per100Unit"); 
        ModelState.Remove("MealItems");
        ModelState.Remove("ShopItems"); 

        await ValidateFoodAsync(food);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(food.UserId, food.IconId, food.Per100UnitId);
            return View(food);
        }

        food.Id = Guid.NewGuid();
        food.UpdatedAt = DateTime.UtcNow;

        _context.Foods.Add(food);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var food = await _context.Foods.FindAsync(id);

        if (food == null)
        {
            return NotFound();
        }

        await LoadListsAsync(food.UserId, food.IconId, food.Per100UnitId);
        return View(food);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Food food)
    {
        ModelState.Remove("User"); 
        ModelState.Remove("Icon"); 
        ModelState.Remove("Per100Unit"); 
        ModelState.Remove("MealItems");
        ModelState.Remove("ShopItems"); 
        
        if (id != food.Id)
        {
            return NotFound();
        }

        await ValidateFoodAsync(food);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(food.UserId, food.IconId, food.Per100UnitId);
            return View(food);
        }

        var foodFromDb = await _context.Foods.FindAsync(id);

        if (foodFromDb == null)
        {
            return NotFound();
        }

        foodFromDb.UserId = food.UserId;
        foodFromDb.IconId = food.IconId;
        foodFromDb.Per100UnitId = food.Per100UnitId;
        foodFromDb.Name = food.Name;
        foodFromDb.ProteinPer100 = food.ProteinPer100;
        foodFromDb.CarbsPer100 = food.CarbsPer100;
        foodFromDb.FatPer100 = food.FatPer100;
        foodFromDb.KcalPer100 = food.KcalPer100;
        foodFromDb.IsCustom = food.IsCustom;
        foodFromDb.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var food = await _context.Foods
            .Include(x => x.User)
            .Include(x => x.Icon)
            .Include(x => x.Per100Unit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (food == null)
        {
            return NotFound();
        }

        return View(food);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var food = await _context.Foods.FindAsync(id);

        if (food == null)
        {
            return NotFound();
        }

        _context.Foods.Remove(food);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadListsAsync(Guid? selectedUserId = null, Guid? selectedIconId = null, Guid? selectedUnitId = null)
    {
        var users = await _context.Users.OrderBy(x => x.Name).ToListAsync();
        var icons = await _context.Icons.OrderBy(x => x.Code).ToListAsync();
        var units = await _context.Units.OrderBy(x => x.Name).ToListAsync();

        ViewBag.UserId = new SelectList(users, "Id", "Name", selectedUserId);
        ViewBag.IconId = new SelectList(icons, "Id", "Code", selectedIconId);
        ViewBag.Per100UnitId = new SelectList(units, "Id", "Name", selectedUnitId);
    }

    private async Task ValidateFoodAsync(Food food)
    {
        if (string.IsNullOrWhiteSpace(food.Name))
        {
            ModelState.AddModelError("Name", "Name is required.");
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == food.UserId);
        if (!userExists)
        {
            ModelState.AddModelError("UserId", "User is required.");
        }

        var unitExists = await _context.Units.AnyAsync(x => x.Id == food.Per100UnitId);
        if (!unitExists)
        {
            ModelState.AddModelError("Per100UnitId", "Per 100 unit is required.");
        }

        if (food.IconId.HasValue)
        {
            var iconExists = await _context.Icons.AnyAsync(x => x.Id == food.IconId.Value);
            if (!iconExists)
            {
                ModelState.AddModelError("IconId", "Selected icon does not exist.");
            }
        }
    }
}