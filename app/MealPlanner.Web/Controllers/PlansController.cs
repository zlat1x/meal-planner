using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class PlansController : Controller
{
    private readonly MealPlannerDbContext _context;

    public PlansController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var plans = await _context.Plans
            .Include(x => x.User)
            .Include(x => x.Macro)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(plans);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var plan = await _context.Plans
            .Include(x => x.User)
            .Include(x => x.Macro)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (plan == null)
        {
            return NotFound();
        }

        return View(plan);
    }

    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Plan plan)
    {
        ModelState.Remove("User");
        ModelState.Remove("Macro");
        ModelState.Remove("Meals");
        ModelState.Remove("ShopLists");
        ModelState.Remove("Exports");

        await ValidatePlanAsync(plan);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(plan.UserId, plan.MacroId);
            return View(plan);
        }

        plan.Id = Guid.NewGuid();
        plan.CreatedAt = DateTime.UtcNow;

        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var plan = await _context.Plans.FindAsync(id);

        if (plan == null)
        {
            return NotFound();
        }

        await LoadListsAsync(plan.UserId, plan.MacroId);
        return View(plan);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Plan plan)
    {
        ModelState.Remove("User");
        ModelState.Remove("Macro");
        ModelState.Remove("Meals");
        ModelState.Remove("ShopLists");
        ModelState.Remove("Exports");

        if (id != plan.Id)
        {
            return NotFound();
        }

        await ValidatePlanAsync(plan);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(plan.UserId, plan.MacroId);
            return View(plan);
        }

        var planFromDb = await _context.Plans.FindAsync(id);

        if (planFromDb == null)
        {
            return NotFound();
        }

        planFromDb.UserId = plan.UserId;
        planFromDb.MacroId = plan.MacroId;
        planFromDb.Days = plan.Days;
        planFromDb.Status = plan.Status;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var plan = await _context.Plans
            .Include(x => x.User)
            .Include(x => x.Macro)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (plan == null)
        {
            return NotFound();
        }

        return View(plan);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var plan = await _context.Plans.FindAsync(id);

        if (plan == null)
        {
            return NotFound();
        }

        _context.Plans.Remove(plan);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadListsAsync(Guid? selectedUserId = null, Guid? selectedMacroId = null)
    {
        var users = await _context.Users.OrderBy(x => x.Name).ToListAsync();
        var macros = await _context.Macros.OrderBy(x => x.Mode).ToListAsync();

        ViewBag.UserId = new SelectList(users, "Id", "Name", selectedUserId);
        ViewBag.MacroId = new SelectList(macros, "Id", "Mode", selectedMacroId);
    }

    private async Task ValidatePlanAsync(Plan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.Status))
        {
            ModelState.AddModelError("Status", "Потрібно вказати статус плану.");
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == plan.UserId);
        if (!userExists)
        {
            ModelState.AddModelError("UserId", "Потрібно вибрати користувача.");
        }

        if (plan.MacroId.HasValue)
        {
            var macroExists = await _context.Macros.AnyAsync(x => x.Id == plan.MacroId.Value);
            if (!macroExists)
            {
                ModelState.AddModelError("MacroId", "Вибраний макрос не існує.");
            }
        }
    }
}