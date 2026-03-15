using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class ConfigurationsController : Controller
{
    private readonly MealPlannerDbContext _context;

    public ConfigurationsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var configurations = await _context.Configurations
            .Include(x => x.User)
            .Include(x => x.ActiveMacro)
            .OrderBy(x => x.User.Name)
            .ToListAsync();

        return View(configurations);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var configuration = await _context.Configurations
            .Include(x => x.User)
            .Include(x => x.ActiveMacro)
            .FirstOrDefaultAsync(x => x.UserId == id);

        if (configuration == null)
        {
            return NotFound();
        }

        return View(configuration);
    }

    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Configuration configuration)
    {
        var userExists = await _context.Users.AnyAsync(x => x.Id == configuration.UserId);
        if (!userExists)
        {
            ModelState.AddModelError("UserId", "User is required.");
        }

        if (configuration.ActiveMacroId.HasValue)
        {
            var macroExists = await _context.Macros.AnyAsync(x => x.Id == configuration.ActiveMacroId.Value);
            if (!macroExists)
            {
                ModelState.AddModelError("ActiveMacroId", "Selected macro does not exist.");
            }
        }

        if (string.IsNullOrWhiteSpace(configuration.Lang))
        {
            ModelState.AddModelError("Lang", "Language is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(configuration.UserId, configuration.ActiveMacroId);
            return View(configuration);
        }

        configuration.UpdatedAt = DateTime.UtcNow;

        _context.Configurations.Add(configuration);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var configuration = await _context.Configurations.FindAsync(id);

        if (configuration == null)
        {
            return NotFound();
        }

        await LoadListsAsync(configuration.UserId, configuration.ActiveMacroId);
        return View(configuration);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Configuration configuration)
    {
        if (id != configuration.UserId)
        {
            return NotFound();
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == configuration.UserId);
        if (!userExists)
        {
            ModelState.AddModelError("UserId", "User is required.");
        }

        if (configuration.ActiveMacroId.HasValue)
        {
            var macroExists = await _context.Macros.AnyAsync(x => x.Id == configuration.ActiveMacroId.Value);
            if (!macroExists)
            {
                ModelState.AddModelError("ActiveMacroId", "Selected macro does not exist.");
            }
        }

        if (string.IsNullOrWhiteSpace(configuration.Lang))
        {
            ModelState.AddModelError("Lang", "Language is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(configuration.UserId, configuration.ActiveMacroId);
            return View(configuration);
        }

        var configurationFromDb = await _context.Configurations.FindAsync(id);

        if (configurationFromDb == null)
        {
            return NotFound();
        }

        configurationFromDb.UserId = configuration.UserId;
        configurationFromDb.Lang = configuration.Lang;
        configurationFromDb.MealsPerDay = configuration.MealsPerDay;
        configurationFromDb.ActiveMacroId = configuration.ActiveMacroId;
        configurationFromDb.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var configuration = await _context.Configurations
            .Include(x => x.User)
            .Include(x => x.ActiveMacro)
            .FirstOrDefaultAsync(x => x.UserId == id);

        if (configuration == null)
        {
            return NotFound();
        }

        return View(configuration);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var configuration = await _context.Configurations.FindAsync(id);

        if (configuration == null)
        {
            return NotFound();
        }

        _context.Configurations.Remove(configuration);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadListsAsync(Guid? selectedUserId = null, Guid? selectedMacroId = null)
    {
        var users = await _context.Users
            .OrderBy(x => x.Name)
            .ToListAsync();

        var macros = await _context.Macros
            .OrderBy(x => x.Mode)
            .ToListAsync();

        ViewBag.UserId = new SelectList(users, "Id", "Name", selectedUserId);
        ViewBag.ActiveMacroId = new SelectList(macros, "Id", "Mode", selectedMacroId);
    }
}