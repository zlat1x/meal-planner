using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class IconsController : Controller
{
    private readonly MealPlannerDbContext _context;

    public IconsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var icons = await _context.Icons
            .OrderBy(x => x.Code)
            .ToListAsync();

        return View(icons);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var icon = await _context.Icons.FirstOrDefaultAsync(x => x.Id == id);

        if (icon == null)
        {
            return NotFound();
        }

        return View(icon);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Icon icon)
    {
        if (string.IsNullOrWhiteSpace(icon.Code))
        {
            ModelState.AddModelError("Code", "Code is required.");
        }

        if (string.IsNullOrWhiteSpace(icon.Emoji))
        {
            ModelState.AddModelError("Emoji", "Emoji is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(icon);
        }

        icon.Id = Guid.NewGuid();

        _context.Icons.Add(icon);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var icon = await _context.Icons.FindAsync(id);

        if (icon == null)
        {
            return NotFound();
        }

        return View(icon);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Icon icon)
    {
        if (id != icon.Id)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(icon.Code))
        {
            ModelState.AddModelError("Code", "Code is required.");
        }

        if (string.IsNullOrWhiteSpace(icon.Emoji))
        {
            ModelState.AddModelError("Emoji", "Emoji is required.");
        }

        if (!ModelState.IsValid)
        {
            return View(icon);
        }

        var iconFromDb = await _context.Icons.FindAsync(id);

        if (iconFromDb == null)
        {
            return NotFound();
        }

        iconFromDb.Code = icon.Code;
        iconFromDb.Emoji = icon.Emoji;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var icon = await _context.Icons.FirstOrDefaultAsync(x => x.Id == id);

        if (icon == null)
        {
            return NotFound();
        }

        return View(icon);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var icon = await _context.Icons.FindAsync(id);

        if (icon == null)
        {
            return NotFound();
        }

        _context.Icons.Remove(icon);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}