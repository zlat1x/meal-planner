using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class UnitsController : Controller
{
    private readonly MealPlannerDbContext _context;

    public UnitsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var units = await _context.Units
            .OrderBy(x => x.Code)
            .ToListAsync();

        return View(units);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var unit = await _context.Units
            .FirstOrDefaultAsync(x => x.Id == id);

        if (unit == null)
        {
            return NotFound();
        }

        return View(unit);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Unit unit)
    {
        if (string.IsNullOrWhiteSpace(unit.Code))
        {
            ModelState.AddModelError("Code", "Code is required.");
        }

        if (string.IsNullOrWhiteSpace(unit.Name))
        {
            ModelState.AddModelError("Name", "Name is required.");
        }

        if (string.IsNullOrWhiteSpace(unit.Kind))
        {
            ModelState.AddModelError("Kind", "Kind is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(unit);
        }

        unit.Id = Guid.NewGuid();

        _context.Units.Add(unit);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var unit = await _context.Units.FindAsync(id);

        if (unit == null)
        {
            return NotFound();
        }

        return View(unit);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Unit unit)
    {
        if (id != unit.Id)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(unit.Code))
        {
            ModelState.AddModelError("Code", "Code is required.");
        }

        if (string.IsNullOrWhiteSpace(unit.Name))
        {
            ModelState.AddModelError("Name", "Name is required.");
        }

        if (string.IsNullOrWhiteSpace(unit.Kind))
        {
            ModelState.AddModelError("Kind", "Kind is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(unit);
        }

        var unitFromDb = await _context.Units.FindAsync(id);

        if (unitFromDb == null)
        {
            return NotFound();
        }

        unitFromDb.Code = unit.Code;
        unitFromDb.Name = unit.Name;
        unitFromDb.Kind = unit.Kind;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var unit = await _context.Units
            .FirstOrDefaultAsync(x => x.Id == id);

        if (unit == null)
        {
            return NotFound();
        }

        return View(unit);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var unit = await _context.Units.FindAsync(id);

        if (unit == null)
        {
            return NotFound();
        }

        _context.Units.Remove(unit);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}