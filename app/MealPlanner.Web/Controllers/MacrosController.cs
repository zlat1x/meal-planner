using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class MacrosController : Controller
{
    private readonly MealPlannerDbContext _context;

    public MacrosController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var macros = await _context.Macros
            .Include(x => x.User)
            .OrderBy(x => x.Mode)
            .ToListAsync();

        return View(macros);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var macro = await _context.Macros
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (macro == null)
        {
            return NotFound();
        }

        return View(macro);
    }

    public async Task<IActionResult> Create()
    {
        await LoadUsersAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Macro macro)
    {
        ModelState.Remove("User");
        ModelState.Remove("Plans");

        if (string.IsNullOrWhiteSpace(macro.Mode))
        {
            ModelState.AddModelError("Mode", "Mode is required.");
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == macro.UserId);
        if (!userExists)
        {
            ModelState.AddModelError("UserId", "User is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadUsersAsync(macro.UserId);
            return View(macro);
        }

        macro.Id = Guid.NewGuid();
        macro.CreatedAt = DateTime.UtcNow;

        _context.Macros.Add(macro);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var macro = await _context.Macros.FindAsync(id);

        if (macro == null)
        {
            return NotFound();
        }

        await LoadUsersAsync(macro.UserId);
        return View(macro);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Macro macro)
    {
        ModelState.Remove("User");
        ModelState.Remove("Plans");

        if (id != macro.Id)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(macro.Mode))
        {
            ModelState.AddModelError("Mode", "Mode is required.");
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == macro.UserId);
        if (!userExists)
        {
            ModelState.AddModelError("UserId", "User is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadUsersAsync(macro.UserId);
            return View(macro);
        }

        var macroFromDb = await _context.Macros.FindAsync(id);

        if (macroFromDb == null)
        {
            return NotFound();
        }

        macroFromDb.UserId = macro.UserId;
        macroFromDb.ProteinG = macro.ProteinG;
        macroFromDb.CarbsG = macro.CarbsG;
        macroFromDb.FatG = macro.FatG;
        macroFromDb.Mode = macro.Mode;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var macro = await _context.Macros
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (macro == null)
        {
            return NotFound();
        }

        return View(macro);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var macro = await _context.Macros.FindAsync(id);

        if (macro == null)
        {
            return NotFound();
        }

        _context.Macros.Remove(macro);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadUsersAsync(Guid? selectedUserId = null)
    {
        var users = await _context.Users
            .OrderBy(x => x.Name)
            .ToListAsync();

        ViewBag.UserId = new SelectList(users, "Id", "Name", selectedUserId);
    }
}