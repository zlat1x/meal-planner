using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class MacroLogsController : Controller
{
    private readonly MealPlannerDbContext _context;

    public MacroLogsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var macroLogs = await _context.MacroLogs
            .Include(x => x.User)
            .Include(x => x.Macro)
            .OrderByDescending(x => x.ChangedAt)
            .ToListAsync();

        return View(macroLogs);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var macroLog = await _context.MacroLogs
            .Include(x => x.User)
            .Include(x => x.Macro)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (macroLog == null)
        {
            return NotFound();
        }

        return View(macroLog);
    }

    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MacroLog macroLog)
    {
        ModelState.Remove("User");
        ModelState.Remove("Macro");

        await ValidateMacroLogAsync(macroLog);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(macroLog.UserId, macroLog.MacroId);
            return View(macroLog);
        }

        macroLog.Id = Guid.NewGuid();
        macroLog.ChangedAt = DateTime.UtcNow;

        _context.MacroLogs.Add(macroLog);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var macroLog = await _context.MacroLogs.FindAsync(id);

        if (macroLog == null)
        {
            return NotFound();
        }

        await LoadListsAsync(macroLog.UserId, macroLog.MacroId);
        return View(macroLog);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MacroLog macroLog)
    {
        ModelState.Remove("User");
        ModelState.Remove("Macro");

        if (id != macroLog.Id)
        {
            return NotFound();
        }

        await ValidateMacroLogAsync(macroLog);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(macroLog.UserId, macroLog.MacroId);
            return View(macroLog);
        }

        var macroLogFromDb = await _context.MacroLogs.FindAsync(id);

        if (macroLogFromDb == null)
        {
            return NotFound();
        }

        macroLogFromDb.UserId = macroLog.UserId;
        macroLogFromDb.MacroId = macroLog.MacroId;
        macroLogFromDb.Event = macroLog.Event;
        macroLogFromDb.ProteinG = macroLog.ProteinG;
        macroLogFromDb.CarbsG = macroLog.CarbsG;
        macroLogFromDb.FatG = macroLog.FatG;
        macroLogFromDb.ChangedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var macroLog = await _context.MacroLogs
            .Include(x => x.User)
            .Include(x => x.Macro)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (macroLog == null)
        {
            return NotFound();
        }

        return View(macroLog);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var macroLog = await _context.MacroLogs.FindAsync(id);

        if (macroLog == null)
        {
            return NotFound();
        }

        _context.MacroLogs.Remove(macroLog);
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
        ViewBag.MacroId = new SelectList(macros, "Id", "Mode", selectedMacroId);
    }

    private async Task ValidateMacroLogAsync(MacroLog macroLog)
    {
        if (string.IsNullOrWhiteSpace(macroLog.Event))
        {
            ModelState.AddModelError("Event", "Потрібно вказати подію.");
        }

        var userExists = await _context.Users.AnyAsync(x => x.Id == macroLog.UserId);
        if (!userExists)
        {
            ModelState.AddModelError("UserId", "Потрібно вибрати користувача.");
        }

        if (macroLog.MacroId.HasValue)
        {
            var macroExists = await _context.Macros.AnyAsync(x => x.Id == macroLog.MacroId.Value);
            if (!macroExists)
            {
                ModelState.AddModelError("MacroId", "Вибраний макрос не існує.");
            }
        }
    }
}