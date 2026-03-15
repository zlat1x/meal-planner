using MealPlanner.Domain.Entities;
using MealPlanner.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Web.Controllers;

public class ShopItemsController : Controller
{
    private readonly MealPlannerDbContext _context;

    public ShopItemsController(MealPlannerDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var shopItems = await _context.ShopItems
            .Include(x => x.List)
            .ThenInclude(x => x.Plan)
            .Include(x => x.Food)
            .Include(x => x.QuantityUnit)
            .OrderBy(x => x.Food.Name)
            .ToListAsync();

        return View(shopItems);
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var shopItem = await _context.ShopItems
            .Include(x => x.List)
            .ThenInclude(x => x.Plan)
            .Include(x => x.Food)
            .Include(x => x.QuantityUnit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shopItem == null)
        {
            return NotFound();
        }

        return View(shopItem);
    }

    public async Task<IActionResult> Create()
    {
        await LoadListsAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShopItem shopItem)
    {
        ModelState.Remove("List");
        ModelState.Remove("Food"); 
        ModelState.Remove("QuantityUnit");

        await ValidateShopItemAsync(shopItem);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(shopItem.ListId, shopItem.FoodId, shopItem.QuantityUnitId);
            return View(shopItem);
        }

        shopItem.Id = Guid.NewGuid();

        _context.ShopItems.Add(shopItem);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var shopItem = await _context.ShopItems.FindAsync(id);

        if (shopItem == null)
        {
            return NotFound();
        }

        await LoadListsAsync(shopItem.ListId, shopItem.FoodId, shopItem.QuantityUnitId);
        return View(shopItem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ShopItem shopItem)
    {
        ModelState.Remove("List");
        ModelState.Remove("Food"); 
        ModelState.Remove("QuantityUnit");
        
        if (id != shopItem.Id)
        {
            return NotFound();
        }

        await ValidateShopItemAsync(shopItem);

        if (!ModelState.IsValid)
        {
            await LoadListsAsync(shopItem.ListId, shopItem.FoodId, shopItem.QuantityUnitId);
            return View(shopItem);
        }

        var shopItemFromDb = await _context.ShopItems.FindAsync(id);

        if (shopItemFromDb == null)
        {
            return NotFound();
        }

        shopItemFromDb.ListId = shopItem.ListId;
        shopItemFromDb.FoodId = shopItem.FoodId;
        shopItemFromDb.TotalQuantityValue = shopItem.TotalQuantityValue;
        shopItemFromDb.QuantityUnitId = shopItem.QuantityUnitId;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var shopItem = await _context.ShopItems
            .Include(x => x.List)
            .Include(x => x.Food)
            .Include(x => x.QuantityUnit)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (shopItem == null)
        {
            return NotFound();
        }

        return View(shopItem);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var shopItem = await _context.ShopItems.FindAsync(id);

        if (shopItem == null)
        {
            return NotFound();
        }

        _context.ShopItems.Remove(shopItem);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task LoadListsAsync(Guid? selectedListId = null, Guid? selectedFoodId = null, Guid? selectedUnitId = null)
    {
        var shopLists = await _context.ShopLists
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        var foods = await _context.Foods
            .OrderBy(x => x.Name)
            .ToListAsync();

        var units = await _context.Units
            .OrderBy(x => x.Name)
            .ToListAsync();

        var listItems = shopLists.Select(x => new
        {
            x.Id,
            Text = $"Shop list ({x.Days} days)"
        });

        ViewBag.ListId = new SelectList(listItems, "Id", "Text", selectedListId);
        ViewBag.FoodId = new SelectList(foods, "Id", "Name", selectedFoodId);
        ViewBag.QuantityUnitId = new SelectList(units, "Id", "Name", selectedUnitId);
    }

    private async Task ValidateShopItemAsync(ShopItem shopItem)
    {
        var listExists = await _context.ShopLists.AnyAsync(x => x.Id == shopItem.ListId);
        if (!listExists)
        {
            ModelState.AddModelError("ListId", "Shop list is required.");
        }

        var foodExists = await _context.Foods.AnyAsync(x => x.Id == shopItem.FoodId);
        if (!foodExists)
        {
            ModelState.AddModelError("FoodId", "Food is required.");
        }

        var unitExists = await _context.Units.AnyAsync(x => x.Id == shopItem.QuantityUnitId);
        if (!unitExists)
        {
            ModelState.AddModelError("QuantityUnitId", "Quantity unit is required.");
        }
    }
}