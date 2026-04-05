using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class ExportsController : Controller
{
    private readonly MealPlannerDbContext _context;

    public ExportsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var exports = await _context.Exports
            .Include(x => x.User)
            .Include(x => x.Plan)
            .Include(x => x.List)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(exports);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var exportItem = await _context.Exports
            .Include(x => x.User)
            .Include(x => x.Plan)
            .Include(x => x.List)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (exportItem == null)
        {
            return NotFound();
        }

        return View(exportItem);
    }

    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Export exportItem)
    {
        ModelState.Remove("User");
        ModelState.Remove("Plan");
        ModelState.Remove("List");

        await ValidateExportAsync(exportItem);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(exportItem.UserId, exportItem.PlanId, exportItem.ListId);
            return View(exportItem);
        }

        exportItem.Id = Guid.NewGuid();
        exportItem.CreatedAt = DateTime.UtcNow;

        _context.Exports.Add(exportItem);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var exportItem = await _context.Exports.FindAsync(id);

        if (exportItem == null)
        {
            return NotFound();
        }

        await LoadListsAsync(exportItem.UserId, exportItem.PlanId, exportItem.ListId);
        return View(exportItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Export exportItem)
    {
        ModelState.Remove("User");
        ModelState.Remove("Plan");
        ModelState.Remove("List");

        if (id != exportItem.Id)
        {
            return NotFound();
        }

        await ValidateExportAsync(exportItem);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(exportItem.UserId, exportItem.PlanId, exportItem.ListId);
            return View(exportItem);
        }

        var exportFromDb = await _context.Exports.FindAsync(id);

        if (exportFromDb == null)
        {
            return NotFound();
        }

        exportFromDb.UserId = exportItem.UserId;
        exportFromDb.Type = exportItem.Type;
        exportFromDb.PlanId = exportItem.PlanId;
        exportFromDb.ListId = exportItem.ListId;
        exportFromDb.FileUrl = exportItem.FileUrl;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var exportItem = await _context.Exports
            .Include(x => x.User)
            .Include(x => x.Plan)
            .Include(x => x.List)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (exportItem == null)
        {
            return NotFound();
        }

        return View(exportItem);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var exportItem = await _context.Exports.FindAsync(id);

        if (exportItem == null)
        {
            return NotFound();
        }

        _context.Exports.Remove(exportItem);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadListsAsync(Guid? selectedUserId = null, Guid? selectedPlanId = null, Guid? selectedListId = null)
    {
        var users = await _context.Users
            .OrderBy(x => x.Name)
            .ToListAsync();

        var plans = await _context.Plans
            .Include(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var shopLists = await _context.ShopLists
            .Include(x => x.Plan)
            .ThenInclude(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var planItems = plans.Select(x => new
        {
            x.Id,
            Text = $"{x.User.Name} | {x.Status} | {x.Days} днів"
        });

        var listItems = shopLists.Select(x => new
        {
            x.Id,
            Text = $"{x.Plan.User.Name} | список на {x.Days} днів"
        });

        ViewBag.UserId = new SelectList(users, "Id", "Name", selectedUserId);
        ViewBag.PlanId = new SelectList(planItems, "Id", "Text", selectedPlanId);
        ViewBag.ListId = new SelectList(listItems, "Id", "Text", selectedListId);
    }

    private async Task ValidateExportAsync(Export exportItem)
    {
        if (string.IsNullOrWhiteSpace(exportItem.Type))
        {
            ModelState.AddModelError("Type", "Потрібно вказати тип експорту.");
        }

        if (string.IsNullOrWhiteSpace(exportItem.FileUrl))
        {
            ModelState.AddModelError("FileUrl", "Потрібно вказати шлях до файлу.");
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == exportItem.UserId);
        if (!userExists)
        {
            ModelState.AddModelError("UserId", "Потрібно вибрати користувача.");
        }

        if (exportItem.PlanId.HasValue)
        {
            var planExists = await _context.Plans.AnyAsync(x => x.Id == exportItem.PlanId.Value);
            if (!planExists)
            {
                ModelState.AddModelError("PlanId", "Вибраний план не існує.");
            }
        }

        if (exportItem.ListId.HasValue)
        {
            var listExists = await _context.ShopLists.AnyAsync(x => x.Id == exportItem.ListId.Value);
            if (!listExists)
            {
                ModelState.AddModelError("ListId", "Вибраний список покупок не існує.");
            }
        }
    }
}