using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class MealsController : Controller
{
    private readonly MealPlannerDbContext _context;

    public MealsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var meals = await _context.Meals
            .Include(x => x.Plan)
            .ThenInclude(x => x.User)
            .OrderBy(x => x.DayNo)
            .ThenBy(x => x.MealNo)
            .ToListAsync();

        return View(meals);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var meal = await _context.Meals
            .Include(x => x.Plan)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (meal == null)
        {
            return NotFound();
        }

        return View(meal);
    }

    public async Task<IActionResult> Create()
    {
        await LoadPlansAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Meal meal)
    {
        if (string.IsNullOrWhiteSpace(meal.Name))
        {
            ModelState.AddModelError("Name", "Name is required.");
        }

        var planExists = await _context.Plans.AnyAsync(x => x.Id == meal.PlanId);
        if (!planExists)
        {
            ModelState.AddModelError("PlanId", "Plan is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPlansAsync(meal.PlanId);
            return View(meal);
        }

        meal.Id = Guid.NewGuid();

        _context.Meals.Add(meal);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var meal = await _context.Meals.FindAsync(id);

        if (meal == null)
        {
            return NotFound();
        }

        await LoadPlansAsync(meal.PlanId);
        return View(meal);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Meal meal)
    {
        if (id != meal.Id)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(meal.Name))
        {
            ModelState.AddModelError("Name", "Name is required.");
        }

        var planExists = await _context.Plans.AnyAsync(x => x.Id == meal.PlanId);
        if (!planExists)
        {
            ModelState.AddModelError("PlanId", "Plan is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPlansAsync(meal.PlanId);
            return View(meal);
        }

        var mealFromDb = await _context.Meals.FindAsync(id);

        if (mealFromDb == null)
        {
            return NotFound();
        }

        mealFromDb.PlanId = meal.PlanId;
        mealFromDb.DayNo = meal.DayNo;
        mealFromDb.MealNo = meal.MealNo;
        mealFromDb.Name = meal.Name;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var meal = await _context.Meals
            .Include(x => x.Plan)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (meal == null)
        {
            return NotFound();
        }

        return View(meal);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var meal = await _context.Meals.FindAsync(id);

        if (meal == null)
        {
            return NotFound();
        }

        _context.Meals.Remove(meal);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadPlansAsync(Guid? selectedPlanId = null)
    {
        var plans = await _context.Plans
            .Include(x => x.User)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        var items = plans.Select(x => new
        {
            x.Id,
            Text = $"{x.User.Name} | {x.Status} | {x.Days} days"
        });

        ViewBag.PlanId = new SelectList(items, "Id", "Text", selectedPlanId);
    }
}