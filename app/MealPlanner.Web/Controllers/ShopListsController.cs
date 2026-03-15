using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class ShopListsController : Controller
{
    private readonly MealPlannerDbContext _context;

    public ShopListsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var shopLists = await _context.ShopLists
            .Include(x => x.Plan)
            .ThenInclude(x => x.User)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(shopLists);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var shopList = await _context.ShopLists
            .Include(x => x.Plan)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shopList == null)
        {
            return NotFound();
        }

        return View(shopList);
    }

    public async Task<IActionResult> Create()
    {
        await LoadPlansAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShopList shopList)
    {
        var planExists = await _context.Plans.AnyAsync(x => x.Id == shopList.PlanId);
        if (!planExists)
        {
            ModelState.AddModelError("PlanId", "Plan is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPlansAsync(shopList.PlanId);
            return View(shopList);
        }

        shopList.Id = Guid.NewGuid();
        shopList.CreatedAt = DateTime.UtcNow;

        _context.ShopLists.Add(shopList);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var shopList = await _context.ShopLists.FindAsync(id);

        if (shopList == null)
        {
            return NotFound();
        }

        await LoadPlansAsync(shopList.PlanId);
        return View(shopList);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ShopList shopList)
    {
        if (id != shopList.Id)
        {
            return NotFound();
        }

        var planExists = await _context.Plans.AnyAsync(x => x.Id == shopList.PlanId);
        if (!planExists)
        {
            ModelState.AddModelError("PlanId", "Plan is required.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPlansAsync(shopList.PlanId);
            return View(shopList);
        }

        var shopListFromDb = await _context.ShopLists.FindAsync(id);

        if (shopListFromDb == null)
        {
            return NotFound();
        }

        shopListFromDb.PlanId = shopList.PlanId;
        shopListFromDb.Days = shopList.Days;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var shopList = await _context.ShopLists
            .Include(x => x.Plan)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shopList == null)
        {
            return NotFound();
        }

        return View(shopList);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var shopList = await _context.ShopLists.FindAsync(id);

        if (shopList == null)
        {
            return NotFound();
        }

        _context.ShopLists.Remove(shopList);
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